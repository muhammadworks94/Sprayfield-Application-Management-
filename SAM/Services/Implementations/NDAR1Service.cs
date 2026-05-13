using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.Utilities;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for NDAR1 (Non-Discharge Application Report) entity operations.
/// </summary>
public class NDAR1Service : INDAR1Service
{
    //private const decimal GALLONS_PER_ACRE_INCH = 27154m;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NDAR1Service> _logger;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IFacilityService _facilityService;
    private readonly IApplicationComplianceService _applicationComplianceService;
    private readonly IWebHostEnvironment _environment;

    public NDAR1Service(
        ApplicationDbContext context,
        ILogger<NDAR1Service> logger,
        ISprayfieldService sprayfieldService,
        IFacilityService facilityService,
        IApplicationComplianceService applicationComplianceService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _sprayfieldService = sprayfieldService;
        _facilityService = facilityService;
        _applicationComplianceService = applicationComplianceService;
        _environment = environment;
    }

    public async Task<IEnumerable<NDAR1>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var query = _context.NDAR1s
            .Include(n => n.Company)
            .Include(n => n.Facility)
            .Include(n => n.Field1)
            .Include(n => n.Field2)
            .Include(n => n.Field3)
            .Include(n => n.Field4)
            .Include(n => n.Fields)
                .ThenInclude(f => f.Sprayfield)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(n => n.FacilityId == facilityId.Value);
        }

        var reports = await query
            .OrderByDescending(n => n.Year)
            .ThenByDescending(n => n.Month)
            .ToListAsync();

        await ApplyDidIrrigationOccurFromApplicationsAsync(reports);
        return reports;
    }

    public async Task<NDAR1?> GetByIdAsync(Guid id)
    {
        var report = await _context.NDAR1s
            .Include(n => n.Company)
            .Include(n => n.Facility)
            .Include(n => n.Field1)
                .ThenInclude(f => f!.Crop)
            .Include(n => n.Field2)
                .ThenInclude(f => f!.Crop)
            .Include(n => n.Field3)
                .ThenInclude(f => f!.Crop)
            .Include(n => n.Field4)
                .ThenInclude(f => f!.Crop)
            .Include(n => n.Fields)
                .ThenInclude(f => f.Sprayfield)
                    .ThenInclude(s => s!.Crop)
            .Include(n => n.Fields)
                .ThenInclude(f => f.DailyValues)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (report != null)
        {
            await ApplyDidIrrigationOccurFromApplicationsAsync(new List<NDAR1> { report });
        }

        return report;
    }

    public async Task<NDAR1> CreateAsync(NDAR1 ndar1)
    {
        if (ndar1 == null)
            throw new ArgumentNullException(nameof(ndar1));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == ndar1.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), ndar1.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == ndar1.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), ndar1.FacilityId);
        if (facility.CompanyId != ndar1.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if report already exists for this facility/month/year
        var existing = await GetByFacilityMonthYearAsync(ndar1.FacilityId, (int)ndar1.Month, ndar1.Year);
        if (existing != null)
            throw new BusinessRuleException($"An NDAR-1 report already exists for facility {facility.Name} for {ndar1.Month} {ndar1.Year}.");

        // Initialize daily arrays if empty
        InitializeDailyArrays(ndar1);
        SyncDynamicFieldsFromLegacy(ndar1);
        ndar1.DidIrrigationOccur = await ComputeDidIrrigationOccurForMonthAsync(
            ndar1.FacilityId,
            ndar1.Year,
            (int)ndar1.Month);

        _context.NDAR1s.Add(ndar1);
        await _context.SaveChangesAsync();

        _logger.LogInformation("NDAR-1 report created for facility '{FacilityName}' for {Month} {Year} (ID: {ReportId})",
            facility.Name, ndar1.Month, ndar1.Year, ndar1.Id);
        return ndar1;
    }

    public async Task<NDAR1> UpdateAsync(NDAR1 ndar1)
    {
        if (ndar1 == null)
            throw new ArgumentNullException(nameof(ndar1));

        var existing = await GetByIdAsync(ndar1.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(NDAR1), ndar1.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == ndar1.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), ndar1.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == ndar1.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), ndar1.FacilityId);
        if (facility.CompanyId != ndar1.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if another report exists for this facility/month/year (excluding current)
        var duplicate = await _context.NDAR1s
            .FirstOrDefaultAsync(n => n.Id != ndar1.Id && n.FacilityId == ndar1.FacilityId && n.Month == ndar1.Month && n.Year == ndar1.Year);
        if (duplicate != null)
            throw new BusinessRuleException($"An NDAR-1 report already exists for facility {facility.Name} for {ndar1.Month} {ndar1.Year}.");

        // Update all properties
        existing.Month = ndar1.Month;
        existing.Year = ndar1.Year;
        existing.DidIrrigationOccur = await ComputeDidIrrigationOccurForMonthAsync(
            ndar1.FacilityId,
            ndar1.Year,
            (int)ndar1.Month);
        existing.WeatherCodeDaily = ndar1.WeatherCodeDaily;
        existing.TemperatureDaily = ndar1.TemperatureDaily;
        existing.PrecipitationDaily = ndar1.PrecipitationDaily;
        existing.FiveDayUpsetDaily = ndar1.FiveDayUpsetDaily;

        // Field 1
        existing.Field1Id = ndar1.Field1Id;
        existing.Field1VolumeAppliedDaily = ndar1.Field1VolumeAppliedDaily;
        existing.Field1TimeIrrigatedDaily = ndar1.Field1TimeIrrigatedDaily;
        existing.Field1DailyLoadingDaily = ndar1.Field1DailyLoadingDaily;
        existing.Field1MaxHourlyLoadingDaily = ndar1.Field1MaxHourlyLoadingDaily;
        existing.Field1MonthlyLoading = ndar1.Field1MonthlyLoading;
        existing.Field1MaxHourlyLoading = ndar1.Field1MaxHourlyLoading;
        existing.Field1TwelveMonthFloatingTotal = ndar1.Field1TwelveMonthFloatingTotal;

        // Field 2
        existing.Field2Id = ndar1.Field2Id;
        existing.Field2VolumeAppliedDaily = ndar1.Field2VolumeAppliedDaily;
        existing.Field2TimeIrrigatedDaily = ndar1.Field2TimeIrrigatedDaily;
        existing.Field2DailyLoadingDaily = ndar1.Field2DailyLoadingDaily;
        existing.Field2MaxHourlyLoadingDaily = ndar1.Field2MaxHourlyLoadingDaily;
        existing.Field2MonthlyLoading = ndar1.Field2MonthlyLoading;
        existing.Field2MaxHourlyLoading = ndar1.Field2MaxHourlyLoading;
        existing.Field2TwelveMonthFloatingTotal = ndar1.Field2TwelveMonthFloatingTotal;

        // Field 3
        existing.Field3Id = ndar1.Field3Id;
        existing.Field3VolumeAppliedDaily = ndar1.Field3VolumeAppliedDaily;
        existing.Field3TimeIrrigatedDaily = ndar1.Field3TimeIrrigatedDaily;
        existing.Field3DailyLoadingDaily = ndar1.Field3DailyLoadingDaily;
        existing.Field3MaxHourlyLoadingDaily = ndar1.Field3MaxHourlyLoadingDaily;
        existing.Field3MonthlyLoading = ndar1.Field3MonthlyLoading;
        existing.Field3MaxHourlyLoading = ndar1.Field3MaxHourlyLoading;
        existing.Field3TwelveMonthFloatingTotal = ndar1.Field3TwelveMonthFloatingTotal;

        // Field 4
        existing.Field4Id = ndar1.Field4Id;
        existing.Field4VolumeAppliedDaily = ndar1.Field4VolumeAppliedDaily;
        existing.Field4TimeIrrigatedDaily = ndar1.Field4TimeIrrigatedDaily;
        existing.Field4DailyLoadingDaily = ndar1.Field4DailyLoadingDaily;
        existing.Field4MaxHourlyLoadingDaily = ndar1.Field4MaxHourlyLoadingDaily;
        existing.Field4MonthlyLoading = ndar1.Field4MonthlyLoading;
        existing.Field4MaxHourlyLoading = ndar1.Field4MaxHourlyLoading;
        existing.Field4TwelveMonthFloatingTotal = ndar1.Field4TwelveMonthFloatingTotal;

        if (ndar1.Fields.Count > 0)
        {
            existing.Fields.Clear();
            foreach (var field in ndar1.Fields.OrderBy(f => f.FieldOrder))
            {
                var mapped = new NDAR1Field
                {
                    Id = Guid.NewGuid(),
                    NDAR1Id = existing.Id,
                    SprayfieldId = field.SprayfieldId,
                    FieldOrder = field.FieldOrder,
                    MonthlyLoading = field.MonthlyLoading,
                    MaxHourlyLoading = field.MaxHourlyLoading,
                    TwelveMonthFloatingTotal = field.TwelveMonthFloatingTotal
                };

                foreach (var daily in field.DailyValues.OrderBy(d => d.DayNo))
                {
                    mapped.DailyValues.Add(new NDAR1FieldDaily
                    {
                        Id = Guid.NewGuid(),
                        DayNo = daily.DayNo,
                        VolumeApplied = daily.VolumeApplied,
                        TimeIrrigated = daily.TimeIrrigated,
                        DailyLoading = daily.DailyLoading,
                        MaxHourlyLoading = daily.MaxHourlyLoading
                    });
                }

                existing.Fields.Add(mapped);
            }
        }
        else
        {
            SyncDynamicFieldsFromLegacy(existing);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("NDAR-1 report updated (ID: {ReportId})", ndar1.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var ndar1 = await GetByIdAsync(id);
        if (ndar1 == null)
            throw new EntityNotFoundException(nameof(NDAR1), id);

        // Hard delete - permanently remove the report from the database
        _context.NDAR1s.Remove(ndar1);
        await _context.SaveChangesAsync();

        _logger.LogInformation("NDAR-1 report hard-deleted (ID: {ReportId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.NDAR1s.AnyAsync(n => n.Id == id);
    }

    public async Task<NDAR1?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year)
    {
        return await _context.NDAR1s
            .FirstOrDefaultAsync(n => n.FacilityId == facilityId && (int)n.Month == month && n.Year == year);
    }

    public async Task<NdarRefreshOutcome> RefreshExistingReportForMonthAsync(Guid facilityId, int month, int year)
    {
        var outcome = new NdarRefreshOutcome
        {
            FacilityId = facilityId,
            Month = month,
            Year = year
        };
        var periodText = new DateTime(year, month, 1).ToString("MMM yyyy");
        var lockResource = $"NDAR1:{facilityId:D}:{year:D4}:{month:D2}";
        var lockTimeoutMs = 60000;
        var refreshStopwatch = Stopwatch.StartNew();

        try
        {
            Guid? refreshedReportId = null;
            var noReport = false;
            const int maxConcurrencyAttempts = 2;
            var concurrencyAttempt = 0;

            while (true)
            {
                try
                {
                    var strategy = _context.Database.CreateExecutionStrategy();
                    await strategy.ExecuteAsync(async () =>
                    {
                        await using var tx = await _context.Database.BeginTransactionAsync();
                        var lockResult = await AcquireNdarRefreshLockAsync(lockResource, lockTimeoutMs);

                        if (lockResult < 0)
                        {
                            throw new NdarRefreshLockException(lockResource, lockResult);
                        }

                        _logger.LogInformation(
                            "Acquired NDAR-1 refresh lock {LockResource} for facility {FacilityId} month {Month} year {Year}.",
                            lockResource, facilityId, month, year);

                        var existing = await _context.NDAR1s
                            .FirstOrDefaultAsync(n => n.FacilityId == facilityId && (int)n.Month == month && n.Year == year);

                        if (existing == null)
                        {
                            noReport = true;
                            await tx.CommitAsync();
                            _logger.LogInformation(
                                "Released NDAR-1 refresh lock {LockResource} for facility {FacilityId} month {Month} year {Year} (no report).",
                                lockResource, facilityId, month, year);
                            return;
                        }

                        _logger.LogInformation("Refreshing NDAR-1 report {ReportId} for facility {FacilityId} month {Month} year {Year}.",
                            existing.Id, facilityId, month, year);

                        var regenerated = await GenerateMonthlyReportAsync(facilityId, month, year);
                        CopyComputedSnapshotWithoutDynamicFields(existing, regenerated);
                        await _context.NDAR1FieldDailies
                            .Where(d => d.NDAR1Field != null && d.NDAR1Field.NDAR1Id == existing.Id)
                            .ExecuteDeleteAsync();
                        await _context.NDAR1Fields
                            .Where(f => f.NDAR1Id == existing.Id)
                            .ExecuteDeleteAsync();

                        var newFields = BuildDynamicFieldEntities(existing.Id, regenerated);
                        if (newFields.Count > 0)
                        {
                            _context.NDAR1Fields.AddRange(newFields);
                        }

                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                        _logger.LogInformation(
                            "Released NDAR-1 refresh lock {LockResource} for facility {FacilityId} month {Month} year {Year} after update.",
                            lockResource, facilityId, month, year);

                        refreshedReportId = existing.Id;
                    });

                    break;
                }
                catch (DbUpdateConcurrencyException ex) when (concurrencyAttempt < maxConcurrencyAttempts - 1)
                {
                    concurrencyAttempt++;
                    _logger.LogWarning(ex,
                        "NDAR-1 refresh concurrency conflict for facility {FacilityId} month {Month} year {Year}. Retrying attempt {Attempt}.",
                        facilityId, month, year, concurrencyAttempt + 1);

                    _context.ChangeTracker.Clear();
                    await Task.Delay(200);
                }
            }

            if (noReport)
            {
                _logger.LogDebug("NDAR-1 refresh skipped: no report for facility {FacilityId} month {Month} year {Year}.",
                    facilityId, month, year);
                outcome.Status = NdarRefreshStatus.NoReport;
                outcome.Message = $"No NDAR-1 report exists for {periodText}.";
                _logger.LogInformation(
                    "NDAR-1 refresh finished in {DurationMs} ms with outcome {Outcome} for facility {FacilityId} month {Month} year {Year}.",
                    refreshStopwatch.ElapsedMilliseconds, outcome.Status, facilityId, month, year);
                return outcome;
            }

            _logger.LogInformation("Refreshed NDAR-1 report {ReportId} for facility {FacilityId} month {Month} year {Year}.",
                refreshedReportId, facilityId, month, year);
            outcome.Status = NdarRefreshStatus.Updated;
            outcome.Message = $"NDAR-1 report for {periodText} refreshed successfully.";
            _logger.LogInformation(
                "NDAR-1 refresh finished in {DurationMs} ms with outcome {Outcome} for facility {FacilityId} month {Month} year {Year}.",
                refreshStopwatch.ElapsedMilliseconds, outcome.Status, facilityId, month, year);
            return outcome;
        }
        catch (NdarRefreshLockException lockEx)
        {
            _logger.LogWarning(lockEx,
                "NDAR-1 refresh lock not acquired for facility {FacilityId} month {Month} year {Year}. LockResult={LockResult}.",
                facilityId, month, year, lockEx.LockResult);
            outcome.Status = NdarRefreshStatus.Failed;
            outcome.Message = $"NDAR-1 refresh could not obtain lock for {periodText}; please retry.";
            _logger.LogInformation(
                "NDAR-1 refresh finished in {DurationMs} ms with outcome {Outcome} for facility {FacilityId} month {Month} year {Year}.",
                refreshStopwatch.ElapsedMilliseconds, outcome.Status, facilityId, month, year);
            return outcome;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "NDAR-1 refresh failed for facility {FacilityId} month {Month} year {Year}.",
                facilityId, month, year);
            outcome.Status = NdarRefreshStatus.Failed;
            outcome.Message = $"NDAR-1 refresh failed for {periodText}. Please retry.";
            _logger.LogInformation(
                "NDAR-1 refresh finished in {DurationMs} ms with outcome {Outcome} for facility {FacilityId} month {Month} year {Year}.",
                refreshStopwatch.ElapsedMilliseconds, outcome.Status, facilityId, month, year);
            return outcome;
        }
    }

    private async Task<int> AcquireNdarRefreshLockAsync(string lockResource, int timeoutMs)
    {
        var lockResultParam = new SqlParameter("@LockResult", System.Data.SqlDbType.Int)
        {
            Direction = System.Data.ParameterDirection.Output
        };

        var resourceParam = new SqlParameter("@Resource", lockResource);
        var lockModeParam = new SqlParameter("@LockMode", "Exclusive");
        var lockOwnerParam = new SqlParameter("@LockOwner", "Transaction");
        var timeoutParam = new SqlParameter("@LockTimeout", timeoutMs);

        await _context.Database.ExecuteSqlRawAsync(
            "EXEC @LockResult = sp_getapplock @Resource = @Resource, @LockMode = @LockMode, @LockOwner = @LockOwner, @LockTimeout = @LockTimeout;",
            lockResultParam, resourceParam, lockModeParam, lockOwnerParam, timeoutParam);

        return lockResultParam.Value is int val ? val : -999;
    }

    private sealed class NdarRefreshLockException : Exception
    {
        public string Resource { get; }
        public int LockResult { get; }

        public NdarRefreshLockException(string resource, int lockResult)
            : base($"Unable to acquire NDAR-1 refresh lock (resource={resource}, result={lockResult}).")
        {
            Resource = resource;
            LockResult = lockResult;
        }
    }

    public async Task<IEnumerable<NDAR1>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.NDAR1s
            .Where(n => n.FacilityId == facilityId)
            .OrderByDescending(n => n.Year)
            .ThenByDescending(n => n.Month)
            .ToListAsync();
    }

    /// <summary>
    /// Generates a monthly NDAR-1 report by aggregating irrigation data for the specified facility, month, and year.
    /// </summary>
    public async Task<NDAR1> GenerateMonthlyReportAsync(Guid facilityId, int month, int year)
    {
        var facility = await _context.Facilities
            .Include(f => f.Company)
            .FirstOrDefaultAsync(f => f.Id == facilityId);

        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), facilityId);

        // Get all monthly applications for this facility in the specified month/year
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var applications = await _context.MonthlyApplications
            .Include(a => a.Sprayfield)
            .Where(a => a.FacilityId == facilityId &&
                       a.ApplicationDate >= startDate &&
                       a.ApplicationDate <= endDate)
            .ToListAsync();

        // Get sprayfields for this facility
        var sprayfields = await _sprayfieldService.GetByFacilityIdAsync(facilityId);
        var sprayfieldList = sprayfields
            .OrderBy(s => ParseFieldOrder(s.FieldId))
            .ThenBy(s => s.FieldId)
            .ToList();
        var sprayfieldById = sprayfieldList.ToDictionary(s => s.Id, s => s);

        // Get operator logs for weather data
        var operatorLogs = await _context.OperatorLogs
            .Where(o => o.FacilityId == facilityId &&
                       !o.IsDeleted &&
                       o.LogDate >= startDate &&
                       o.LogDate <= endDate)
            .ToListAsync();

        // Initialize the report
        var report = new NDAR1
        {
            CompanyId = facility.CompanyId,
            FacilityId = facilityId,
            Month = (MonthEnum)month,
            Year = year,
            DidIrrigationOccur = applications.Any(a => a.TimeIrrigatedMinutes.HasValue && a.TimeIrrigatedMinutes.Value > 0m)
        };

        // Initialize daily arrays
        InitializeDailyArrays(report);

        // Populate field references
        if (sprayfieldList.Count > 0) report.Field1Id = sprayfieldList[0].Id;
        if (sprayfieldList.Count > 1) report.Field2Id = sprayfieldList[1].Id;
        if (sprayfieldList.Count > 2) report.Field3Id = sprayfieldList[2].Id;
        if (sprayfieldList.Count > 3) report.Field4Id = sprayfieldList[3].Id;

        // Aggregate daily weather data from operator logs
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);
            var dayIndex = day - 1;

            var logForDay = operatorLogs.FirstOrDefault(o => o.LogDate.Date == currentDate.Date);
            if (logForDay != null && !string.IsNullOrEmpty(logForDay.WeatherConditions))
            {
                report.WeatherCodeDaily[dayIndex] = logForDay.WeatherConditions;
            }
            report.TemperatureDaily[dayIndex] = logForDay?.TemperatureF;
            report.PrecipitationDaily[dayIndex] = logForDay?.PrecipitationIn;
            report.FiveDayUpsetDaily[dayIndex] = logForDay?.FiveDayUpsetFt;
        }

        // Aggregate daily application data by field
        var fieldApplications = new Dictionary<Guid, List<MonthlyApplication>>();
        foreach (var application in applications)
        {
            var sprayfieldId = application.SprayfieldId;
            if (!fieldApplications.ContainsKey(sprayfieldId))
            {
                fieldApplications[sprayfieldId] = new List<MonthlyApplication>();
            }

            fieldApplications[sprayfieldId].Add(application);
        }

        // Process each legacy field block (first 4 kept for backward compatibility)
        ProcessFieldData(report.Field1Id, sprayfieldById, fieldApplications, report.Field1VolumeAppliedDaily,
            report.Field1TimeIrrigatedDaily, report.Field1DailyLoadingDaily,
            report.Field1MaxHourlyLoadingDaily, startDate, daysInMonth);
        ProcessFieldData(report.Field2Id, sprayfieldById, fieldApplications, report.Field2VolumeAppliedDaily,
            report.Field2TimeIrrigatedDaily, report.Field2DailyLoadingDaily,
            report.Field2MaxHourlyLoadingDaily, startDate, daysInMonth);
        ProcessFieldData(report.Field3Id, sprayfieldById, fieldApplications, report.Field3VolumeAppliedDaily,
            report.Field3TimeIrrigatedDaily, report.Field3DailyLoadingDaily,
            report.Field3MaxHourlyLoadingDaily, startDate, daysInMonth);
        ProcessFieldData(report.Field4Id, sprayfieldById, fieldApplications, report.Field4VolumeAppliedDaily,
            report.Field4TimeIrrigatedDaily, report.Field4DailyLoadingDaily,
            report.Field4MaxHourlyLoadingDaily, startDate, daysInMonth);

        // Calculate monthly totals
        report.Field1MonthlyLoading = report.Field1DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field1MaxHourlyLoading = report.Field1MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();
        report.Field2MonthlyLoading = report.Field2DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field2MaxHourlyLoading = report.Field2MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();
        report.Field3MonthlyLoading = report.Field3DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field3MaxHourlyLoading = report.Field3MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();
        report.Field4MonthlyLoading = report.Field4DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field4MaxHourlyLoading = report.Field4MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();

        // Calculate rolling 365-day hydraulic totals from zone-model applications.
        await CalculateRollingFloatingTotals(report, endDate);
        BuildDynamicFields(report, sprayfieldList, sprayfieldById, fieldApplications, startDate, daysInMonth, endDate);

        return report;
    }

    private static void CopyComputedSnapshotWithoutDynamicFields(NDAR1 target, NDAR1 source)
    {
        target.Month = source.Month;
        target.Year = source.Year;
        target.DidIrrigationOccur = source.DidIrrigationOccur;
        target.WeatherCodeDaily = source.WeatherCodeDaily;
        target.TemperatureDaily = source.TemperatureDaily;
        target.PrecipitationDaily = source.PrecipitationDaily;
        target.FiveDayUpsetDaily = source.FiveDayUpsetDaily;

        target.Field1Id = source.Field1Id;
        target.Field1VolumeAppliedDaily = source.Field1VolumeAppliedDaily;
        target.Field1TimeIrrigatedDaily = source.Field1TimeIrrigatedDaily;
        target.Field1DailyLoadingDaily = source.Field1DailyLoadingDaily;
        target.Field1MaxHourlyLoadingDaily = source.Field1MaxHourlyLoadingDaily;
        target.Field1MonthlyLoading = source.Field1MonthlyLoading;
        target.Field1MaxHourlyLoading = source.Field1MaxHourlyLoading;
        target.Field1TwelveMonthFloatingTotal = source.Field1TwelveMonthFloatingTotal;

        target.Field2Id = source.Field2Id;
        target.Field2VolumeAppliedDaily = source.Field2VolumeAppliedDaily;
        target.Field2TimeIrrigatedDaily = source.Field2TimeIrrigatedDaily;
        target.Field2DailyLoadingDaily = source.Field2DailyLoadingDaily;
        target.Field2MaxHourlyLoadingDaily = source.Field2MaxHourlyLoadingDaily;
        target.Field2MonthlyLoading = source.Field2MonthlyLoading;
        target.Field2MaxHourlyLoading = source.Field2MaxHourlyLoading;
        target.Field2TwelveMonthFloatingTotal = source.Field2TwelveMonthFloatingTotal;

        target.Field3Id = source.Field3Id;
        target.Field3VolumeAppliedDaily = source.Field3VolumeAppliedDaily;
        target.Field3TimeIrrigatedDaily = source.Field3TimeIrrigatedDaily;
        target.Field3DailyLoadingDaily = source.Field3DailyLoadingDaily;
        target.Field3MaxHourlyLoadingDaily = source.Field3MaxHourlyLoadingDaily;
        target.Field3MonthlyLoading = source.Field3MonthlyLoading;
        target.Field3MaxHourlyLoading = source.Field3MaxHourlyLoading;
        target.Field3TwelveMonthFloatingTotal = source.Field3TwelveMonthFloatingTotal;

        target.Field4Id = source.Field4Id;
        target.Field4VolumeAppliedDaily = source.Field4VolumeAppliedDaily;
        target.Field4TimeIrrigatedDaily = source.Field4TimeIrrigatedDaily;
        target.Field4DailyLoadingDaily = source.Field4DailyLoadingDaily;
        target.Field4MaxHourlyLoadingDaily = source.Field4MaxHourlyLoadingDaily;
        target.Field4MonthlyLoading = source.Field4MonthlyLoading;
        target.Field4MaxHourlyLoading = source.Field4MaxHourlyLoading;
        target.Field4TwelveMonthFloatingTotal = source.Field4TwelveMonthFloatingTotal;
    }

    private static void AddDynamicFields(NDAR1 target, NDAR1 source)
    {
        foreach (var sourceField in source.Fields.OrderBy(f => f.FieldOrder))
        {
            var mappedField = new NDAR1Field
            {
                Id = Guid.NewGuid(),
                NDAR1Id = target.Id,
                SprayfieldId = sourceField.SprayfieldId,
                FieldOrder = sourceField.FieldOrder,
                MonthlyLoading = sourceField.MonthlyLoading,
                MaxHourlyLoading = sourceField.MaxHourlyLoading,
                TwelveMonthFloatingTotal = sourceField.TwelveMonthFloatingTotal
            };

            foreach (var sourceDaily in sourceField.DailyValues.OrderBy(d => d.DayNo))
            {
                mappedField.DailyValues.Add(new NDAR1FieldDaily
                {
                    Id = Guid.NewGuid(),
                    DayNo = sourceDaily.DayNo,
                    VolumeApplied = sourceDaily.VolumeApplied,
                    TimeIrrigated = sourceDaily.TimeIrrigated,
                    DailyLoading = sourceDaily.DailyLoading,
                    MaxHourlyLoading = sourceDaily.MaxHourlyLoading
                });
            }

            target.Fields.Add(mappedField);
        }
    }

    private static List<NDAR1Field> BuildDynamicFieldEntities(Guid ndar1Id, NDAR1 source)
    {
        var fields = new List<NDAR1Field>();

        foreach (var sourceField in source.Fields.OrderBy(f => f.FieldOrder))
        {
            var mappedField = new NDAR1Field
            {
                Id = Guid.NewGuid(),
                NDAR1Id = ndar1Id,
                SprayfieldId = sourceField.SprayfieldId,
                FieldOrder = sourceField.FieldOrder,
                MonthlyLoading = sourceField.MonthlyLoading,
                MaxHourlyLoading = sourceField.MaxHourlyLoading,
                TwelveMonthFloatingTotal = sourceField.TwelveMonthFloatingTotal
            };

            foreach (var sourceDaily in sourceField.DailyValues.OrderBy(d => d.DayNo))
            {
                mappedField.DailyValues.Add(new NDAR1FieldDaily
                {
                    Id = Guid.NewGuid(),
                    DayNo = sourceDaily.DayNo,
                    VolumeApplied = sourceDaily.VolumeApplied,
                    TimeIrrigated = sourceDaily.TimeIrrigated,
                    DailyLoading = sourceDaily.DailyLoading,
                    MaxHourlyLoading = sourceDaily.MaxHourlyLoading
                });
            }

            fields.Add(mappedField);
        }

        return fields;
    }

    private async Task ApplyDidIrrigationOccurFromApplicationsAsync(List<NDAR1> reports)
    {
        if (reports.Count == 0)
        {
            return;
        }

        var facilityIds = reports.Select(r => r.FacilityId).Distinct().ToList();
        var start = reports.Min(r => new DateTime(r.Year, (int)r.Month, 1));
        var endExclusive = reports
            .Max(r => new DateTime(r.Year, (int)r.Month, 1).AddMonths(1));

        var irrigatedMonthKeys = await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a =>
                facilityIds.Contains(a.FacilityId) &&
                a.ApplicationDate >= start &&
                a.ApplicationDate < endExclusive &&
                a.TimeIrrigatedMinutes.HasValue &&
                a.TimeIrrigatedMinutes.Value > 0m)
            .Select(a => new { a.FacilityId, a.ApplicationDate.Year, a.ApplicationDate.Month })
            .Distinct()
            .ToListAsync();

        var irrigatedLookup = irrigatedMonthKeys
            .Select(x => (x.FacilityId, x.Year, x.Month))
            .ToHashSet();

        foreach (var report in reports)
        {
            report.DidIrrigationOccur = irrigatedLookup.Contains((report.FacilityId, report.Year, (int)report.Month));
        }
    }

    private async Task<bool> ComputeDidIrrigationOccurForMonthAsync(Guid facilityId, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEndExclusive = monthStart.AddMonths(1);

        return await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a =>
                a.FacilityId == facilityId &&
                a.ApplicationDate >= monthStart &&
                a.ApplicationDate < monthEndExclusive &&
                a.TimeIrrigatedMinutes.HasValue &&
                a.TimeIrrigatedMinutes.Value > 0m)
            .AnyAsync();
    }

    private void BuildDynamicFields(
        NDAR1 report,
        List<Sprayfield> sprayfieldList,
        Dictionary<Guid, Sprayfield> sprayfieldById,
        Dictionary<Guid, List<MonthlyApplication>> fieldApplications,
        DateTime startDate,
        int daysInMonth,
        DateTime asOfDate)
    {
        report.Fields.Clear();
        var order = 1;
        foreach (var sprayfield in sprayfieldList)
        {
            var volumeDaily = Enumerable.Repeat<decimal?>(null, 31).ToList();
            var timeDaily = Enumerable.Repeat<decimal?>(null, 31).ToList();
            var loadingDaily = Enumerable.Repeat<decimal?>(null, 31).ToList();
            var maxHourlyDaily = Enumerable.Repeat<decimal?>(null, 31).ToList();

            ProcessFieldData(sprayfield.Id, sprayfieldById, fieldApplications, volumeDaily, timeDaily, loadingDaily, maxHourlyDaily, startDate, daysInMonth);

            var field = new NDAR1Field
            {
                Id = Guid.NewGuid(),
                SprayfieldId = sprayfield.Id,
                FieldOrder = order++,
                MonthlyLoading = loadingDaily.Where(v => v.HasValue).Sum(v => v ?? 0m),
                MaxHourlyLoading = maxHourlyDaily.Where(v => v.HasValue).Select(v => v ?? 0m).DefaultIfEmpty(0m).Max(),
                TwelveMonthFloatingTotal = 0m
            };

            field.TwelveMonthFloatingTotal = GetRollingHydraulicInchesAsync(report.FacilityId, sprayfield.Id, asOfDate).GetAwaiter().GetResult();

            for (var day = 1; day <= 31; day++)
            {
                var idx = day - 1;
                field.DailyValues.Add(new NDAR1FieldDaily
                {
                    Id = Guid.NewGuid(),
                    DayNo = day,
                    VolumeApplied = volumeDaily[idx],
                    TimeIrrigated = timeDaily[idx],
                    DailyLoading = loadingDaily[idx],
                    MaxHourlyLoading = maxHourlyDaily[idx]
                });
            }

            report.Fields.Add(field);
        }
    }

    private void ProcessFieldData(
        Guid? fieldId,
        Dictionary<Guid, Sprayfield> sprayfieldById,
        Dictionary<Guid, List<MonthlyApplication>> fieldApplications,
        List<decimal?> volumeDaily,
        List<decimal?> timeDaily,
        List<decimal?> loadingDaily,
        List<decimal?> maxHourlyLoadingDaily,
        DateTime startDate,
        int daysInMonth)
    {
        if (!fieldId.HasValue || !sprayfieldById.TryGetValue(fieldId.Value, out var sprayfield))
            return;

        var applications = fieldApplications.TryGetValue(fieldId.Value, out var appList)
            ? appList
            : new List<MonthlyApplication>();
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, day);
            var dayIndex = day - 1;
            var dayApplications = applications.Where(i => i.ApplicationDate.Date == currentDate.Date).ToList();

            var volumeFromApplications = dayApplications.Any()
                ? dayApplications.Sum(i => i.VolumeGallons)
                : (decimal?)null;

            if (!volumeFromApplications.HasValue)
            {
                continue;
            }

            var minutesFromApplications = dayApplications.Any(i => i.TimeIrrigatedMinutes.HasValue)
                ? dayApplications.Where(i => i.TimeIrrigatedMinutes.HasValue).Sum(i => i.TimeIrrigatedMinutes ?? 0m)
                : (decimal?)null;

            var areaAcres = SprayfieldReportHelper.GetReportAcres(sprayfield);
            var volumeApplied = volumeFromApplications.Value;
            volumeDaily[dayIndex] = volumeApplied;
            timeDaily[dayIndex] = minutesFromApplications;

            if (areaAcres > 0)
            {
                loadingDaily[dayIndex] = volumeApplied / (areaAcres * MonthlyApplicationCalculationHelper.GallonsPerAcreInch);
            }
            else
            {
                loadingDaily[dayIndex] = null;
            }

            var dailyLoading = loadingDaily[dayIndex];
            if (!dailyLoading.HasValue)
            {
                maxHourlyLoadingDaily[dayIndex] = null;
            }
            else if (!minutesFromApplications.HasValue || minutesFromApplications.Value <= 0)
            {
                maxHourlyLoadingDaily[dayIndex] = null;
            }
            else if (minutesFromApplications.Value < 60m)
            {
                maxHourlyLoadingDaily[dayIndex] = dailyLoading;
            }
            else
            {
                maxHourlyLoadingDaily[dayIndex] = (dailyLoading.Value / minutesFromApplications.Value) * 60m;
            }
        }
    }

    private static int ParseFieldOrder(string? fieldId)
    {
        return int.TryParse(fieldId, out var parsed) ? parsed : int.MaxValue;
    }

    private void SyncDynamicFieldsFromLegacy(NDAR1 ndar1)
    {
        if (ndar1.Fields.Count > 0)
        {
            return;
        }

        AddLegacyField(ndar1, ndar1.Field1Id, 1, ndar1.Field1VolumeAppliedDaily, ndar1.Field1TimeIrrigatedDaily, ndar1.Field1DailyLoadingDaily, ndar1.Field1MaxHourlyLoadingDaily, ndar1.Field1MonthlyLoading, ndar1.Field1MaxHourlyLoading, ndar1.Field1TwelveMonthFloatingTotal);
        AddLegacyField(ndar1, ndar1.Field2Id, 2, ndar1.Field2VolumeAppliedDaily, ndar1.Field2TimeIrrigatedDaily, ndar1.Field2DailyLoadingDaily, ndar1.Field2MaxHourlyLoadingDaily, ndar1.Field2MonthlyLoading, ndar1.Field2MaxHourlyLoading, ndar1.Field2TwelveMonthFloatingTotal);
        AddLegacyField(ndar1, ndar1.Field3Id, 3, ndar1.Field3VolumeAppliedDaily, ndar1.Field3TimeIrrigatedDaily, ndar1.Field3DailyLoadingDaily, ndar1.Field3MaxHourlyLoadingDaily, ndar1.Field3MonthlyLoading, ndar1.Field3MaxHourlyLoading, ndar1.Field3TwelveMonthFloatingTotal);
        AddLegacyField(ndar1, ndar1.Field4Id, 4, ndar1.Field4VolumeAppliedDaily, ndar1.Field4TimeIrrigatedDaily, ndar1.Field4DailyLoadingDaily, ndar1.Field4MaxHourlyLoadingDaily, ndar1.Field4MonthlyLoading, ndar1.Field4MaxHourlyLoading, ndar1.Field4TwelveMonthFloatingTotal);
    }

    private static void AddLegacyField(
        NDAR1 ndar1,
        Guid? sprayfieldId,
        int order,
        List<decimal?> volumeDaily,
        List<decimal?> timeDaily,
        List<decimal?> loadingDaily,
        List<decimal?> maxHourlyDaily,
        decimal monthlyLoading,
        decimal maxHourlyLoading,
        decimal floatingTotal)
    {
        if (!sprayfieldId.HasValue)
        {
            return;
        }

        var field = new NDAR1Field
        {
            Id = Guid.NewGuid(),
            NDAR1Id = ndar1.Id,
            SprayfieldId = sprayfieldId.Value,
            FieldOrder = order,
            MonthlyLoading = monthlyLoading,
            MaxHourlyLoading = maxHourlyLoading,
            TwelveMonthFloatingTotal = floatingTotal
        };

        for (var day = 1; day <= 31; day++)
        {
            var idx = day - 1;
            field.DailyValues.Add(new NDAR1FieldDaily
            {
                Id = Guid.NewGuid(),
                DayNo = day,
                VolumeApplied = idx < volumeDaily.Count ? volumeDaily[idx] : null,
                TimeIrrigated = idx < timeDaily.Count ? timeDaily[idx] : null,
                DailyLoading = idx < loadingDaily.Count ? loadingDaily[idx] : null,
                MaxHourlyLoading = idx < maxHourlyDaily.Count ? maxHourlyDaily[idx] : null
            });
        }

        ndar1.Fields.Add(field);
    }

    private async Task CalculateRollingFloatingTotals(NDAR1 report, DateTime asOfDate)
    {
        report.Field1TwelveMonthFloatingTotal = await GetRollingHydraulicInchesAsync(report.FacilityId, report.Field1Id, asOfDate);
        report.Field2TwelveMonthFloatingTotal = await GetRollingHydraulicInchesAsync(report.FacilityId, report.Field2Id, asOfDate);
        report.Field3TwelveMonthFloatingTotal = await GetRollingHydraulicInchesAsync(report.FacilityId, report.Field3Id, asOfDate);
        report.Field4TwelveMonthFloatingTotal = await GetRollingHydraulicInchesAsync(report.FacilityId, report.Field4Id, asOfDate);
    }

    private async Task<decimal> GetRollingHydraulicInchesAsync(Guid facilityId, Guid? sprayfieldId, DateTime asOfDate)
    {
        if (!sprayfieldId.HasValue)
        {
            return 0m;
        }

        var metrics = await _applicationComplianceService.GetFieldRollingMetricsAsync(facilityId, sprayfieldId.Value, asOfDate);
        return metrics.RollingHydraulicInches;
    }

    private void InitializeDailyArrays(NDAR1 ndar1)
    {
        // Initialize all daily arrays with 31 null values
        while (ndar1.WeatherCodeDaily.Count < 31) ndar1.WeatherCodeDaily.Add(null);
        while (ndar1.TemperatureDaily.Count < 31) ndar1.TemperatureDaily.Add(null);
        while (ndar1.PrecipitationDaily.Count < 31) ndar1.PrecipitationDaily.Add(null);
        while (ndar1.FiveDayUpsetDaily.Count < 31) ndar1.FiveDayUpsetDaily.Add(null);

        while (ndar1.Field1VolumeAppliedDaily.Count < 31) ndar1.Field1VolumeAppliedDaily.Add(null);
        while (ndar1.Field1TimeIrrigatedDaily.Count < 31) ndar1.Field1TimeIrrigatedDaily.Add(null);
        while (ndar1.Field1DailyLoadingDaily.Count < 31) ndar1.Field1DailyLoadingDaily.Add(null);
        while (ndar1.Field1MaxHourlyLoadingDaily.Count < 31) ndar1.Field1MaxHourlyLoadingDaily.Add(null);

        while (ndar1.Field2VolumeAppliedDaily.Count < 31) ndar1.Field2VolumeAppliedDaily.Add(null);
        while (ndar1.Field2TimeIrrigatedDaily.Count < 31) ndar1.Field2TimeIrrigatedDaily.Add(null);
        while (ndar1.Field2DailyLoadingDaily.Count < 31) ndar1.Field2DailyLoadingDaily.Add(null);
        while (ndar1.Field2MaxHourlyLoadingDaily.Count < 31) ndar1.Field2MaxHourlyLoadingDaily.Add(null);

        while (ndar1.Field3VolumeAppliedDaily.Count < 31) ndar1.Field3VolumeAppliedDaily.Add(null);
        while (ndar1.Field3TimeIrrigatedDaily.Count < 31) ndar1.Field3TimeIrrigatedDaily.Add(null);
        while (ndar1.Field3DailyLoadingDaily.Count < 31) ndar1.Field3DailyLoadingDaily.Add(null);
        while (ndar1.Field3MaxHourlyLoadingDaily.Count < 31) ndar1.Field3MaxHourlyLoadingDaily.Add(null);

        while (ndar1.Field4VolumeAppliedDaily.Count < 31) ndar1.Field4VolumeAppliedDaily.Add(null);
        while (ndar1.Field4TimeIrrigatedDaily.Count < 31) ndar1.Field4TimeIrrigatedDaily.Add(null);
        while (ndar1.Field4DailyLoadingDaily.Count < 31) ndar1.Field4DailyLoadingDaily.Add(null);
        while (ndar1.Field4MaxHourlyLoadingDaily.Count < 31) ndar1.Field4MaxHourlyLoadingDaily.Add(null);
    }

    /// <summary>
    /// Exports NDAR-1 report to Excel format using the template file.
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(Guid id)
    {
        var report = await GetByIdAsync(id);
        if (report == null)
            throw new EntityNotFoundException(nameof(NDAR1), id);

        var facility = report.Facility;
        if (facility == null)
            throw new BusinessRuleException("Facility not found for this report.");

        var irrigationReport = await _context.IrrRprts
            .Where(i => i.FacilityId == facility.Id &&
                        i.Month == report.Month &&
                        i.Year == report.Year)
            .OrderByDescending(i => i.UpdatedDate)
            .FirstOrDefaultAsync();

        var operatorLogStorageByDate = await LoadOperatorLogStorageByDateAsync(facility.Id, report.Year, (int)report.Month);

        // Load template file
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "Non-Discharge Application Report (NDAR-1).xlsx");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        using var workbook = new XLWorkbook(templatePath);
        var worksheet = workbook.Worksheet(1);

        // Populate header information
        // Row 1 value cells: D1 (Permit No.), I1 (Facility Name), P1 (County), S1 (Month), V1 (Year)
        worksheet.Cell("D1").Value = facility.PermitNumber;
        worksheet.Cell("I1").Value = facility.Name;
        worksheet.Cell("P1").Value = facility.County;
        worksheet.Cell("S1").Value = report.Month.ToString();
        worksheet.Cell("V1").Value = report.Year;

        WriteFacilityIrrigationCheckbox(worksheet, report.DidIrrigationOccur);

        // Field information (Rows 2-7)
        // Field 1: Headers at G2-G7, Data at G-J (Volume, Time, Daily Loading, Max Hourly)
        // Field 2: Headers at K2-K7, Data at K-N
        // Field 3: Headers at O2-O7, Data at O-R
        // Field 4: Headers at S2-S7, Data at S-V
        var fields = new[] { report.Field1, report.Field2, report.Field3, report.Field4 };
        var fieldHeaderColumns = new[] { "G", "K", "O", "S" };
        var fieldValueColumns = new[]
        {
            ResolveFieldValueColumn(worksheet, "G", "J"),
            ResolveFieldValueColumn(worksheet, "K", "N"),
            ResolveFieldValueColumn(worksheet, "O", "R"),
            ResolveFieldValueColumn(worksheet, "S", "V")
        };

        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] != null)
            {
                var col = fieldValueColumns[i];
                worksheet.Cell($"{col}2").Value = fields[i].FieldId; // Field Name
                worksheet.Cell($"{col}3").Value = SprayfieldReportHelper.GetReportAcres(fields[i]); // Area (report acres)
                worksheet.Cell($"{col}4").Value = SprayfieldZoneSummaryHelper.GetCropSummary(fields[i]) ?? ""; // Cover Crop
                worksheet.Cell($"{col}5").Value = fields[i].HourlyRateInches; // Hourly Rate (in)
                worksheet.Cell($"{col}6").Value = fields[i].AnnualRateInches ?? fields[i].HydraulicLoadingLimitInPerYr; // Annual Rate (in/year)
            }
        }

        // Daily data (Rows 10-40, starting from row 10 for day 1)
        var daysInMonth = DateTime.DaysInMonth(report.Year, (int)report.Month);
        var startRow = 10;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var row = startRow + day - 1;
            var dayIndex = day - 1;

            // Day number
            worksheet.Cell($"A{row}").Value = day;

            // Weather data
            worksheet.Cell($"B{row}").Value = report.WeatherCodeDaily[dayIndex];
            worksheet.Cell($"C{row}").Value = report.TemperatureDaily[dayIndex];
            worksheet.Cell($"D{row}").Value = report.PrecipitationDaily[dayIndex];
            worksheet.Cell($"E{row}").Value = operatorLogStorageByDate.TryGetValue(new DateTime(report.Year, (int)report.Month, day), out var storageFt)
                ? storageFt
                : null;
            worksheet.Cell($"F{row}").Value = report.FiveDayUpsetDaily[dayIndex];

            // Field 1 data (Columns G-J: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field1Id.HasValue)
            {
                worksheet.Cell($"G{row}").Value = RoundWholeForDisplay(report.Field1VolumeAppliedDaily[dayIndex]);
                worksheet.Cell($"H{row}").Value = RoundWholeForDisplay(report.Field1TimeIrrigatedDaily[dayIndex]);
                // Daily Loading formula: =IF(ISBLANK(G{row})," ",G{row}/($G$3*27154))
                worksheet.Cell($"I{row}").FormulaA1 = $"=IF(ISBLANK(G{row}),\" \",G{row}/(${fieldValueColumns[0]}$3*27154))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(G{row}),ISBLANK(H{row}))," ",IF(H{row}<60,I{row},(I{row}/H{row})*60))
                worksheet.Cell($"J{row}").FormulaA1 = $"=IF(OR(ISBLANK(G{row}),ISBLANK(H{row})),\" \",IF(H{row}<60,I{row},(I{row}/H{row})*60))";
            }

            // Field 2 data (Columns K-N: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field2Id.HasValue)
            {
                worksheet.Cell($"K{row}").Value = RoundWholeForDisplay(report.Field2VolumeAppliedDaily[dayIndex]);
                worksheet.Cell($"L{row}").Value = RoundWholeForDisplay(report.Field2TimeIrrigatedDaily[dayIndex]);
                // Daily Loading formula: =IF(ISBLANK(K{row})," ",K{row}/($K$3*27154))
                worksheet.Cell($"M{row}").FormulaA1 = $"=IF(ISBLANK(K{row}),\" \",K{row}/(${fieldValueColumns[1]}$3*27154))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(K{row}),ISBLANK(L{row}))," ",IF(L{row}<60,M{row},(M{row}/L{row})*60))
                worksheet.Cell($"N{row}").FormulaA1 = $"=IF(OR(ISBLANK(K{row}),ISBLANK(L{row})),\" \",IF(L{row}<60,M{row},(M{row}/L{row})*60))";
            }

            // Field 3 data (Columns O-R: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field3Id.HasValue)
            {
                worksheet.Cell($"O{row}").Value = RoundWholeForDisplay(report.Field3VolumeAppliedDaily[dayIndex]);
                worksheet.Cell($"P{row}").Value = RoundWholeForDisplay(report.Field3TimeIrrigatedDaily[dayIndex]);
                // Daily Loading formula: =IF(ISBLANK(O{row})," ",O{row}/($O$3*27154))
                worksheet.Cell($"Q{row}").FormulaA1 = $"=IF(ISBLANK(O{row}),\" \",O{row}/(${fieldValueColumns[2]}$3*27154))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(O{row}),ISBLANK(P{row}))," ",IF(P{row}<60,Q{row},(Q{row}/P{row})*60))
                worksheet.Cell($"R{row}").FormulaA1 = $"=IF(OR(ISBLANK(O{row}),ISBLANK(P{row})),\" \",IF(P{row}<60,Q{row},(Q{row}/P{row})*60))";
            }

            // Field 4 data (Columns S-V: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field4Id.HasValue)
            {
                worksheet.Cell($"S{row}").Value = RoundWholeForDisplay(report.Field4VolumeAppliedDaily[dayIndex]);
                worksheet.Cell($"T{row}").Value = RoundWholeForDisplay(report.Field4TimeIrrigatedDaily[dayIndex]);
                // Daily Loading formula: =IF(ISBLANK(S{row})," ",S{row}/($S$3*27154))
                worksheet.Cell($"U{row}").FormulaA1 = $"=IF(ISBLANK(S{row}),\" \",S{row}/(${fieldValueColumns[3]}$3*27154))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(S{row}),ISBLANK(T{row}))," ",IF(T{row}<60,U{row},(U{row}/T{row})*60))
                worksheet.Cell($"V{row}").FormulaA1 = $"=IF(OR(ISBLANK(S{row}),ISBLANK(T{row})),\" \",IF(T{row}<60,U{row},(U{row}/T{row})*60))";
            }
        }

        // Monthly totals (Row 41) and 12-month floating totals (Row 42)
        var monthlyRow = 41;
        var floatingRow = 42;
        WriteFooterForLegacyFourFields(worksheet, monthlyRow, floatingRow, new[]
        {
            report.Field1Id.HasValue ? report.Field1VolumeAppliedDaily : null,
            report.Field2Id.HasValue ? report.Field2VolumeAppliedDaily : null,
            report.Field3Id.HasValue ? report.Field3VolumeAppliedDaily : null,
            report.Field4Id.HasValue ? report.Field4VolumeAppliedDaily : null
        },
        new[]
        {
            report.Field1Id.HasValue ? report.Field1TimeIrrigatedDaily : null,
            report.Field2Id.HasValue ? report.Field2TimeIrrigatedDaily : null,
            report.Field3Id.HasValue ? report.Field3TimeIrrigatedDaily : null,
            report.Field4Id.HasValue ? report.Field4TimeIrrigatedDaily : null
        },
        new[]
        {
            report.Field1Id.HasValue ? report.Field1MonthlyLoading : (decimal?)null,
            report.Field2Id.HasValue ? report.Field2MonthlyLoading : (decimal?)null,
            report.Field3Id.HasValue ? report.Field3MonthlyLoading : (decimal?)null,
            report.Field4Id.HasValue ? report.Field4MonthlyLoading : (decimal?)null
        },
        new[]
        {
            report.Field1Id.HasValue ? report.Field1MaxHourlyLoading : (decimal?)null,
            report.Field2Id.HasValue ? report.Field2MaxHourlyLoading : (decimal?)null,
            report.Field3Id.HasValue ? report.Field3MaxHourlyLoading : (decimal?)null,
            report.Field4Id.HasValue ? report.Field4MaxHourlyLoading : (decimal?)null
        },
        new[]
        {
            report.Field1Id.HasValue ? report.Field1TwelveMonthFloatingTotal : (decimal?)null,
            report.Field2Id.HasValue ? report.Field2TwelveMonthFloatingTotal : (decimal?)null,
            report.Field3Id.HasValue ? report.Field3TwelveMonthFloatingTotal : (decimal?)null,
            report.Field4Id.HasValue ? report.Field4TwelveMonthFloatingTotal : (decimal?)null
        });

        WriteFieldIrrigatedCheckboxes(worksheet, BuildFieldIrrigatedFlagsFromLegacy(report));
        RemoveInstructionalHighlights(worksheet);

        RemovePreExistingDynamicNdarSheets(workbook);

        var exportFields = BuildExportFields(report);
        if (exportFields.Count > 4)
        {
            var chunkIndex = 1;
            for (var offset = 4; offset < exportFields.Count; offset += 4)
            {
                var chunk = exportFields.Skip(offset).Take(4).ToList();
                var extraSheetName = $"NDAR-1 ({chunkIndex + 1})";
                var extraSheet = worksheet.CopyTo(extraSheetName);
                WriteNdarSheetChunk(extraSheet, facility, report, chunk, operatorLogStorageByDate);
                chunkIndex++;
            }
        }

        WriteCertificationPage(workbook, facility, irrigationReport?.ComplianceStatus);
        MoveSheetToEndByName(workbook, "Certification Page");
        MoveSheetToEndByName(workbook, "Formulas & Weather Codes");

        // Convert to byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string ResolveFieldValueColumn(IXLWorksheet worksheet, string blockStartColumn, string blockEndColumn)
    {
        var start = XLHelper.GetColumnNumberFromLetter(blockStartColumn);
        var end = XLHelper.GetColumnNumberFromLetter(blockEndColumn);

        for (var colNum = start; colNum <= end; colNum++)
        {
            var col = XLHelper.GetColumnLetterFromNumber(colNum);
            var cell = worksheet.Cell($"{col}2");
            var isEmpty = string.IsNullOrWhiteSpace(cell.GetString());

            if (!isEmpty)
            {
                continue;
            }

            if (!cell.IsMerged())
            {
                return col;
            }

            var merged = cell.MergedRange();
            if (merged != null && merged.FirstCell().Address.Equals(cell.Address))
            {
                return col;
            }
        }

        // Fallback to right edge of the field block (typically value column in legacy templates).
        return blockEndColumn;
    }

    private static List<NdarExportField> BuildExportFields(NDAR1 report)
    {
        if (report.Fields.Any())
        {
            return report.Fields
                .OrderBy(f => f.FieldOrder)
                .Select(f => new NdarExportField
                {
                    Sprayfield = f.Sprayfield,
                    Volume = To31(f.DailyValues.OrderBy(d => d.DayNo).Select(d => d.VolumeApplied).ToList()),
                    Time = To31(f.DailyValues.OrderBy(d => d.DayNo).Select(d => d.TimeIrrigated).ToList()),
                    DailyLoading = To31(f.DailyValues.OrderBy(d => d.DayNo).Select(d => d.DailyLoading).ToList()),
                    MaxHourly = To31(f.DailyValues.OrderBy(d => d.DayNo).Select(d => d.MaxHourlyLoading).ToList()),
                    MonthlyLoading = f.MonthlyLoading,
                    MaxHourlyLoading = f.MaxHourlyLoading,
                    FloatingTotal = f.TwelveMonthFloatingTotal
                })
                .Where(x => x.Sprayfield != null)
                .ToList();
        }

        return new List<NdarExportField>
        {
            new() { Sprayfield = report.Field1, Volume = To31(report.Field1VolumeAppliedDaily), Time = To31(report.Field1TimeIrrigatedDaily), DailyLoading = To31(report.Field1DailyLoadingDaily), MaxHourly = To31(report.Field1MaxHourlyLoadingDaily), MonthlyLoading = report.Field1MonthlyLoading, MaxHourlyLoading = report.Field1MaxHourlyLoading, FloatingTotal = report.Field1TwelveMonthFloatingTotal },
            new() { Sprayfield = report.Field2, Volume = To31(report.Field2VolumeAppliedDaily), Time = To31(report.Field2TimeIrrigatedDaily), DailyLoading = To31(report.Field2DailyLoadingDaily), MaxHourly = To31(report.Field2MaxHourlyLoadingDaily), MonthlyLoading = report.Field2MonthlyLoading, MaxHourlyLoading = report.Field2MaxHourlyLoading, FloatingTotal = report.Field2TwelveMonthFloatingTotal },
            new() { Sprayfield = report.Field3, Volume = To31(report.Field3VolumeAppliedDaily), Time = To31(report.Field3TimeIrrigatedDaily), DailyLoading = To31(report.Field3DailyLoadingDaily), MaxHourly = To31(report.Field3MaxHourlyLoadingDaily), MonthlyLoading = report.Field3MonthlyLoading, MaxHourlyLoading = report.Field3MaxHourlyLoading, FloatingTotal = report.Field3TwelveMonthFloatingTotal },
            new() { Sprayfield = report.Field4, Volume = To31(report.Field4VolumeAppliedDaily), Time = To31(report.Field4TimeIrrigatedDaily), DailyLoading = To31(report.Field4DailyLoadingDaily), MaxHourly = To31(report.Field4MaxHourlyLoadingDaily), MonthlyLoading = report.Field4MonthlyLoading, MaxHourlyLoading = report.Field4MaxHourlyLoading, FloatingTotal = report.Field4TwelveMonthFloatingTotal }
        }.Where(x => x.Sprayfield != null).ToList();
    }

    private static List<decimal?> To31(List<decimal?> values)
    {
        var copy = values.Take(31).ToList();
        while (copy.Count < 31)
        {
            copy.Add(null);
        }

        return copy;
    }

    private static decimal? RoundWholeForDisplay(decimal? value)
    {
        return value.HasValue
            ? Math.Round(value.Value, 0, MidpointRounding.AwayFromZero)
            : null;
    }

    private static void WriteNdarSheetChunk(
        IXLWorksheet worksheet,
        Facility facility,
        NDAR1 report,
        List<NdarExportField> chunk,
        IReadOnlyDictionary<DateTime, decimal?> operatorLogStorageByDate)
    {
        worksheet.Cell("D1").Value = facility.PermitNumber;
        worksheet.Cell("I1").Value = facility.Name;
        worksheet.Cell("P1").Value = facility.County;
        worksheet.Cell("S1").Value = report.Month.ToString();
        worksheet.Cell("V1").Value = report.Year;
        WriteFacilityIrrigationCheckbox(worksheet, report.DidIrrigationOccur);

        var fieldValueColumns = new[] { "I", "M", "Q", "U" };
        var dataColumnSets = new[] { ("G","H","I","J"), ("K","L","M","N"), ("O","P","Q","R"), ("S","T","U","V") };
        var monthlyRow = 41;
        var floatingRow = 42;

        // Always clear all 4 header blocks first so cloned sheets do not keep stale template values (1,2,3,4).
        for (int i = 0; i < 4; i++)
        {
            var col = fieldValueColumns[i];
            worksheet.Cell($"{col}2").Value = string.Empty;
            worksheet.Cell($"{col}3").Value = string.Empty;
            worksheet.Cell($"{col}4").Value = string.Empty;
            worksheet.Cell($"{col}5").Value = string.Empty;
            worksheet.Cell($"{col}6").Value = string.Empty;
        }

        // Always clear all 4 data/footer blocks first so leftover template formulas/values don't leak into empty field sections.
        for (int i = 0; i < 4; i++)
        {
            var cols = dataColumnSets[i];
            worksheet.Range($"{cols.Item1}10:{cols.Item4}40").Clear(XLClearOptions.Contents);
            worksheet.Range($"{cols.Item1}{monthlyRow}:{cols.Item4}{floatingRow}").Clear(XLClearOptions.Contents);
        }

        for (int i = 0; i < 4; i++)
        {
            if (i >= chunk.Count)
            {
                continue;
            }

            var f = chunk[i];
            var col = fieldValueColumns[i];
            worksheet.Cell($"{col}2").Value = f.Sprayfield?.FieldId ?? string.Empty;
            worksheet.Cell($"{col}3").Value = f.Sprayfield != null ? SprayfieldReportHelper.GetReportAcres(f.Sprayfield) : 0m;
            worksheet.Cell($"{col}4").Value = f.Sprayfield != null ? SprayfieldZoneSummaryHelper.GetCropSummary(f.Sprayfield) ?? "" : "";
            worksheet.Cell($"{col}5").Value = f.Sprayfield?.HourlyRateInches;
            worksheet.Cell($"{col}6").Value = (f.Sprayfield?.AnnualRateInches ?? f.Sprayfield?.HydraulicLoadingLimitInPerYr);
        }

        var daysInMonth = DateTime.DaysInMonth(report.Year, (int)report.Month);
        for (int day = 1; day <= daysInMonth; day++)
        {
            var row = 9 + day;
            var dayIndex = day - 1;
            worksheet.Cell($"A{row}").Value = day;
            worksheet.Cell($"B{row}").Value = report.WeatherCodeDaily[dayIndex];
            worksheet.Cell($"C{row}").Value = report.TemperatureDaily[dayIndex];
            worksheet.Cell($"D{row}").Value = report.PrecipitationDaily[dayIndex];
            worksheet.Cell($"E{row}").Value = operatorLogStorageByDate.TryGetValue(new DateTime(report.Year, (int)report.Month, day), out var storageFt)
                ? storageFt
                : null;
            worksheet.Cell($"F{row}").Value = report.FiveDayUpsetDaily[dayIndex];

            for (int i = 0; i < 4; i++)
            {
                if (i >= chunk.Count) continue;
                var f = chunk[i];
                var cols = dataColumnSets[i];
                worksheet.Cell($"{cols.Item1}{row}").Value = RoundWholeForDisplay(f.Volume[dayIndex]);
                worksheet.Cell($"{cols.Item2}{row}").Value = RoundWholeForDisplay(f.Time[dayIndex]);
                worksheet.Cell($"{cols.Item3}{row}").Value = f.DailyLoading[dayIndex];
                worksheet.Cell($"{cols.Item4}{row}").Value = f.MaxHourly[dayIndex];
            }
        }

        WriteFooterForChunk(worksheet, monthlyRow, floatingRow, chunk);
        WriteFieldIrrigatedCheckboxes(worksheet, BuildFieldIrrigatedFlagsFromChunk(chunk));
        RemoveInstructionalHighlights(worksheet);
    }

    private static void WriteFacilityIrrigationCheckbox(IXLWorksheet worksheet, bool didIrrigate)
    {
        worksheet.Cell("A5").Value = didIrrigate ? "\u2611 Yes" : "\u2610 Yes";
        worksheet.Cell("D5").Value = didIrrigate ? "\u2610 No" : "\u2611 No";
    }

    private static void WriteFieldIrrigatedCheckboxes(IXLWorksheet worksheet, IReadOnlyList<bool> fieldIrrigatedFlags)
    {
        var yesNoPairs = new[] { ("I7", "J7"), ("M7", "N7"), ("Q7", "R7"), ("U7", "V7") };
        for (var i = 0; i < yesNoPairs.Length; i++)
        {
            var (yesCell, noCell) = yesNoPairs[i];
            var didIrrigate = i < fieldIrrigatedFlags.Count && fieldIrrigatedFlags[i];
            worksheet.Cell(yesCell).Value = didIrrigate ? "\u2611 Yes" : "\u2610 Yes";
            worksheet.Cell(noCell).Value = didIrrigate ? "\u2610 No" : "\u2611 No";
        }
    }

    private static bool[] BuildFieldIrrigatedFlagsFromLegacy(NDAR1 report)
    {
        return new[]
        {
            HasAnyDayData(report.Field1VolumeAppliedDaily, report.Field1TimeIrrigatedDaily),
            HasAnyDayData(report.Field2VolumeAppliedDaily, report.Field2TimeIrrigatedDaily),
            HasAnyDayData(report.Field3VolumeAppliedDaily, report.Field3TimeIrrigatedDaily),
            HasAnyDayData(report.Field4VolumeAppliedDaily, report.Field4TimeIrrigatedDaily)
        };
    }

    private static bool[] BuildFieldIrrigatedFlagsFromChunk(List<NdarExportField> chunk)
    {
        var flags = new bool[4];
        for (var i = 0; i < chunk.Count && i < 4; i++)
        {
            flags[i] = HasAnyDayData(chunk[i].Volume, chunk[i].Time);
        }

        return flags;
    }

    private static bool HasAnyDayData(List<decimal?>? volumeDaily, List<decimal?>? timeDaily)
    {
        var hasVolume = volumeDaily?.Any(v => v.HasValue && v.Value > 0m) == true;
        var hasTime = timeDaily?.Any(v => v.HasValue && v.Value > 0m) == true;
        return hasVolume || hasTime;
    }

    private static void WriteFooterForLegacyFourFields(
        IXLWorksheet worksheet,
        int monthlyRow,
        int floatingRow,
        List<decimal?>?[] volumeDailyByField,
        List<decimal?>?[] timeDailyByField,
        decimal?[] monthlyLoadingByField,
        decimal?[] monthlyMaxByField,
        decimal?[] floatingTotalByField)
    {
        var fieldColumns = new[]
        {
            ("G", "H", "I", "J"),
            ("K", "L", "M", "N"),
            ("O", "P", "Q", "R"),
            ("S", "T", "U", "V")
        };

        for (var i = 0; i < 4; i++)
        {
            var hasField = monthlyLoadingByField[i].HasValue || monthlyMaxByField[i].HasValue || floatingTotalByField[i].HasValue;
            if (!hasField)
            {
                continue;
            }

            var (volumeCol, timeCol, dailyCol, maxCol) = fieldColumns[i];
            worksheet.Cell($"{volumeCol}{monthlyRow}").Value = SumWhole(volumeDailyByField[i]);
            worksheet.Cell($"{timeCol}{monthlyRow}").Value = SumWhole(timeDailyByField[i]);
            worksheet.Cell($"{dailyCol}{monthlyRow}").Value = monthlyLoadingByField[i].Value;
            worksheet.Cell($"{maxCol}{monthlyRow}").Value = monthlyMaxByField[i].Value;

            worksheet.Cell($"{volumeCol}{floatingRow}").Value = floatingTotalByField[i].Value;
            worksheet.Cell($"{timeCol}{floatingRow}").Value = floatingTotalByField[i].Value;
            worksheet.Cell($"{dailyCol}{floatingRow}").Value = floatingTotalByField[i].Value;
            worksheet.Cell($"{maxCol}{floatingRow}").Value = floatingTotalByField[i].Value;
        }

        ApplyFooterNumberFormatting(worksheet, monthlyRow, floatingRow);
    }

    private static void WriteFooterForChunk(IXLWorksheet worksheet, int monthlyRow, int floatingRow, List<NdarExportField> chunk)
    {
        var fieldColumns = new[]
        {
            ("G", "H", "I", "J"),
            ("K", "L", "M", "N"),
            ("O", "P", "Q", "R"),
            ("S", "T", "U", "V")
        };

        for (var i = 0; i < chunk.Count && i < 4; i++)
        {
            var (volumeCol, timeCol, dailyCol, maxCol) = fieldColumns[i];
            worksheet.Cell($"{volumeCol}{monthlyRow}").Value = SumWhole(chunk[i].Volume);
            worksheet.Cell($"{timeCol}{monthlyRow}").Value = SumWhole(chunk[i].Time);
            worksheet.Cell($"{dailyCol}{monthlyRow}").Value = chunk[i].MonthlyLoading;
            worksheet.Cell($"{maxCol}{monthlyRow}").Value = chunk[i].MaxHourlyLoading;

            worksheet.Cell($"{volumeCol}{floatingRow}").Value = chunk[i].FloatingTotal;
            worksheet.Cell($"{timeCol}{floatingRow}").Value = chunk[i].FloatingTotal;
            worksheet.Cell($"{dailyCol}{floatingRow}").Value = chunk[i].FloatingTotal;
            worksheet.Cell($"{maxCol}{floatingRow}").Value = chunk[i].FloatingTotal;
        }

        ApplyFooterNumberFormatting(worksheet, monthlyRow, floatingRow);
    }

    private static decimal SumWhole(List<decimal?>? values)
    {
        if (values == null)
        {
            return 0m;
        }

        return values.Where(v => v.HasValue).Sum(v => Math.Round(v!.Value, 0, MidpointRounding.AwayFromZero));
    }

    private static void RemoveInstructionalHighlights(IXLWorksheet worksheet)
    {
        foreach (var rangeAddress in new[] { "A5:F6", "A41:V42" })
        {
            var range = worksheet.Range(rangeAddress);
            range.Style.Fill.BackgroundColor = XLColor.NoColor;
            range.Style.Fill.PatternType = XLFillPatternValues.None;
        }
    }

    private static void ApplyFooterNumberFormatting(IXLWorksheet worksheet, int monthlyRow, int floatingRow)
    {
        var range = worksheet.Range($"G{monthlyRow}:V{floatingRow}");
        range.Style.NumberFormat.Format = "0.00";
        range.Style.Alignment.ShrinkToFit = true;
        worksheet.Cell("G41").Style.NumberFormat.Format = "0.00";
        worksheet.Cell("G42").Style.NumberFormat.Format = "0.00";
    }


    private async Task<Dictionary<DateTime, decimal?>> LoadOperatorLogStorageByDateAsync(Guid facilityId, int year, int month)
    {
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        var logs = await _context.OperatorLogs
            .AsNoTracking()
            .Where(o => o.FacilityId == facilityId
                        && !o.IsDeleted
                        && o.LogDate >= startDate
                        && o.LogDate < endDate)
            .OrderByDescending(o => o.UpdatedDate ?? o.CreatedDate)
            .ThenByDescending(o => o.CreatedDate)
            .ToListAsync();

        var dailyStorage = new Dictionary<DateTime, decimal?>();
        foreach (var log in logs)
        {
            var date = log.LogDate.Date;
            if (dailyStorage.ContainsKey(date))
            {
                continue;
            }

            dailyStorage[date] = log.StorageFt;
        }

        return dailyStorage;
    }

    private sealed class NdarExportField
    {
        public Sprayfield? Sprayfield { get; set; }
        public List<decimal?> Volume { get; set; } = new();
        public List<decimal?> Time { get; set; } = new();
        public List<decimal?> DailyLoading { get; set; } = new();
        public List<decimal?> MaxHourly { get; set; } = new();
        public decimal MonthlyLoading { get; set; }
        public decimal MaxHourlyLoading { get; set; }
        public decimal FloatingTotal { get; set; }
    }

    private static void WriteCertificationPage(IXLWorkbook workbook, Facility facility, ComplianceStatusEnum? complianceStatus)
    {
        var certificationWorksheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, "Certification Page", StringComparison.OrdinalIgnoreCase));

        if (certificationWorksheet == null && workbook.Worksheets.Count >= 2)
        {
            certificationWorksheet = workbook.Worksheet(2);
        }

        if (certificationWorksheet == null)
        {
            return;
        }

        WriteFacilityStatusComplianceRows(certificationWorksheet, complianceStatus);

        certificationWorksheet.Cell("C10").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("E11").Value = facility.OperatorNumber ?? string.Empty;
        certificationWorksheet.Cell("C12").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("I12").Value = facility.OperatorPhone ?? string.Empty;
        certificationWorksheet.Cell("A13").Value = $"Has the ORC changed since the previous NDAR-1? {(facility.ChangeInOrc == true ? "Yes" : "No")}";
        certificationWorksheet.Cell("K14").Value = DateTime.Today.ToString("MM/dd/yyyy");

        // Permittee certification section
        certificationWorksheet.Cell("O10").Value = facility.Permittee ?? string.Empty;
        certificationWorksheet.Cell("O11").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("P12").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("O13").Value = facility.PermitPhone ?? string.Empty;
        certificationWorksheet.Cell("T13").Value = facility.PermitExpirationDate?.ToString("MM/dd/yyyy") ?? string.Empty;
        certificationWorksheet.Cell("U14").Value = DateTime.Today.ToString("MM/dd/yyyy");
    }

    private static void RemovePreExistingDynamicNdarSheets(IXLWorkbook workbook)
    {
        var staleSheets = workbook.Worksheets
            .Where(ws => Regex.IsMatch(ws.Name, @"^NDAR-1 \(\d+\)$", RegexOptions.IgnoreCase))
            .ToList();

        foreach (var sheet in staleSheets)
        {
            sheet.Delete();
        }
    }

    private static void MoveSheetToEndByName(IXLWorkbook workbook, string sheetName)
    {
        var sheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase));

        if (sheet == null)
        {
            return;
        }

        sheet.Position = workbook.Worksheets.Count;
    }

    private static void WriteFacilityStatusComplianceRows(IXLWorksheet certificationWorksheet, ComplianceStatusEnum? complianceStatus)
    {
        var statusOptionText = complianceStatus switch
        {
            ComplianceStatusEnum.Compliant => "\u2611 Compliant    \u2610 Non-Compliant",
            ComplianceStatusEnum.NonCompliant => "\u2610 Compliant    \u2611 Non-Compliant",
            _ => "\u2610 Compliant    \u2610 Non-Compliant"
        };

        var rowAddresses = new[] { "A1", "A2", "A3", "A4", "A5" };
        var defaultQuestionText = new[]
        {
            "Did the application rates exceed the limits in Attachment B of your permit?",
            "Were adequate measures taken to prevent effluent ponding in or runoff from the sites?",
            "Was a suitable vegetative cover maintained on all sites as specified in your permit?",
            "Were all setbacks listed in your permit maintained for every application to each permitted site ?",
            "Were all freeboards maintained in accordance with the specified freeboard heights in your permit?"
        };

        for (var i = 0; i < rowAddresses.Length; i++)
        {
            var rowCell = certificationWorksheet.Cell(rowAddresses[i]);
            var questionText = rowCell.GetString().Trim();
            if (string.IsNullOrWhiteSpace(questionText))
            {
                questionText = defaultQuestionText[i];
            }

            var richText = rowCell.GetRichText();
            richText.ClearText();
            richText.AddText($"{questionText}    ");
            richText.AddText(statusOptionText)
                .SetFontSize(10)
                .SetBold(false);
        }
    }
}




using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Utilities;
using System.Text.Json;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for NDAR1 (Non-Discharge Application Report) entity operations.
/// </summary>
public class NDAR1Service : INDAR1Service
{
    private const decimal GALLONS_PER_ACRE_INCH = 27152m;
    private const string NdarPayloadPrefix = "NDAR_JSON:";

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
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(n => n.FacilityId == facilityId.Value);
        }

        return await query
            .OrderByDescending(n => n.Year)
            .ThenByDescending(n => n.Month)
            .ToListAsync();
    }

    public async Task<NDAR1?> GetByIdAsync(Guid id)
    {
        return await _context.NDAR1s
            .Include(n => n.Company)
            .Include(n => n.Facility)
            .Include(n => n.Field1)
                .ThenInclude(f => f.ApplicationZones)
                    .ThenInclude(z => z.Crop)
            .Include(n => n.Field2)
                .ThenInclude(f => f.ApplicationZones)
                    .ThenInclude(z => z.Crop)
            .Include(n => n.Field3)
                .ThenInclude(f => f.ApplicationZones)
                    .ThenInclude(z => z.Crop)
            .Include(n => n.Field4)
                .ThenInclude(f => f.ApplicationZones)
                    .ThenInclude(z => z.Crop)
            .FirstOrDefaultAsync(n => n.Id == id);
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
        existing.DidIrrigationOccur = ndar1.DidIrrigationOccur;
        existing.WeatherCodeDaily = ndar1.WeatherCodeDaily;
        existing.TemperatureDaily = ndar1.TemperatureDaily;
        existing.PrecipitationDaily = ndar1.PrecipitationDaily;
        existing.StorageDaily = ndar1.StorageDaily;
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
            .Include(a => a.Zone)
                .ThenInclude(z => z!.Sprayfield)
            .Where(a => a.FacilityId == facilityId &&
                       a.ApplicationDate >= startDate &&
                       a.ApplicationDate <= endDate)
            .ToListAsync();

        // Get sprayfields for this facility
        var sprayfields = await _sprayfieldService.GetByFacilityIdAsync(facilityId);
        var sprayfieldList = sprayfields.Take(4).ToList(); // Limit to 4 fields
        var sprayfieldById = sprayfieldList.ToDictionary(s => s.Id, s => s);

        // Get operator logs for weather data
        var operatorLogs = await _context.OperatorLogs
            .Where(o => o.FacilityId == facilityId &&
                       o.LogDate >= startDate &&
                       o.LogDate <= endDate)
            .ToListAsync();

        var volumeOverridesByDay = new Dictionary<int, Dictionary<string, decimal?>>();
        var minutesOverridesByDay = new Dictionary<int, Dictionary<string, decimal?>>();
        var floatingOverridesByFieldCode = new Dictionary<string, decimal?>();
        var hasNdarPayloadValues = false;

        // Initialize the report
        var report = new NDAR1
        {
            CompanyId = facility.CompanyId,
            FacilityId = facilityId,
            Month = (MonthEnum)month,
            Year = year,
            DidIrrigationOccur = applications.Any()
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

            if (logForDay != null && TryParseNdarPayload(logForDay.NextShiftNotes, out var payload))
            {
                hasNdarPayloadValues = true;

                if (!string.IsNullOrWhiteSpace(payload.WeatherCode))
                {
                    report.WeatherCodeDaily[dayIndex] = payload.WeatherCode;
                }

                report.TemperatureDaily[dayIndex] = payload.Temperature;
                report.PrecipitationDaily[dayIndex] = payload.Precipitation;
                report.StorageDaily[dayIndex] = payload.Storage;
                report.FiveDayUpsetDaily[dayIndex] = payload.FiveDayUpset;

                if (payload.VolumesByFieldCode is not null && payload.VolumesByFieldCode.Count > 0)
                {
                    volumeOverridesByDay[dayIndex] = payload.VolumesByFieldCode;
                }

                if (payload.MinutesByFieldCode is not null && payload.MinutesByFieldCode.Count > 0)
                {
                    minutesOverridesByDay[dayIndex] = payload.MinutesByFieldCode;
                }

                if (payload.FloatingTotalsByFieldCode is not null)
                {
                    foreach (var kvp in payload.FloatingTotalsByFieldCode)
                    {
                        floatingOverridesByFieldCode[kvp.Key] = kvp.Value;
                    }
                }

                if (payload.FloatingField1.HasValue) floatingOverridesByFieldCode["1"] = payload.FloatingField1.Value;
                if (payload.FloatingField2.HasValue) floatingOverridesByFieldCode["2"] = payload.FloatingField2.Value;
                if (payload.FloatingField3.HasValue) floatingOverridesByFieldCode["3"] = payload.FloatingField3.Value;
                if (payload.FloatingField4.HasValue) floatingOverridesByFieldCode["4"] = payload.FloatingField4.Value;
            }
        }

        // Aggregate daily application data by field
        var fieldApplications = new Dictionary<Guid, List<MonthlyApplication>>();
        foreach (var application in applications)
        {
            var sprayfieldId = application.Zone?.SprayfieldId;
            if (!sprayfieldId.HasValue)
            {
                continue;
            }

            if (!fieldApplications.ContainsKey(sprayfieldId.Value))
            {
                fieldApplications[sprayfieldId.Value] = new List<MonthlyApplication>();
            }

            fieldApplications[sprayfieldId.Value].Add(application);
        }

        // Process each field
        ProcessFieldData(report.Field1Id, sprayfieldById, fieldApplications, volumeOverridesByDay, minutesOverridesByDay, report.Field1VolumeAppliedDaily,
            report.Field1TimeIrrigatedDaily, report.Field1DailyLoadingDaily,
            report.Field1MaxHourlyLoadingDaily, startDate, daysInMonth, hasNdarPayloadValues);
        ProcessFieldData(report.Field2Id, sprayfieldById, fieldApplications, volumeOverridesByDay, minutesOverridesByDay, report.Field2VolumeAppliedDaily,
            report.Field2TimeIrrigatedDaily, report.Field2DailyLoadingDaily,
            report.Field2MaxHourlyLoadingDaily, startDate, daysInMonth, hasNdarPayloadValues);
        ProcessFieldData(report.Field3Id, sprayfieldById, fieldApplications, volumeOverridesByDay, minutesOverridesByDay, report.Field3VolumeAppliedDaily,
            report.Field3TimeIrrigatedDaily, report.Field3DailyLoadingDaily,
            report.Field3MaxHourlyLoadingDaily, startDate, daysInMonth, hasNdarPayloadValues);
        ProcessFieldData(report.Field4Id, sprayfieldById, fieldApplications, volumeOverridesByDay, minutesOverridesByDay, report.Field4VolumeAppliedDaily,
            report.Field4TimeIrrigatedDaily, report.Field4DailyLoadingDaily,
            report.Field4MaxHourlyLoadingDaily, startDate, daysInMonth, hasNdarPayloadValues);

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
        ApplyFloatingTotalOverrides(report, sprayfieldById, floatingOverridesByFieldCode);

        return report;
    }

    private void ProcessFieldData(
        Guid? fieldId,
        Dictionary<Guid, Sprayfield> sprayfieldById,
        Dictionary<Guid, List<MonthlyApplication>> fieldApplications,
        Dictionary<int, Dictionary<string, decimal?>> volumeOverridesByDay,
        Dictionary<int, Dictionary<string, decimal?>> minutesOverridesByDay,
        List<decimal?> volumeDaily,
        List<decimal?> timeDaily,
        List<decimal?> loadingDaily,
        List<decimal?> maxHourlyLoadingDaily,
        DateTime startDate,
        int daysInMonth,
        bool usePayloadAsAuthoritative)
    {
        if (!fieldId.HasValue || !sprayfieldById.TryGetValue(fieldId.Value, out var sprayfield))
            return;

        var applications = fieldApplications.TryGetValue(fieldId.Value, out var appList)
            ? appList
            : new List<MonthlyApplication>();
        var fieldCode = sprayfield.FieldId;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, day);
            var dayIndex = day - 1;
            var dayApplications = applications.Where(i => i.ApplicationDate.Date == currentDate.Date).ToList();

            var volumeFromApplications = dayApplications.Any()
                ? dayApplications.Sum(i => i.VolumeGallons)
                : (decimal?)null;

            decimal? overrideVolume = null;
            if (volumeOverridesByDay.TryGetValue(dayIndex, out var dayVolumeMap) &&
                !string.IsNullOrWhiteSpace(fieldCode) &&
                dayVolumeMap.TryGetValue(fieldCode, out var mappedVolume))
            {
                overrideVolume = mappedVolume;
            }

            decimal? overrideMinutes = null;
            if (minutesOverridesByDay.TryGetValue(dayIndex, out var dayMinutesMap) &&
                !string.IsNullOrWhiteSpace(fieldCode) &&
                dayMinutesMap.TryGetValue(fieldCode, out var mappedMinutes))
            {
                overrideMinutes = mappedMinutes;
            }

            var hasVolume = usePayloadAsAuthoritative
                ? overrideVolume.HasValue
                : (overrideVolume.HasValue || volumeFromApplications.HasValue);
            if (!hasVolume)
            {
                continue;
            }

            var areaAcres = SprayfieldReportHelper.GetReportAcres(sprayfield);
            var volumeApplied = overrideVolume ?? volumeFromApplications!.Value;
            volumeDaily[dayIndex] = volumeApplied;
            timeDaily[dayIndex] = overrideMinutes;

            if (areaAcres > 0)
            {
                loadingDaily[dayIndex] = volumeApplied / (areaAcres * GALLONS_PER_ACRE_INCH);
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
            else if (!overrideMinutes.HasValue || overrideMinutes.Value <= 0 || overrideMinutes.Value < 60m)
            {
                maxHourlyLoadingDaily[dayIndex] = dailyLoading;
            }
            else
            {
                maxHourlyLoadingDaily[dayIndex] = (dailyLoading.Value / overrideMinutes.Value) * 60m;
            }
        }
    }
    private static bool TryParseNdarPayload(string? nextShiftNotes, out NdarDailyPayload payload)
    {
        payload = new NdarDailyPayload();

        if (string.IsNullOrWhiteSpace(nextShiftNotes))
        {
            return false;
        }

        if (!nextShiftNotes.StartsWith(NdarPayloadPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var json = nextShiftNotes.Substring(NdarPayloadPrefix.Length).Trim();
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<NdarDailyPayload>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is null)
            {
                return false;
            }

            payload = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed class NdarDailyPayload
    {
        public string? WeatherCode { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Precipitation { get; set; }
        public decimal? Storage { get; set; }
        public decimal? FiveDayUpset { get; set; }
        public Dictionary<string, decimal?>? VolumesByFieldCode { get; set; }
        public Dictionary<string, decimal?>? MinutesByFieldCode { get; set; }
        public Dictionary<string, decimal?>? FloatingTotalsByFieldCode { get; set; }
        public decimal? FloatingField1 { get; set; }
        public decimal? FloatingField2 { get; set; }
        public decimal? FloatingField3 { get; set; }
        public decimal? FloatingField4 { get; set; }
    }

    private static void ApplyFloatingTotalOverrides(
        NDAR1 report,
        Dictionary<Guid, Sprayfield> sprayfieldById,
        Dictionary<string, decimal?> floatingOverridesByFieldCode)
    {
        if (floatingOverridesByFieldCode.Count == 0)
        {
            return;
        }

        if (report.Field1Id.HasValue &&
            sprayfieldById.TryGetValue(report.Field1Id.Value, out var field1) &&
            !string.IsNullOrWhiteSpace(field1.FieldId) &&
            floatingOverridesByFieldCode.TryGetValue(field1.FieldId, out var field1Floating) &&
            field1Floating.HasValue)
        {
            report.Field1TwelveMonthFloatingTotal = field1Floating.Value;
        }

        if (report.Field2Id.HasValue &&
            sprayfieldById.TryGetValue(report.Field2Id.Value, out var field2) &&
            !string.IsNullOrWhiteSpace(field2.FieldId) &&
            floatingOverridesByFieldCode.TryGetValue(field2.FieldId, out var field2Floating) &&
            field2Floating.HasValue)
        {
            report.Field2TwelveMonthFloatingTotal = field2Floating.Value;
        }

        if (report.Field3Id.HasValue &&
            sprayfieldById.TryGetValue(report.Field3Id.Value, out var field3) &&
            !string.IsNullOrWhiteSpace(field3.FieldId) &&
            floatingOverridesByFieldCode.TryGetValue(field3.FieldId, out var field3Floating) &&
            field3Floating.HasValue)
        {
            report.Field3TwelveMonthFloatingTotal = field3Floating.Value;
        }

        if (report.Field4Id.HasValue &&
            sprayfieldById.TryGetValue(report.Field4Id.Value, out var field4) &&
            !string.IsNullOrWhiteSpace(field4.FieldId) &&
            floatingOverridesByFieldCode.TryGetValue(field4.FieldId, out var field4Floating) &&
            field4Floating.HasValue)
        {
            report.Field4TwelveMonthFloatingTotal = field4Floating.Value;
        }
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
        while (ndar1.StorageDaily.Count < 31) ndar1.StorageDaily.Add(null);
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

        // Did irrigation occur checkbox (Row 5, approximate location)
        // Note: Excel checkboxes are complex, this is a simplified approach
        // You may need to adjust based on actual template structure

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
                worksheet.Cell($"{col}6").Value = fields[i].WeeklyRateInches; // Weekly Rate (in/week)
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
            worksheet.Cell($"E{row}").Value = report.StorageDaily[dayIndex];
            worksheet.Cell($"F{row}").Value = report.FiveDayUpsetDaily[dayIndex];

            // Field 1 data (Columns G-J: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field1Id.HasValue)
            {
                worksheet.Cell($"G{row}").Value = report.Field1VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"H{row}").Value = report.Field1TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(G{row})," ",G{row}/($G$3*27152))
                worksheet.Cell($"I{row}").FormulaA1 = $"=IF(ISBLANK(G{row}),\" \",G{row}/(${fieldValueColumns[0]}$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(G{row}),ISBLANK(H{row}))," ",IF(H{row}<60,I{row},(I{row}/H{row})*60))
                worksheet.Cell($"J{row}").FormulaA1 = $"=IF(OR(ISBLANK(G{row}),ISBLANK(H{row})),\" \",IF(H{row}<60,I{row},(I{row}/H{row})*60))";
            }

            // Field 2 data (Columns K-N: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field2Id.HasValue)
            {
                worksheet.Cell($"K{row}").Value = report.Field2VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"L{row}").Value = report.Field2TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(K{row})," ",K{row}/($K$3*27152))
                worksheet.Cell($"M{row}").FormulaA1 = $"=IF(ISBLANK(K{row}),\" \",K{row}/(${fieldValueColumns[1]}$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(K{row}),ISBLANK(L{row}))," ",IF(L{row}<60,M{row},(M{row}/L{row})*60))
                worksheet.Cell($"N{row}").FormulaA1 = $"=IF(OR(ISBLANK(K{row}),ISBLANK(L{row})),\" \",IF(L{row}<60,M{row},(M{row}/L{row})*60))";
            }

            // Field 3 data (Columns O-R: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field3Id.HasValue)
            {
                worksheet.Cell($"O{row}").Value = report.Field3VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"P{row}").Value = report.Field3TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(O{row})," ",O{row}/($O$3*27152))
                worksheet.Cell($"Q{row}").FormulaA1 = $"=IF(ISBLANK(O{row}),\" \",O{row}/(${fieldValueColumns[2]}$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(O{row}),ISBLANK(P{row}))," ",IF(P{row}<60,Q{row},(Q{row}/P{row})*60))
                worksheet.Cell($"R{row}").FormulaA1 = $"=IF(OR(ISBLANK(O{row}),ISBLANK(P{row})),\" \",IF(P{row}<60,Q{row},(Q{row}/P{row})*60))";
            }

            // Field 4 data (Columns S-V: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field4Id.HasValue)
            {
                worksheet.Cell($"S{row}").Value = report.Field4VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"T{row}").Value = report.Field4TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(S{row})," ",S{row}/($S$3*27152))
                worksheet.Cell($"U{row}").FormulaA1 = $"=IF(ISBLANK(S{row}),\" \",S{row}/(${fieldValueColumns[3]}$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(S{row}),ISBLANK(T{row}))," ",IF(T{row}<60,U{row},(U{row}/T{row})*60))
                worksheet.Cell($"V{row}").FormulaA1 = $"=IF(OR(ISBLANK(S{row}),ISBLANK(T{row})),\" \",IF(T{row}<60,U{row},(U{row}/T{row})*60))";
            }
        }

        // Monthly totals (Row 41)
        var monthlyRow = 41;
        var dataStartRow = startRow; // Data starts at row 10 (day 1)
        var dataEndRow = startRow + daysInMonth - 1; // Data ends at the last day of the month
        
        if (report.Field1Id.HasValue)
        {
            // Monthly Loading: Sum of daily loading from row 10 to row 40
            worksheet.Cell($"I{monthlyRow}").FormulaA1 = $"=SUM(I{dataStartRow}:I{dataEndRow})";
            // Maximum Hourly Loading: Keep as calculated value (MAX of daily values)
            worksheet.Cell($"J{monthlyRow}").Value = report.Field1MaxHourlyLoading;
        }
        if (report.Field2Id.HasValue)
        {
            worksheet.Cell($"M{monthlyRow}").FormulaA1 = $"=SUM(M{dataStartRow}:M{dataEndRow})";
            worksheet.Cell($"N{monthlyRow}").Value = report.Field2MaxHourlyLoading;
        }
        if (report.Field3Id.HasValue)
        {
            worksheet.Cell($"Q{monthlyRow}").FormulaA1 = $"=SUM(Q{dataStartRow}:Q{dataEndRow})";
            worksheet.Cell($"R{monthlyRow}").Value = report.Field3MaxHourlyLoading;
        }
        if (report.Field4Id.HasValue)
        {
            worksheet.Cell($"U{monthlyRow}").FormulaA1 = $"=SUM(U{dataStartRow}:U{dataEndRow})";
            worksheet.Cell($"V{monthlyRow}").Value = report.Field4MaxHourlyLoading;
        }

        // 12-month floating totals (Row 42)
        var floatingRow = 42;
        if (report.Field1Id.HasValue)
        {
            worksheet.Cell($"I{floatingRow}").Value = report.Field1TwelveMonthFloatingTotal;
        }
        if (report.Field2Id.HasValue)
        {
            worksheet.Cell($"M{floatingRow}").Value = report.Field2TwelveMonthFloatingTotal;
        }
        if (report.Field3Id.HasValue)
        {
            worksheet.Cell($"Q{floatingRow}").Value = report.Field3TwelveMonthFloatingTotal;
        }
        if (report.Field4Id.HasValue)
        {
            worksheet.Cell($"U{floatingRow}").Value = report.Field4TwelveMonthFloatingTotal;
        }

        WriteCertificationPage(workbook, facility, irrigationReport?.ComplianceStatus);

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




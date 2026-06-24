using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.Utilities;
using SAM.ViewModels.Reports;

namespace SAM.Services.Implementations;

public class NDAR1RowEditService : INDAR1RowEditService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(10);
    private readonly ApplicationDbContext _context;
    private readonly IMonthlyReportProvisionerService _reportProvisioner;

    public NDAR1RowEditService(
        ApplicationDbContext context,
        IMonthlyReportProvisionerService reportProvisioner)
    {
        _context = context;
        _reportProvisioner = reportProvisioner;
    }

    public async Task<NDAR1EditGridViewModel> BuildGridAsync(Guid ndar1Id, string? currentUserId = null)
    {
        var report = await _context.NDAR1s
            .Include(x => x.Facility)
            .Include(x => x.Fields)
                .ThenInclude(f => f.Sprayfield)
            .Include(x => x.Field1)
            .Include(x => x.Field2)
            .Include(x => x.Field3)
            .Include(x => x.Field4)
            .FirstOrDefaultAsync(x => x.Id == ndar1Id);

        if (report == null)
        {
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);
        }

        var fieldColumns = GetFieldColumns(report);
        var sprayfieldById = GetSprayfieldsById(report);
        var start = new DateTime(report.Year, (int)report.Month, 1);
        var end = start.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(report.Year, (int)report.Month);

        var operatorLogs = await _context.OperatorLogs
            .Where(x => x.FacilityId == report.FacilityId && x.LogDate >= start && x.LogDate < end)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .ToListAsync();

        var monthlyApps = await _context.MonthlyApplications
            .Where(x => x.FacilityId == report.FacilityId && x.ApplicationDate >= start && x.ApplicationDate < end)
            .ToListAsync();

        var activeLocks = await _context.NdarEditLocks
            .Where(x => x.FacilityId == report.FacilityId && x.EditDate >= start && x.EditDate < end && x.ExpiresAtUtc > DateTime.UtcNow)
            .ToListAsync();

        var vm = new NDAR1EditGridViewModel
        {
            NDAR1Id = report.Id,
            CompanyId = report.CompanyId,
            FacilityId = report.FacilityId,
            FacilityName = report.Facility?.Name ?? string.Empty,
            Month = report.Month,
            Year = report.Year,
            FieldColumns = fieldColumns
        };

        var rollingStart = start.AddMonths(-11);
        var rollingEndExclusive = end;
        var rollingApps = await _context.MonthlyApplications
            .Where(x => x.FacilityId == report.FacilityId && x.ApplicationDate >= rollingStart && x.ApplicationDate < rollingEndExclusive)
            .ToListAsync();

        foreach (var field in vm.FieldColumns)
        {
            var monthlyForField = monthlyApps.Where(x => x.SprayfieldId == field.SprayfieldId).ToList();
            field.MonthlyVolumeTotalGallons = Math.Round(monthlyForField.Sum(x => x.VolumeGallons), 0, MidpointRounding.AwayFromZero);

            if (field.Acres.HasValue && field.Acres.Value > 0m)
            {
                field.MonthlyDailyLoadingTotalInches = monthlyForField.Sum(x => x.VolumeGallons / (field.Acres.Value * MonthlyApplicationCalculationHelper.GallonsPerAcreInch));
                field.TwelveMonthFloatingTotalInches = rollingApps
                    .Where(x => x.SprayfieldId == field.SprayfieldId)
                    .Sum(x => x.VolumeGallons / (field.Acres.Value * MonthlyApplicationCalculationHelper.GallonsPerAcreInch));
            }
        }

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(report.Year, (int)report.Month, day);
            var log = operatorLogs.FirstOrDefault(x => x.LogDate.Date == date.Date);
            var row = new NDAR1GridDayRowViewModel
            {
                DayNo = day,
                Date = date,
                WeatherCode = WeatherCodeCatalog.TryNormalizeAbbreviation(log?.WeatherConditions, out var code) ? code : null,
                TemperatureF = log?.TemperatureF,
                PrecipitationIn = log?.PrecipitationIn,
                StorageFt = log?.StorageFt,
                FiveDayUpsetFt = log?.FiveDayUpsetFt
            };

            var dayLock = activeLocks.FirstOrDefault(x => x.EditDate.Date == date.Date);
            if (dayLock != null)
            {
                row.LockToken = dayLock.LockToken;
                row.LockedBy = dayLock.LockedByDisplayName;
                row.IsLockedByCurrentUser = !string.IsNullOrEmpty(currentUserId) &&
                    string.Equals(dayLock.LockedByUserId, currentUserId, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var field in fieldColumns)
            {
                var app = monthlyApps.FirstOrDefault(x => x.ApplicationDate.Date == date.Date && x.SprayfieldId == field.SprayfieldId);
                sprayfieldById.TryGetValue(field.SprayfieldId, out var sprayfield);
                decimal? dailyLoading = app != null && field.Acres.HasValue && field.Acres.Value > 0m
                    ? app.VolumeGallons / (field.Acres.Value * MonthlyApplicationCalculationHelper.GallonsPerAcreInch)
                    : null;
                decimal? maxHourlyLoading = app?.TimeIrrigatedMinutes is > 0
                    ? (sprayfield?.ActualHourlyRateInches ?? app.MaximumHourlyLoadingInchesPerAcre)
                    : app?.MaximumHourlyLoadingInchesPerAcre;
                row.Applications.Add(new NDAR1GridApplicationCellViewModel
                {
                    SprayfieldId = field.SprayfieldId,
                    VolumeGallons = app != null ? Math.Round(app.VolumeGallons, 0, MidpointRounding.AwayFromZero) : null,
                    TimeIrrigatedMinutes = app?.TimeIrrigatedMinutes.HasValue == true ? Math.Round(app.TimeIrrigatedMinutes.Value, 0, MidpointRounding.AwayFromZero) : null,
                    MaximumHourlyLoadingInchesPerAcre = maxHourlyLoading,
                    DailyLoadingInches = dailyLoading
                });
            }

            vm.Rows.Add(row);
        }

        return vm;
    }

    public async Task<NDAR1RowEditLockResult> BeginRowEditAsync(Guid ndar1Id, int dayNo, string userId, string userDisplayName)
    {
        var report = await _context.NDAR1s.FirstOrDefaultAsync(x => x.Id == ndar1Id);
        if (report == null)
        {
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);
        }

        var date = new DateTime(report.Year, (int)report.Month, dayNo);
        var now = DateTime.UtcNow;
        var lockRow = await _context.NdarEditLocks.FirstOrDefaultAsync(x => x.FacilityId == report.FacilityId && x.EditDate == date);

        if (lockRow != null && lockRow.ExpiresAtUtc > now && !string.Equals(lockRow.LockedByUserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return new NDAR1RowEditLockResult
            {
                Success = false,
                DayNo = dayNo,
                LockedBy = lockRow.LockedByDisplayName,
                RemainingSeconds = (int)Math.Ceiling((lockRow.ExpiresAtUtc - now).TotalSeconds),
                Message = $"This row is currently being edited by {lockRow.LockedByDisplayName}."
            };
        }

        var token = Guid.NewGuid();
        if (lockRow == null)
        {
            lockRow = new NdarEditLock
            {
                Id = Guid.NewGuid(),
                CompanyId = report.CompanyId,
                FacilityId = report.FacilityId,
                EditDate = date,
                LockedByUserId = userId,
                LockedByDisplayName = userDisplayName,
                LockToken = token,
                LockedAtUtc = now,
                ExpiresAtUtc = now.Add(LockTimeout),
                CreatedBy = userDisplayName
            };
            _context.NdarEditLocks.Add(lockRow);
        }
        else
        {
            lockRow.LockedByUserId = userId;
            lockRow.LockedByDisplayName = userDisplayName;
            lockRow.LockToken = token;
            lockRow.LockedAtUtc = now;
            lockRow.ExpiresAtUtc = now.Add(LockTimeout);
            lockRow.ReleasedAtUtc = null;
            lockRow.IsDeleted = false;
        }

        await _context.SaveChangesAsync();

        return new NDAR1RowEditLockResult
        {
            Success = true,
            DayNo = dayNo,
            LockToken = token
        };
    }

    public async Task<NDAR1GridEditBeginResult> BeginGridEditAsync(Guid ndar1Id, string userId, string userDisplayName)
    {
        var report = await _context.NDAR1s.FirstOrDefaultAsync(x => x.Id == ndar1Id);
        if (report == null)
        {
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);
        }

        var start = new DateTime(report.Year, (int)report.Month, 1);
        var end = start.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(report.Year, (int)report.Month);
        var now = DateTime.UtcNow;

        var existingLocks = await _context.NdarEditLocks
            .Where(x => x.FacilityId == report.FacilityId && x.EditDate >= start && x.EditDate < end)
            .ToListAsync();

        var result = new NDAR1GridEditBeginResult();

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(report.Year, (int)report.Month, day);
            var lockRow = existingLocks.FirstOrDefault(x => x.EditDate == date);

            if (lockRow != null && lockRow.ExpiresAtUtc > now &&
                !string.Equals(lockRow.LockedByUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                result.Locks.Add(new NDAR1GridDayLockResult
                {
                    DayNo = day,
                    Success = false,
                    LockedBy = lockRow.LockedByDisplayName,
                    Message = $"This row is currently being edited by {lockRow.LockedByDisplayName}."
                });
                continue;
            }

            var token = Guid.NewGuid();
            if (lockRow == null)
            {
                lockRow = new NdarEditLock
                {
                    Id = Guid.NewGuid(),
                    CompanyId = report.CompanyId,
                    FacilityId = report.FacilityId,
                    EditDate = date,
                    LockedByUserId = userId,
                    LockedByDisplayName = userDisplayName,
                    LockToken = token,
                    LockedAtUtc = now,
                    ExpiresAtUtc = now.Add(LockTimeout),
                    CreatedBy = userDisplayName
                };
                _context.NdarEditLocks.Add(lockRow);
                existingLocks.Add(lockRow);
            }
            else
            {
                lockRow.LockedByUserId = userId;
                lockRow.LockedByDisplayName = userDisplayName;
                lockRow.LockToken = token;
                lockRow.LockedAtUtc = now;
                lockRow.ExpiresAtUtc = now.Add(LockTimeout);
                lockRow.ReleasedAtUtc = null;
                lockRow.IsDeleted = false;
            }

            result.Locks.Add(new NDAR1GridDayLockResult
            {
                DayNo = day,
                Success = true,
                LockToken = token
            });
        }

        await _context.SaveChangesAsync();
        return result;
    }

    public async Task ReleaseGridEditAsync(Guid ndar1Id, IEnumerable<Guid> lockTokens, string userId)
    {
        var tokens = lockTokens.Where(x => x != Guid.Empty).Distinct().ToList();
        if (tokens.Count == 0)
        {
            return;
        }

        var report = await _context.NDAR1s.FirstOrDefaultAsync(x => x.Id == ndar1Id);
        if (report == null)
        {
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);
        }

        var start = new DateTime(report.Year, (int)report.Month, 1);
        var end = start.AddMonths(1);

        var lockRows = await _context.NdarEditLocks
            .Where(x =>
                x.FacilityId == report.FacilityId &&
                x.EditDate >= start &&
                x.EditDate < end &&
                tokens.Contains(x.LockToken))
            .ToListAsync();

        var now = DateTime.UtcNow;
        foreach (var lockRow in lockRows)
        {
            if (!string.Equals(lockRow.LockedByUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            lockRow.ReleasedAtUtc = now;
            lockRow.ExpiresAtUtc = now;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<NDAR1GridFooterTotalsResult> GetGridFooterTotalsAsync(Guid ndar1Id)
    {
        var grid = await BuildGridAsync(ndar1Id);
        return new NDAR1GridFooterTotalsResult
        {
            FieldColumns = grid.FieldColumns
        };
    }

    public async Task<NDAR1RowEditResult> UpdateRowAsync(Guid ndar1Id, NDAR1DayRowUpdateRequest request, string userId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        NDAR1RowEditResult? lockValidationFailure = null;
        NDAR1RowSaveSummaryViewModel? saveSummary = null;

        await strategy.ExecuteAsync(async () =>
        {
            var report = await _context.NDAR1s
                .FirstOrDefaultAsync(x => x.Id == ndar1Id);

            if (report == null)
            {
                throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);
            }

            var dayDate = new DateTime(report.Year, (int)report.Month, request.DayNo);
            var lockRow = await _context.NdarEditLocks.FirstOrDefaultAsync(x => x.FacilityId == report.FacilityId && x.EditDate == dayDate);
            if (lockRow == null || lockRow.ExpiresAtUtc <= DateTime.UtcNow || lockRow.LockToken != request.LockToken || !string.Equals(lockRow.LockedByUserId, userId, StringComparison.OrdinalIgnoreCase))
            {
                lockValidationFailure = new NDAR1RowEditResult
                {
                    Success = false,
                    Message = "Edit lock expired or invalid. Please click Edit again."
                };
                return;
            }

            if (!WeatherCodeCatalog.TryNormalizeAbbreviation(request.WeatherCode, out var normalizedWeatherCode))
            {
                lockValidationFailure = new NDAR1RowEditResult
                {
                    Success = false,
                    IsValidationError = true,
                    Message = "Weather code is invalid. Please select a valid abbreviation."
                };
                return;
            }

            var requestedSprayfieldIds = request.Applications
                .Select(x => x.SprayfieldId)
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            var sprayfields = await _context.Sprayfields
                .Where(x => x.FacilityId == report.FacilityId && requestedSprayfieldIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id);

            var setupErrors = new List<string>();
            foreach (var sprayfieldId in requestedSprayfieldIds)
            {
                if (!sprayfields.TryGetValue(sprayfieldId, out var sprayfield))
                {
                    setupErrors.Add($"Sprayfield {sprayfieldId} was not found for this facility.");
                    continue;
                }

                var acres = MonthlyApplicationCalculationHelper.ResolveAcres(sprayfield);
                if (acres <= 0m)
                {
                    setupErrors.Add($"Sprayfield {sprayfield.FieldId} is missing Area (acres).");
                }

                if (!sprayfield.ActualHourlyRateInches.HasValue)
                {
                    setupErrors.Add($"Sprayfield {sprayfield.FieldId} is missing Actual Hourly Rate.");
                }
            }

            if (setupErrors.Count > 0)
            {
                lockValidationFailure = new NDAR1RowEditResult
                {
                    Success = false,
                    IsValidationError = true,
                    Message = string.Join(" ", setupErrors.Distinct())
                };
                return;
            }

            await using var tx = await _context.Database.BeginTransactionAsync();
            saveSummary = new NDAR1RowSaveSummaryViewModel
            {
                DayNo = request.DayNo,
                Date = dayDate,
                OperatorLogAction = "None"
            };

            var operatorLog = await _context.OperatorLogs
                .Where(x => x.FacilityId == report.FacilityId && x.LogDate == dayDate)
                .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                .FirstOrDefaultAsync();

            var hasWeatherData = !string.IsNullOrWhiteSpace(request.WeatherCode)
                || request.TemperatureF.HasValue
                || request.PrecipitationIn.HasValue
                || request.StorageFt.HasValue
                || request.FiveDayUpsetFt.HasValue;

            if (operatorLog == null && hasWeatherData)
            {
                saveSummary.OperatorLogAction = "Created";
                saveSummary.WeatherFieldsChanged = CollectWeatherChanges(
                    null,
                    normalizedWeatherCode,
                    request.TemperatureF,
                    request.PrecipitationIn,
                    request.StorageFt,
                    request.FiveDayUpsetFt);
                operatorLog = new OperatorLog
                {
                    Id = Guid.NewGuid(),
                    CompanyId = report.CompanyId,
                    FacilityId = report.FacilityId,
                    LogDate = dayDate,
                    OperatorName = "System",
                    WeatherConditions = normalizedWeatherCode ?? string.Empty,
                    TemperatureF = request.TemperatureF,
                    PrecipitationIn = request.PrecipitationIn,
                    StorageFt = request.StorageFt,
                    FiveDayUpsetFt = request.FiveDayUpsetFt,
                    ArrivalTime = TimeSpan.Zero,
                    TimeOnSiteHours = 0m,
                    MaintenancePerformed = string.Empty,
                    EquipmentInspected = string.Empty,
                    IssuesNoted = string.Empty,
                    CorrectiveActions = string.Empty,
                    NextShiftNotes = string.Empty,
                    CreatedBy = userId
                };
                _context.OperatorLogs.Add(operatorLog);
            }
            else if (operatorLog != null)
            {
                var changedWeather = CollectWeatherChanges(
                    operatorLog,
                    normalizedWeatherCode,
                    request.TemperatureF,
                    request.PrecipitationIn,
                    request.StorageFt,
                    request.FiveDayUpsetFt);
                saveSummary.WeatherFieldsChanged = changedWeather;
                saveSummary.OperatorLogAction = changedWeather.Count > 0 ? "Updated" : "Unchanged";
                operatorLog.WeatherConditions = normalizedWeatherCode ?? string.Empty;
                operatorLog.TemperatureF = request.TemperatureF;
                operatorLog.PrecipitationIn = request.PrecipitationIn;
                operatorLog.StorageFt = request.StorageFt;
                operatorLog.FiveDayUpsetFt = request.FiveDayUpsetFt;
            }

            foreach (var cell in request.Applications)
            {
                var app = await _context.MonthlyApplications
                    .FirstOrDefaultAsync(x => x.FacilityId == report.FacilityId && x.SprayfieldId == cell.SprayfieldId && x.ApplicationDate == dayDate);

                if (!sprayfields.TryGetValue(cell.SprayfieldId, out var sprayfield))
                {
                    continue;
                }

                var acres = MonthlyApplicationCalculationHelper.ResolveAcres(sprayfield);
                var computedMaxHourly = sprayfield.ActualHourlyRateInches.Value;
                var computedDailyLoading = MonthlyApplicationCalculationHelper.ComputeDailyLoadingInches(cell.TimeIrrigatedMinutes, computedMaxHourly);
                var computedVolumeGallons = computedDailyLoading.HasValue
                    ? MonthlyApplicationCalculationHelper.ComputeVolumeGallons(computedDailyLoading.Value, acres)
                    : 0m;

                var hasData = cell.TimeIrrigatedMinutes.HasValue;

                if (app == null && hasData)
                {
                    saveSummary.MonthlyApplicationsCreated.Add(ToEntityChange(sprayfield));
                    app = new MonthlyApplication
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = report.CompanyId,
                        FacilityId = report.FacilityId,
                        SprayfieldId = cell.SprayfieldId,
                        ApplicationDate = dayDate,
                        VolumeGallons = computedVolumeGallons,
                        TimeIrrigatedMinutes = cell.TimeIrrigatedMinutes,
                        MaximumHourlyLoadingInchesPerAcre = computedMaxHourly,
                        OperatorSnapshotName = "System",
                        Comments = "Updated from NDAR1 row edit",
                        CreatedBy = userId
                    };
                    _context.MonthlyApplications.Add(app);
                }
                else if (app != null)
                {
                    var isNdarOrigin = !string.IsNullOrWhiteSpace(app.Comments) &&
                        app.Comments.Contains("NDAR1 row edit", StringComparison.OrdinalIgnoreCase);
                    var originalTimeIrrigated = app.TimeIrrigatedMinutes;
                    var hasStaleComputedValues = app.TimeIrrigatedMinutes.HasValue &&
                        app.TimeIrrigatedMinutes.Value > 0m &&
                        (app.VolumeGallons <= 0m || app.MaximumHourlyLoadingInchesPerAcre <= 0m);

                    app.VolumeGallons = computedVolumeGallons;
                    app.TimeIrrigatedMinutes = cell.TimeIrrigatedMinutes;
                    app.MaximumHourlyLoadingInchesPerAcre = computedMaxHourly;
                    app.Comments = "Updated from NDAR1 row edit";

                    if (isNdarOrigin && hasStaleComputedValues && !cell.TimeIrrigatedMinutes.HasValue)
                    {
                        var backfilledDaily = MonthlyApplicationCalculationHelper.ComputeDailyLoadingInches(originalTimeIrrigated, computedMaxHourly);
                        app.VolumeGallons = backfilledDaily.HasValue
                            ? MonthlyApplicationCalculationHelper.ComputeVolumeGallons(backfilledDaily.Value, acres)
                            : 0m;
                        app.TimeIrrigatedMinutes = originalTimeIrrigated;
                        app.MaximumHourlyLoadingInchesPerAcre = computedMaxHourly;
                        saveSummary.MonthlyApplicationsBackfilled.Add(ToEntityChange(sprayfield));
                    }
                    else
                    {
                        saveSummary.MonthlyApplicationsUpdated.Add(ToEntityChange(sprayfield));
                    }
                }
            }

            var monthStart = new DateTime(report.Year, (int)report.Month, 1);
            var monthEndExclusive = monthStart.AddMonths(1);
            var hasOtherIrrigationThisMonth = await _context.MonthlyApplications
                .Where(x =>
                    x.FacilityId == report.FacilityId &&
                    x.ApplicationDate >= monthStart &&
                    x.ApplicationDate < monthEndExclusive &&
                    x.ApplicationDate != dayDate &&
                    x.TimeIrrigatedMinutes.HasValue &&
                    x.TimeIrrigatedMinutes.Value > 0m)
                .AnyAsync();
            var hasIrrigationForEditedDay = request.Applications.Any(x => x.TimeIrrigatedMinutes.HasValue && x.TimeIrrigatedMinutes.Value > 0m);
            report.DidIrrigationOccur = hasOtherIrrigationThisMonth || hasIrrigationForEditedDay;
            saveSummary.IrrigationFlagAfterSave = report.DidIrrigationOccur;

            if (request.KeepLockAfterSave)
            {
                lockRow.ExpiresAtUtc = DateTime.UtcNow.Add(LockTimeout);
                lockRow.ReleasedAtUtc = null;
            }
            else
            {
                lockRow.ReleasedAtUtc = DateTime.UtcNow;
                lockRow.ExpiresAtUtc = DateTime.UtcNow;
            }

            try
            {
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync();
                lockValidationFailure = new NDAR1RowEditResult
                {
                    Success = false,
                    Message = "This row was changed by another process. Please click Edit again and retry."
                };
            }
        });

        if (lockValidationFailure != null)
        {
            return lockValidationFailure;
        }

        if (!request.SkipNdarRefresh)
        {
            var reportMonth = saveSummary!.Date.Month;
            var reportYear = saveSummary.Date.Year;
            Services.Models.NdarRefreshOutcome ndarRefreshOutcome;

            var sourceReport = await _context.NDAR1s
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == ndar1Id);

            if (sourceReport != null)
            {
                ndarRefreshOutcome = await _reportProvisioner.EnsureNdar1ForMonthAsync(
                    sourceReport.FacilityId,
                    reportMonth,
                    reportYear);
                await _reportProvisioner.EnsureNdmrForMonthAsync(sourceReport.FacilityId, reportMonth, reportYear);
                await _reportProvisioner.EnsureNdmlrForMonthAsync(sourceReport.FacilityId, reportMonth, reportYear);
            }
            else
            {
                ndarRefreshOutcome = new Services.Models.NdarRefreshOutcome
                {
                    FacilityId = Guid.Empty,
                    Month = reportMonth,
                    Year = reportYear,
                    Status = Services.Models.NdarRefreshStatus.NoReport,
                    Message = "No NDAR-1 report exists for this period."
                };
            }

            saveSummary.NdarRefreshStatus = ndarRefreshOutcome.Status.ToString();
            saveSummary.NdarRefreshMessage = ndarRefreshOutcome.Message;
            if (ndarRefreshOutcome.Status == Services.Models.NdarRefreshStatus.Failed && !string.IsNullOrWhiteSpace(ndarRefreshOutcome.Message))
            {
                saveSummary.Warnings.Add(ndarRefreshOutcome.Message);
            }
        }
        else
        {
            saveSummary!.NdarRefreshStatus = "Skipped";
        }

        _context.ChangeTracker.Clear();
        var refreshed = await BuildGridAsync(ndar1Id, userId);
        return new NDAR1RowEditResult
        {
            Success = true,
            Row = refreshed.Rows.First(x => x.DayNo == request.DayNo),
            SaveSummary = saveSummary
        };
    }

    public async Task<NdarRefreshOutcome> RefreshStoredReportAsync(Guid ndar1Id)
    {
        var report = await _context.NDAR1s
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ndar1Id)
            ?? throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);

        var month = (int)report.Month;
        var outcome = await _reportProvisioner.EnsureNdar1ForMonthAsync(report.FacilityId, month, report.Year);
        await _reportProvisioner.EnsureNdmrForMonthAsync(report.FacilityId, month, report.Year);
        await _reportProvisioner.EnsureNdmlrForMonthAsync(report.FacilityId, month, report.Year);
        return outcome;
    }

    private static NDAR1RowEntityChangeViewModel ToEntityChange(Sprayfield sprayfield) =>
        new()
        {
            SprayfieldId = sprayfield.Id,
            SprayfieldCode = string.IsNullOrWhiteSpace(sprayfield.FieldId) ? sprayfield.Id.ToString() : sprayfield.FieldId
        };

    private static List<string> CollectWeatherChanges(
        OperatorLog? existingLog,
        string? newWeatherCode,
        decimal? newTemperatureF,
        decimal? newPrecipitationIn,
        decimal? newStorageFt,
        decimal? newFiveDayUpsetFt)
    {
        var changes = new List<string>();
        var normalizedIncomingWeather = newWeatherCode ?? string.Empty;
        var existingWeather = existingLog?.WeatherConditions ?? string.Empty;

        if (!string.Equals(existingWeather, normalizedIncomingWeather, StringComparison.OrdinalIgnoreCase))
        {
            changes.Add("Weather Code");
        }

        if (existingLog?.TemperatureF != newTemperatureF) changes.Add("Temperature");
        if (existingLog?.PrecipitationIn != newPrecipitationIn) changes.Add("Precipitation");
        if (existingLog?.StorageFt != newStorageFt) changes.Add("Storage");
        if (existingLog?.FiveDayUpsetFt != newFiveDayUpsetFt) changes.Add("5-Day Upset");

        return changes;
    }

    public async Task CancelRowEditAsync(Guid ndar1Id, int dayNo, Guid lockToken, string userId)
    {
        var report = await _context.NDAR1s.FirstOrDefaultAsync(x => x.Id == ndar1Id);
        if (report == null)
        {
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);
        }

        var date = new DateTime(report.Year, (int)report.Month, dayNo);
        var lockRow = await _context.NdarEditLocks.FirstOrDefaultAsync(x => x.FacilityId == report.FacilityId && x.EditDate == date);
        if (lockRow == null || lockRow.LockToken != lockToken || !string.Equals(lockRow.LockedByUserId, userId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        lockRow.ReleasedAtUtc = DateTime.UtcNow;
        lockRow.ExpiresAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static Dictionary<Guid, Sprayfield> GetSprayfieldsById(NDAR1 report)
    {
        var sprayfields = new List<Sprayfield>();
        if (report.Fields.Any())
        {
            sprayfields.AddRange(report.Fields.Where(f => f.Sprayfield != null).Select(f => f.Sprayfield!));
        }
        else
        {
            if (report.Field1 != null) sprayfields.Add(report.Field1);
            if (report.Field2 != null) sprayfields.Add(report.Field2);
            if (report.Field3 != null) sprayfields.Add(report.Field3);
            if (report.Field4 != null) sprayfields.Add(report.Field4);
        }

        return sprayfields
            .DistinctBy(s => s.Id)
            .ToDictionary(s => s.Id);
    }

    private List<NDAR1GridFieldColumnViewModel> GetFieldColumns(NDAR1 report)
    {
        if (report.Fields.Any())
        {
            return report.Fields
                .OrderBy(x => x.FieldOrder)
                .Where(x => x.Sprayfield != null)
                .Select(x => new NDAR1GridFieldColumnViewModel
                {
                    SprayfieldId = x.SprayfieldId,
                    FieldCode = x.Sprayfield!.FieldId,
                    Acres = x.Sprayfield.SizeAcres
                })
                .ToList();
        }

        var list = new List<Sprayfield>();
        if (report.Field1 != null) list.Add(report.Field1);
        if (report.Field2 != null) list.Add(report.Field2);
        if (report.Field3 != null) list.Add(report.Field3);
        if (report.Field4 != null) list.Add(report.Field4);

        return list
            .DistinctBy(x => x.Id)
            .Select(x => new NDAR1GridFieldColumnViewModel
            {
                SprayfieldId = x.Id,
                FieldCode = x.FieldId,
                Acres = x.SizeAcres
            })
            .ToList();
    }
}

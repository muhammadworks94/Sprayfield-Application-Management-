using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.ViewModels.Reports;

namespace SAM.Services.Implementations;

public class NDAR1RowEditService : INDAR1RowEditService
{
    private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(10);
    private readonly ApplicationDbContext _context;
    private readonly INDAR1Service _ndar1Service;

    public NDAR1RowEditService(ApplicationDbContext context, INDAR1Service ndar1Service)
    {
        _context = context;
        _ndar1Service = ndar1Service;
    }

    public async Task<NDAR1EditGridViewModel> BuildGridAsync(Guid ndar1Id)
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

        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateTime(report.Year, (int)report.Month, day);
            var log = operatorLogs.FirstOrDefault(x => x.LogDate.Date == date.Date);
            var row = new NDAR1GridDayRowViewModel
            {
                DayNo = day,
                Date = date,
                WeatherCode = log?.WeatherConditions,
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
            }

            foreach (var field in fieldColumns)
            {
                var app = monthlyApps.FirstOrDefault(x => x.ApplicationDate.Date == date.Date && x.SprayfieldId == field.SprayfieldId);
                decimal? dailyLoading = app != null && field.Acres.HasValue && field.Acres.Value > 0m
                    ? app.VolumeGallons / (field.Acres.Value * 27152m)
                    : null;
                row.Applications.Add(new NDAR1GridApplicationCellViewModel
                {
                    SprayfieldId = field.SprayfieldId,
                    VolumeGallons = app?.VolumeGallons,
                    TimeIrrigatedMinutes = app?.TimeIrrigatedMinutes,
                    MaximumHourlyLoadingInchesPerAcre = app?.MaximumHourlyLoadingInchesPerAcre,
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

    public async Task<NDAR1RowEditResult> UpdateRowAsync(Guid ndar1Id, NDAR1DayRowUpdateRequest request, string userId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        NDAR1RowEditResult? lockValidationFailure = null;

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

            await using var tx = await _context.Database.BeginTransactionAsync();

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
                operatorLog = new OperatorLog
                {
                    Id = Guid.NewGuid(),
                    CompanyId = report.CompanyId,
                    FacilityId = report.FacilityId,
                    LogDate = dayDate,
                    OperatorName = "System",
                    WeatherConditions = request.WeatherCode ?? string.Empty,
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
                operatorLog.WeatherConditions = request.WeatherCode ?? string.Empty;
                operatorLog.TemperatureF = request.TemperatureF;
                operatorLog.PrecipitationIn = request.PrecipitationIn;
                operatorLog.StorageFt = request.StorageFt;
                operatorLog.FiveDayUpsetFt = request.FiveDayUpsetFt;
            }

            foreach (var cell in request.Applications)
            {
                var app = await _context.MonthlyApplications
                    .FirstOrDefaultAsync(x => x.FacilityId == report.FacilityId && x.SprayfieldId == cell.SprayfieldId && x.ApplicationDate == dayDate);

                var hasData = cell.VolumeGallons.HasValue
                    || cell.TimeIrrigatedMinutes.HasValue
                    || cell.MaximumHourlyLoadingInchesPerAcre.HasValue;

                if (app == null && hasData)
                {
                    app = new MonthlyApplication
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = report.CompanyId,
                        FacilityId = report.FacilityId,
                        SprayfieldId = cell.SprayfieldId,
                        ApplicationDate = dayDate,
                        VolumeGallons = cell.VolumeGallons ?? 0m,
                        TimeIrrigatedMinutes = cell.TimeIrrigatedMinutes,
                        MaximumHourlyLoadingInchesPerAcre = cell.MaximumHourlyLoadingInchesPerAcre ?? 0m,
                        OperatorSnapshotName = "System",
                        Comments = "Updated from NDAR1 row edit",
                        CreatedBy = userId
                    };
                    _context.MonthlyApplications.Add(app);
                }
                else if (app != null)
                {
                    app.VolumeGallons = cell.VolumeGallons ?? 0m;
                    app.TimeIrrigatedMinutes = cell.TimeIrrigatedMinutes;
                    app.MaximumHourlyLoadingInchesPerAcre = cell.MaximumHourlyLoadingInchesPerAcre ?? 0m;
                    app.Comments = "Updated from NDAR1 row edit";
                }
            }

            lockRow.ReleasedAtUtc = DateTime.UtcNow;
            lockRow.ExpiresAtUtc = DateTime.UtcNow;

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

        var refreshed = await BuildGridAsync(ndar1Id);
        return new NDAR1RowEditResult
        {
            Success = true,
            Row = refreshed.Rows.First(x => x.DayNo == request.DayNo)
        };
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

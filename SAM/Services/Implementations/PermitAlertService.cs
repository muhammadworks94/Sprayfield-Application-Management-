using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class PermitAlertService : IPermitAlertService
{
    private readonly ApplicationDbContext _context;

    public PermitAlertService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PermitAlert>> GetAlertsAsync(Guid? companyId, DateTime asOfUtc)
    {
        var today = asOfUtc.Date;
        var query = _context.FacilityPermits
            .AsNoTracking()
            .Include(p => p.Facility)
            .Where(p => p.IsActive && p.EffectiveEndDate.HasValue);

        if (companyId.HasValue)
        {
            query = query.Where(p => p.CompanyId == companyId.Value);
        }

        var permits = await query.ToListAsync();
        var alerts = new List<PermitAlert>();

        foreach (var permit in permits)
        {
            var endDate = permit.EffectiveEndDate!.Value.Date;
            var daysUntil = (endDate - today).Days;
            if (daysUntil < 0 || daysUntil > 30)
            {
                continue;
            }

            var hasFutureVersion = await _context.FacilityPermits
                .AsNoTracking()
                .AnyAsync(p => p.FacilityId == permit.FacilityId
                               && p.IsActive
                               && p.Id != permit.Id
                               && p.EffectiveStartDate > today);

            if (hasFutureVersion)
            {
                continue;
            }

            alerts.Add(new PermitAlert
            {
                FacilityId = permit.FacilityId,
                FacilityName = permit.Facility?.Name ?? "Facility",
                PermitNumber = permit.PermitNumber,
                PermitVersion = permit.PermitVersion,
                DaysUntilExpiration = daysUntil,
                EffectiveEndDate = endDate,
                Severity = daysUntil <= 7 ? PermitAlertSeverity.Critical : PermitAlertSeverity.Warning,
                Message = $"DEQ Permit expires in {daysUntil} day{(daysUntil == 1 ? string.Empty : "s")}. Upload new permit version and details for future dates in System Administration => Permits."
            });
        }

        var startedToday = await _context.FacilityPermits
            .AsNoTracking()
            .Include(p => p.Facility)
            .Where(p => p.IsActive && p.EffectiveStartDate.Date == today)
            .Where(p => !companyId.HasValue || p.CompanyId == companyId.Value)
            .ToListAsync();

        foreach (var permit in startedToday)
        {
            var superseded = await _context.FacilityPermits
                .AsNoTracking()
                .AnyAsync(p => p.FacilityId == permit.FacilityId
                               && p.Id != permit.Id
                               && p.EffectiveEndDate.HasValue
                               && p.EffectiveEndDate.Value.Date < today);

            if (!superseded)
            {
                continue;
            }

            alerts.Add(new PermitAlert
            {
                FacilityId = permit.FacilityId,
                FacilityName = permit.Facility?.Name ?? "Facility",
                PermitNumber = permit.PermitNumber,
                PermitVersion = permit.PermitVersion,
                DaysUntilExpiration = 0,
                EffectiveEndDate = permit.EffectiveEndDate?.Date,
                Severity = PermitAlertSeverity.Info,
                Message = $"The Permit was updated to v{permit.PermitVersion} due to expiration. No action is needed."
            });
        }

        return alerts
            .OrderBy(a => a.Severity)
            .ThenBy(a => a.DaysUntilExpiration)
            .ToList();
    }
}

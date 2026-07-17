using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class ReportAccessService : IReportAccessService
{
    public const string PermitExpiredReason = "Permit expired — renewal required";
    public const string ReportDisabledReason = "Report disabled by administrator";

    private readonly ApplicationDbContext _context;

    public ReportAccessService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FacilityReportAccess> GetFacilityAccessAsync(Guid facilityId, ReportKind kind)
    {
        var map = await GetFacilityAccessMapAsync(new[] { facilityId }, kind);
        return map[facilityId];
    }

    public async Task<CompanyReportVisibility> GetCompanyReportVisibilityAsync(Guid companyId)
    {
        var today = DateTime.UtcNow.Date;

        var validPermitFlags = await _context.FacilityPermits
            .AsNoTracking()
            .Where(p => p.CompanyId == companyId
                        && p.IsActive
                        && p.EffectiveStartDate <= today
                        && (!p.EffectiveEndDate.HasValue || p.EffectiveEndDate.Value >= today))
            .Select(p => new
            {
                p.ShowNdar1Report,
                p.ShowNdmrReport,
                p.ShowNdmlrReport,
                p.ShowGw59Report
            })
            .ToListAsync();

        if (validPermitFlags.Count == 0)
        {
            // No permit valid today: if the company has never had permits configured,
            // keep everything visible; otherwise all permits are expired/inactive → hide all.
            var hasAnyPermit = await _context.FacilityPermits
                .AsNoTracking()
                .AnyAsync(p => p.CompanyId == companyId);

            var showAll = !hasAnyPermit;
            return new CompanyReportVisibility
            {
                ShowNdar1 = showAll,
                ShowNdmr = showAll,
                ShowNdmlr = showAll,
                ShowGw59 = showAll
            };
        }

        return new CompanyReportVisibility
        {
            ShowNdar1 = validPermitFlags.Any(p => p.ShowNdar1Report),
            ShowNdmr = validPermitFlags.Any(p => p.ShowNdmrReport),
            ShowNdmlr = validPermitFlags.Any(p => p.ShowNdmlrReport),
            ShowGw59 = validPermitFlags.Any(p => p.ShowGw59Report)
        };
    }

    public async Task<Dictionary<Guid, FacilityReportAccess>> GetFacilityAccessMapAsync(IReadOnlyCollection<Guid> facilityIds, ReportKind kind)
    {
        var result = new Dictionary<Guid, FacilityReportAccess>();
        if (facilityIds.Count == 0)
        {
            return result;
        }

        var today = DateTime.UtcNow.Date;
        var ids = facilityIds.Distinct().ToList();

        var permits = await _context.FacilityPermits
            .AsNoTracking()
            .Where(p => ids.Contains(p.FacilityId)
                        && p.IsActive
                        && p.EffectiveStartDate <= today
                        && (!p.EffectiveEndDate.HasValue || p.EffectiveEndDate.Value >= today))
            .Select(p => new
            {
                p.FacilityId,
                p.ShowNdar1Report,
                p.ShowNdmrReport,
                p.ShowNdmlrReport,
                p.ShowGw59Report
            })
            .ToListAsync();

        var byFacility = permits.ToLookup(p => p.FacilityId);

        foreach (var facilityId in ids)
        {
            var facilityPermits = byFacility[facilityId].ToList();
            if (facilityPermits.Count == 0)
            {
                result[facilityId] = new FacilityReportAccess
                {
                    HasValidPermit = false,
                    ReportEnabled = false,
                    Reason = PermitExpiredReason
                };
                continue;
            }

            var enabled = facilityPermits.Any(p => kind switch
            {
                ReportKind.Ndar1 => p.ShowNdar1Report,
                ReportKind.Ndmr => p.ShowNdmrReport,
                ReportKind.Ndmlr => p.ShowNdmlrReport,
                ReportKind.Gw59 => p.ShowGw59Report,
                _ => true
            });

            result[facilityId] = new FacilityReportAccess
            {
                HasValidPermit = true,
                ReportEnabled = enabled,
                Reason = enabled ? null : ReportDisabledReason
            };
        }

        return result;
    }
}

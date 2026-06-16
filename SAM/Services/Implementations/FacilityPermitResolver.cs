using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class FacilityPermitResolver : IFacilityPermitResolver
{
    private readonly ApplicationDbContext _context;

    public FacilityPermitResolver(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FacilityPermit?> ResolveForDateAsync(Guid facilityId, DateTime reportDate)
    {
        var date = reportDate.Date;
        return await _context.FacilityPermits
            .Where(p => p.FacilityId == facilityId
                        && p.IsActive
                        && p.EffectiveStartDate <= date
                        && (!p.EffectiveEndDate.HasValue || p.EffectiveEndDate.Value >= date))
            .OrderByDescending(p => p.EffectiveStartDate)
            .FirstOrDefaultAsync();
    }
}


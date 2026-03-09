using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class FieldAggregationService : IFieldAggregationService
{
    private readonly ApplicationDbContext _context;

    public FieldAggregationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetFieldMonthVolumeGallonsAsync(Guid sprayfieldId, int month, int year)
    {
        return await _context.MonthlyApplications
            .Where(a => a.Zone != null && a.Zone.SprayfieldId == sprayfieldId)
            .Where(a => a.ApplicationDate.Month == month && a.ApplicationDate.Year == year)
            .SumAsync(a => a.VolumeGallons);
    }

    public async Task<decimal> GetFieldMonthLbsAppliedAsync(Guid sprayfieldId, int month, int year)
    {
        return await _context.LoadCalculations
            .Where(c => c.Application != null && c.Application.Zone != null && c.Application.Zone.SprayfieldId == sprayfieldId)
            .Where(c => c.Application!.ApplicationDate.Month == month && c.Application.ApplicationDate.Year == year)
            .SumAsync(c => c.LbsApplied);
    }
}

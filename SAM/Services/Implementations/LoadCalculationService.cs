using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class LoadCalculationService : ILoadCalculationService
{
    private readonly ApplicationDbContext _context;

    public LoadCalculationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LoadCalculation> RecalculateForApplicationAsync(Guid applicationId)
    {
        var application = await _context.MonthlyApplications
            .Include(a => a.Zone)
            .FirstOrDefaultAsync(a => a.Id == applicationId)
            ?? throw new EntityNotFoundException(nameof(MonthlyApplication), applicationId);

        if (application.Zone == null || application.Zone.Acres <= 0)
        {
            throw new BusinessRuleException("Cannot calculate load because zone acreage is missing or zero.");
        }

        var lbs = application.VolumeGallons * application.NitrogenMgL * 8.34m / 1_000_000m;
        var lbsPerAcre = lbs / application.Zone.Acres;

        var existing = await _context.LoadCalculations
            .FirstOrDefaultAsync(c => c.ApplicationId == applicationId);

        if (existing == null)
        {
            existing = new LoadCalculation
            {
                ApplicationId = applicationId
            };
            _context.LoadCalculations.Add(existing);
        }

        existing.LbsApplied = lbs;
        existing.LbsPerAcre = lbsPerAcre;
        existing.ZoneAcresSnapshot = application.Zone.Acres;
        existing.ZonePercentSnapshot = application.Zone.PercentOfField;
        existing.FormulaVersion = "v1";
        existing.CalculatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }
}

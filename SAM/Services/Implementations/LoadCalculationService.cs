using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Utilities;

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
            .Include(a => a.Sprayfield)
            .FirstOrDefaultAsync(a => a.Id == applicationId)
            ?? throw new EntityNotFoundException(nameof(MonthlyApplication), applicationId);

        var acres = application.Sprayfield is null ? 0m : SprayfieldReportHelper.GetReportAcres(application.Sprayfield);
        if (acres <= 0)
        {
            throw new BusinessRuleException("Cannot calculate load because sprayfield acreage is missing or zero.");
        }

        var lbs = application.VolumeGallons * application.NitrogenMgL * 8.34m / 1_000_000m;
        var lbsPerAcre = lbs / acres;

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
        existing.ZoneAcresSnapshot = acres;
        existing.ZonePercentSnapshot = 100m;
        existing.FormulaVersion = "v1";
        existing.CalculatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }
}

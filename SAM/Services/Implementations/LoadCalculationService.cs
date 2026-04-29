using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
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

        var wwChar = await _context.WWChars
            .AsNoTracking()
            .Where(w => w.FacilityId == application.FacilityId
                        && w.Year == application.ApplicationDate.Year
                        && w.Month == (MonthEnum)application.ApplicationDate.Month)
            .OrderByDescending(w => w.UpdatedDate ?? w.CreatedDate)
            .FirstOrDefaultAsync();

        if (wwChar == null)
        {
            throw new BusinessRuleException(
                $"Cannot calculate load because WWChar record is missing for {application.ApplicationDate:yyyy-MM}.");
        }

        // Use WWChar chemistry directly now that MonthlyApplication no longer stores NitrogenMgL.
        // Prefer TKNN; otherwise fallback to available NO2/NO3/NH3 components.
        var avgNh3 = wwChar.NH3NDaily.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0m).Average();
        var hasNh3 = wwChar.NH3NDaily.Any(v => v.HasValue);
        var hasNo2 = wwChar.NO2N.HasValue;
        var hasNo3 = wwChar.NO3N.HasValue;

        decimal nitrogenMgL;
        if (wwChar.TKNN.HasValue)
        {
            nitrogenMgL = wwChar.TKNN.Value;
        }
        else if (hasNh3 || hasNo2 || hasNo3)
        {
            nitrogenMgL = avgNh3 + (wwChar.NO2N ?? 0m) + (wwChar.NO3N ?? 0m);
        }
        else
        {
            throw new BusinessRuleException(
                $"Cannot calculate load because WWChar nitrogen chemistry is missing for {application.ApplicationDate:yyyy-MM}.");
        }

        var lbs = application.VolumeGallons * nitrogenMgL * 8.34m / 1_000_000m;
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
        existing.FormulaVersion = "v2-wwchar-direct";
        existing.CalculatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return existing;
    }
}

using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class ApplicationComplianceService : IApplicationComplianceService
{
    private const decimal GallonsPerAcreInch = 27152m;

    private readonly ApplicationDbContext _context;
    private readonly IPANCalculationService _panCalculationService;

    public ApplicationComplianceService(ApplicationDbContext context, IPANCalculationService panCalculationService)
    {
        _context = context;
        _panCalculationService = panCalculationService;
    }

    public async Task<ComplianceProjectionResult> GetProjectedComplianceAsync(ComplianceProjectionRequest request)
    {
        var field = await _context.Sprayfields
            .Include(s => s.Crop)
            .FirstOrDefaultAsync(s => s.Id == request.SprayfieldId);

        if (field == null)
        {
            throw new InvalidOperationException("Sprayfield not found.");
        }

        if (request.FacilityId == Guid.Empty)
        {
            throw new InvalidOperationException("Facility is required.");
        }

        var asOfDate = request.ApplicationDate.Date;
        var windowStart = asOfDate.AddDays(-364);
        var facilityInputs = await GetFacilityPanInputsAsync(request.FacilityId);
        var chemistryByMonth = await GetChemistryByMonthAsync(request.FacilityId, windowStart, asOfDate);

        var historicalData = await GetHistoricalDataAsync(
            request.FacilityId,
            field.Id,
            windowStart,
            asOfDate,
            request.ExistingApplicationId,
            chemistryByMonth,
            facilityInputs.MineralizationRate,
            facilityInputs.VolatilizationRate);

        var prospectivePanLbs = CalculatePanLbs(
            request.ApplicationDate,
            request.VolumeGallons,
            request.NitrogenMgL,
            chemistryByMonth,
            facilityInputs.MineralizationRate,
            facilityInputs.VolatilizationRate);
        var projectedPanLbs = historicalData.HistoricalPanLbs + prospectivePanLbs;
        var projectedGallons = historicalData.HistoricalGallons + request.VolumeGallons;

        var metrics = await BuildFieldMetricsAsync(
            request.FacilityId,
            field.Id,
            field.PermitFieldName ?? field.FieldId,
            field.AcresTotal ?? field.SizeAcres,
            field.HydraulicLoadingLimitInPerYr,
            projectedPanLbs,
            projectedGallons,
            asOfDate,
            windowStart);

        var result = new ComplianceProjectionResult
        {
            WindowStartDate = windowStart,
            WindowEndDate = asOfDate,
            SprayfieldId = field.Id,
            SprayfieldName = metrics.SprayfieldName,
            FieldAcres = metrics.FieldAcres,
            HistoricalPanLbs = historicalData.HistoricalPanLbs,
            ProspectivePanLbs = prospectivePanLbs,
            ProjectedPanLbs = projectedPanLbs,
            ProjectedPanLbsPerAcre = metrics.RollingPanLbsPerAcre,
            PanLimitLbsPerAcre = metrics.PanLimitLbsPerAcre,
            PanUtilizationPercent = metrics.PanUtilizationPercent,
            HistoricalGallons = historicalData.HistoricalGallons,
            ProspectiveGallons = request.VolumeGallons,
            ProjectedGallons = projectedGallons,
            ProjectedHydraulicInches = metrics.RollingHydraulicInches,
            HydraulicLimitInchesPerYear = metrics.HydraulicLimitInchesPerYear,
            HydraulicUtilizationPercent = metrics.HydraulicUtilizationPercent,
            Warnings = metrics.Warnings
        };

        result.PanWarnAt85 = result.PanUtilizationPercent.HasValue && result.PanUtilizationPercent.Value >= 85m;
        result.PanExceedsLimit = result.PanUtilizationPercent.HasValue && result.PanUtilizationPercent.Value > 100m;
        result.HydraulicExceedsLimit = result.HydraulicUtilizationPercent.HasValue && result.HydraulicUtilizationPercent.Value > 100m;

        if (result.PanWarnAt85)
        {
            result.Warnings.Add($"WARNING: Applying this load will exceed 85% of the Annual PAN limit for {result.SprayfieldName}.");
        }

        if (result.PanExceedsLimit)
        {
            result.Warnings.Add($"WARNING: Projected PAN load exceeds 100% of the Annual PAN limit for {result.SprayfieldName}.");
        }

        if (result.HydraulicExceedsLimit)
        {
            result.Warnings.Add($"WARNING: Projected hydraulic load exceeds 100% of the annual hydraulic limit for {result.SprayfieldName}.");
        }

        result.RequiresConfirmation = result.Warnings.Count > 0;
        return result;
    }

    public async Task<FieldRollingMetricsResult> GetFieldRollingMetricsAsync(
        Guid facilityId,
        Guid sprayfieldId,
        DateTime asOfDate,
        Guid? excludeApplicationId = null)
    {
        var field = await _context.Sprayfields
            .FirstOrDefaultAsync(s => s.Id == sprayfieldId);

        if (field == null)
        {
            throw new InvalidOperationException("Sprayfield not found.");
        }

        var endDate = asOfDate.Date;
        var startDate = endDate.AddDays(-364);
        var facilityInputs = await GetFacilityPanInputsAsync(facilityId);
        var chemistryByMonth = await GetChemistryByMonthAsync(facilityId, startDate, endDate);
        var historicalData = await GetHistoricalDataAsync(
            facilityId,
            sprayfieldId,
            startDate,
            endDate,
            excludeApplicationId,
            chemistryByMonth,
            facilityInputs.MineralizationRate,
            facilityInputs.VolatilizationRate);

        return await BuildFieldMetricsAsync(
            facilityId,
            sprayfieldId,
            field.PermitFieldName ?? field.FieldId,
            field.AcresTotal ?? field.SizeAcres,
            field.HydraulicLoadingLimitInPerYr,
            historicalData.HistoricalPanLbs,
            historicalData.HistoricalGallons,
            endDate,
            startDate);
    }

    private async Task<(decimal HistoricalPanLbs, decimal HistoricalGallons)> GetHistoricalDataAsync(
        Guid facilityId,
        Guid sprayfieldId,
        DateTime startDate,
        DateTime endDate,
        Guid? excludeApplicationId,
        IReadOnlyDictionary<(int Year, int Month), PanChemistryInputs> chemistryByMonth,
        decimal mineralizationRate,
        decimal volatilizationRate)
    {
        var query = _context.MonthlyApplications
            .AsNoTracking()
            .Include(a => a.Sprayfield)
            .Where(a => a.FacilityId == facilityId
                        && a.SprayfieldId == sprayfieldId
                        && a.ApplicationDate >= startDate
                        && a.ApplicationDate <= endDate);

        if (excludeApplicationId.HasValue)
        {
            query = query.Where(a => a.Id != excludeApplicationId.Value);
        }

        var applications = await query.ToListAsync();
        var historicalGallons = applications.Sum(a => a.VolumeGallons);
        var historicalPanLbs = applications.Sum(a =>
            CalculatePanLbs(
                a.ApplicationDate,
                a.VolumeGallons,
                a.NitrogenMgL,
                chemistryByMonth,
                mineralizationRate,
                volatilizationRate));
        return (historicalPanLbs, historicalGallons);
    }

    private async Task<PanRateInputs> GetFacilityPanInputsAsync(Guid facilityId)
    {
        var facility = await _context.Facilities
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == facilityId);

        if (facility == null)
        {
            throw new InvalidOperationException("Facility not found.");
        }

        return new PanRateInputs(
            (facility.MineralizationRatePercent ?? 40m) / 100m,
            (facility.VolatilizationRatePercent ?? 50m) / 100m);
    }

    private async Task<IReadOnlyDictionary<(int Year, int Month), PanChemistryInputs>> GetChemistryByMonthAsync(
        Guid facilityId,
        DateTime startDate,
        DateTime endDate)
    {
        var minYear = startDate.Year;
        var maxYear = endDate.Year;

        var wwChars = await _context.WWChars
            .AsNoTracking()
            .Where(w => w.FacilityId == facilityId && w.Year >= minYear && w.Year <= maxYear)
            .ToListAsync();

        return wwChars
            .GroupBy(w => (w.Year, (int)w.Month))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g
                        .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                        .First();
                    return new PanChemistryInputs(
                        latest.TKNN,
                        latest.NH3NDaily.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0m).Average(),
                        latest.NO2N,
                        latest.NO3N);
                });
    }

    private decimal CalculatePanLbs(
        DateTime applicationDate,
        decimal volumeGallons,
        decimal fallbackNitrogenMgL,
        IReadOnlyDictionary<(int Year, int Month), PanChemistryInputs> chemistryByMonth,
        decimal mineralizationRate,
        decimal volatilizationRate)
    {
        if (volumeGallons <= 0)
        {
            return 0m;
        }

        var key = (applicationDate.Year, applicationDate.Month);
        if (chemistryByMonth.TryGetValue(key, out var chemistry))
        {
            var tkn = chemistry.TknMgL ?? fallbackNitrogenMgL;
            return _panCalculationService
                .Calculate(tkn, chemistry.Nh3MgL, chemistry.No2MgL, chemistry.No3MgL, mineralizationRate, volatilizationRate, volumeGallons, 1m)
                .PanLbs;
        }

        return _panCalculationService
            .Calculate(fallbackNitrogenMgL, 0m, 0m, 0m, mineralizationRate, volatilizationRate, volumeGallons, 1m)
            .PanLbs;
    }

    private async Task<FieldRollingMetricsResult> BuildFieldMetricsAsync(
        Guid facilityId,
        Guid sprayfieldId,
        string sprayfieldName,
        decimal fieldAcres,
        decimal hydraulicLimit,
        decimal rollingPanLbs,
        decimal rollingGallons,
        DateTime asOfDate,
        DateTime windowStartDate)
    {
        var warnings = new List<string>();
        decimal rollingPanLbsPerAcre = 0m;
        decimal rollingHydraulicInches = 0m;

        if (fieldAcres <= 0)
        {
            warnings.Add($"Field acreage is not configured for {sprayfieldName}; PAN/hydraulic utilization cannot be evaluated.");
        }
        else
        {
            rollingPanLbsPerAcre = rollingPanLbs / fieldAcres;
            rollingHydraulicInches = rollingGallons / (fieldAcres * GallonsPerAcreInch);
        }

        var panLimit = await GetWeightedPanLimitAsync(sprayfieldId);
        if (!panLimit.HasValue || panLimit.Value <= 0)
        {
            warnings.Add($"PAN limit is not configured for {sprayfieldName}; PAN utilization is unavailable.");
            panLimit = null;
        }

        decimal? panUtilization = null;
        if (panLimit.HasValue && panLimit.Value > 0 && fieldAcres > 0)
        {
            panUtilization = rollingPanLbsPerAcre / panLimit.Value * 100m;
        }

        decimal? hydraulicLimitValue = hydraulicLimit > 0 ? hydraulicLimit : null;
        if (!hydraulicLimitValue.HasValue)
        {
            warnings.Add($"Hydraulic limit is not configured for {sprayfieldName}; hydraulic utilization is unavailable.");
        }

        decimal? hydraulicUtilization = null;
        if (hydraulicLimitValue.HasValue && fieldAcres > 0)
        {
            hydraulicUtilization = rollingHydraulicInches / hydraulicLimitValue.Value * 100m;
        }

        return new FieldRollingMetricsResult
        {
            FacilityId = facilityId,
            SprayfieldId = sprayfieldId,
            SprayfieldName = sprayfieldName,
            AsOfDate = asOfDate,
            WindowStartDate = windowStartDate,
            WindowEndDate = asOfDate,
            FieldAcres = fieldAcres,
            RollingPanLbs = rollingPanLbs,
            RollingPanLbsPerAcre = rollingPanLbsPerAcre,
            PanLimitLbsPerAcre = panLimit,
            PanUtilizationPercent = panUtilization,
            RollingGallons = rollingGallons,
            RollingHydraulicInches = rollingHydraulicInches,
            HydraulicLimitInchesPerYear = hydraulicLimitValue,
            HydraulicUtilizationPercent = hydraulicUtilization,
            Warnings = warnings
        };
    }

    private async Task<decimal?> GetWeightedPanLimitAsync(Guid sprayfieldId)
    {
        var sprayfield = await _context.Sprayfields
            .AsNoTracking()
            .Include(s => s.Crop)
            .FirstOrDefaultAsync(s => s.Id == sprayfieldId);

        if (sprayfield?.Crop == null)
        {
            return null;
        }

        var panLimit = sprayfield.Crop.PANLimit;
        if (!panLimit.HasValue || panLimit.Value <= 0)
        {
            panLimit = PanLimitDerivation.DeriveFromNUptake(sprayfield.Crop.NUptake);
        }

        return panLimit > 0 ? panLimit : null;
    }

    private sealed record PanRateInputs(decimal MineralizationRate, decimal VolatilizationRate);
    private sealed record PanChemistryInputs(decimal? TknMgL, decimal? Nh3MgL, decimal? No2MgL, decimal? No3MgL);
}

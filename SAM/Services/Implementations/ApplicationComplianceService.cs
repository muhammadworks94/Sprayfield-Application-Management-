using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class ApplicationComplianceService : IApplicationComplianceService
{
    private const decimal PoundsFactor = 8.34m / 1_000_000m;
    private const decimal GallonsPerAcreInch = 27152m;

    private readonly ApplicationDbContext _context;

    public ApplicationComplianceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ComplianceProjectionResult> GetProjectedComplianceAsync(ComplianceProjectionRequest request)
    {
        var zone = await _context.ApplicationZones
            .Include(z => z.Sprayfield)
            .FirstOrDefaultAsync(z => z.Id == request.ZoneId);

        if (zone?.Sprayfield == null)
        {
            throw new InvalidOperationException("Application zone or sprayfield not found.");
        }

        if (request.FacilityId == Guid.Empty)
        {
            throw new InvalidOperationException("Facility is required.");
        }

        var field = zone.Sprayfield;
        var asOfDate = request.ApplicationDate.Date;
        var windowStart = asOfDate.AddDays(-364);

        var historicalData = await GetHistoricalDataAsync(
            request.FacilityId,
            field.Id,
            windowStart,
            asOfDate,
            request.ExistingApplicationId);

        var prospectivePanLbs = request.VolumeGallons * request.NitrogenMgL * PoundsFactor;
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
        var historicalData = await GetHistoricalDataAsync(facilityId, sprayfieldId, startDate, endDate, excludeApplicationId);

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
        Guid? excludeApplicationId)
    {
        var query = _context.MonthlyApplications
            .AsNoTracking()
            .Include(a => a.Zone)
            .Where(a => a.FacilityId == facilityId
                        && a.Zone != null
                        && a.Zone.SprayfieldId == sprayfieldId
                        && a.ApplicationDate >= startDate
                        && a.ApplicationDate <= endDate);

        if (excludeApplicationId.HasValue)
        {
            query = query.Where(a => a.Id != excludeApplicationId.Value);
        }

        var applications = await query.ToListAsync();
        var historicalGallons = applications.Sum(a => a.VolumeGallons);
        var historicalPanLbs = applications.Sum(a => a.VolumeGallons * a.NitrogenMgL * PoundsFactor);
        return (historicalPanLbs, historicalGallons);
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
        var zones = await _context.ApplicationZones
            .AsNoTracking()
            .Include(z => z.Crop)
            .Where(z => z.SprayfieldId == sprayfieldId && z.Active)
            .ToListAsync();

        if (zones.Count == 0)
        {
            return null;
        }

        decimal weightedLimit = 0m;
        decimal coveredPercent = 0m;

        foreach (var zone in zones)
        {
            var zonePanLimit = zone.Crop?.PANLimit;
            if (!zonePanLimit.HasValue || zonePanLimit.Value <= 0)
            {
                continue;
            }

            coveredPercent += zone.PercentOfField;
            weightedLimit += (zone.PercentOfField / 100m) * zonePanLimit.Value;
        }

        if (coveredPercent <= 0)
        {
            return null;
        }

        return weightedLimit;
    }
}

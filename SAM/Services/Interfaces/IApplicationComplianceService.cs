using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IApplicationComplianceService
{
    Task<ComplianceProjectionResult> GetProjectedComplianceAsync(ComplianceProjectionRequest request);
    Task<FieldRollingMetricsResult> GetFieldRollingMetricsAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, Guid? excludeApplicationId = null);

    /// <summary>
    /// Rolling 365-day hydraulic inches from application volume only (no WWChar/PAN dependency).
    /// Used by NDAR-1 floating totals where PAN chemistry is not required.
    /// </summary>
    Task<decimal> GetFieldRollingHydraulicInchesAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, Guid? excludeApplicationId = null);
}

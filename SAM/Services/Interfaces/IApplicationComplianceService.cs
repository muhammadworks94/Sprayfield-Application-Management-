using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IApplicationComplianceService
{
    Task<ComplianceProjectionResult> GetProjectedComplianceAsync(ComplianceProjectionRequest request);
    Task<FieldRollingMetricsResult> GetFieldRollingMetricsAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, Guid? excludeApplicationId = null);
}

using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for OperatorLog entity operations.
/// </summary>
public interface IOperatorLogService
{
    Task<IEnumerable<OperatorLog>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null);
    Task<OperatorLog?> GetByIdAsync(Guid id);
    Task<Guid> GetCompanyIdAsync(Guid id);
    Task<OperatorLog> CreateAsync(OperatorLog operatorLog);
    Task<OperatorLog> UpdateAsync(OperatorLog operatorLog);
    Task<bool> DeleteAsync(Guid id);
    Task<OperatorLogMutationResult> CreateWithNdarRefreshAsync(OperatorLog operatorLog);
    Task<OperatorLogMutationResult> UpdateWithNdarRefreshAsync(OperatorLog operatorLog);
    Task<(bool Deleted, Guid CompanyId, List<NdarRefreshOutcome> NdarRefreshOutcomes)> DeleteWithNdarRefreshAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<OperatorLog>> GetByFacilityIdAsync(Guid facilityId);
    Task<IEnumerable<OperatorLog>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate);
}



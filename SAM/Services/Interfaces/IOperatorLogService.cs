using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for OperatorLog entity operations.
/// </summary>
public interface IOperatorLogService
{
    Task<IEnumerable<OperatorLog>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null);
    Task<OperatorLog?> GetByIdAsync(Guid id);
    Task<OperatorLog> CreateAsync(OperatorLog operatorLog);
    Task<OperatorLog> UpdateAsync(OperatorLog operatorLog);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<OperatorLog>> GetByFacilityIdAsync(Guid facilityId);
    Task<IEnumerable<OperatorLog>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate);
}



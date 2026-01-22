using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Irrigate (Irrigation Log) entity operations.
/// </summary>
public interface IIrrigateService
{
    Task<IEnumerable<Irrigate>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? sprayfieldId = null);
    Task<Irrigate?> GetByIdAsync(Guid id);
    Task<Irrigate> CreateAsync(Irrigate irrigate);
    Task<Irrigate> UpdateAsync(Irrigate irrigate);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Irrigate>> GetBySprayfieldIdAsync(Guid sprayfieldId);
    Task<IEnumerable<Irrigate>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate);
}


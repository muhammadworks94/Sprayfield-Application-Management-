using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Facility entity operations.
/// </summary>
public interface IFacilityService
{
    Task<IEnumerable<Facility>> GetAllAsync(Guid? companyId = null);
    Task<Facility?> GetByIdAsync(Guid id);
    Task<Facility> CreateAsync(Facility facility);
    Task<Facility> UpdateAsync(Facility facility);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Facility>> GetByCompanyIdAsync(Guid companyId);
}


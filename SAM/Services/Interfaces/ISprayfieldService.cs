using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Sprayfield entity operations.
/// </summary>
public interface ISprayfieldService
{
    Task<IEnumerable<Sprayfield>> GetAllAsync(Guid? companyId = null);
    Task<Sprayfield?> GetByIdAsync(Guid id);
    Task<Sprayfield> CreateAsync(Sprayfield sprayfield);
    Task<Sprayfield> UpdateAsync(Sprayfield sprayfield);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Sprayfield>> GetByCompanyIdAsync(Guid companyId);
    Task<IEnumerable<Sprayfield>> GetByFacilityIdAsync(Guid facilityId);
}



using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for WWChar (Wastewater Characteristics) entity operations.
/// </summary>
public interface IWWCharService
{
    Task<IEnumerable<WWChar>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null);
    Task<WWChar?> GetByIdAsync(Guid id);
    Task<WWChar> CreateAsync(WWChar wwChar);
    Task<WWChar> UpdateAsync(WWChar wwChar);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<WWChar?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year);
    Task<IEnumerable<WWChar>> GetByFacilityIdAsync(Guid facilityId);
}


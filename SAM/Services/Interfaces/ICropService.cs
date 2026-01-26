using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Crop entity operations.
/// </summary>
public interface ICropService
{
    Task<IEnumerable<Crop>> GetAllAsync(Guid? companyId = null);
    Task<Crop?> GetByIdAsync(Guid id);
    Task<Crop> CreateAsync(Crop crop);
    Task<Crop> UpdateAsync(Crop crop);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Crop>> GetByCompanyIdAsync(Guid companyId);
}



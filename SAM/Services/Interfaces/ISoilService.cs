using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Soil entity operations.
/// </summary>
public interface ISoilService
{
    Task<IEnumerable<Soil>> GetAllAsync(Guid? companyId = null);
    Task<Soil?> GetByIdAsync(Guid id);
    Task<Soil> CreateAsync(Soil soil);
    Task<Soil> UpdateAsync(Soil soil);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Soil>> GetByCompanyIdAsync(Guid companyId);
}



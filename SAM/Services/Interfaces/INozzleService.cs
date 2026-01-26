using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for Nozzle entity operations.
/// </summary>
public interface INozzleService
{
    Task<IEnumerable<Nozzle>> GetAllAsync(Guid? companyId = null);
    Task<Nozzle?> GetByIdAsync(Guid id);
    Task<Nozzle> CreateAsync(Nozzle nozzle);
    Task<Nozzle> UpdateAsync(Nozzle nozzle);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<Nozzle>> GetByCompanyIdAsync(Guid companyId);
}



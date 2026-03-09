using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface IApplicationZoneService
{
    Task<IEnumerable<ApplicationZone>> GetBySprayfieldIdAsync(Guid sprayfieldId);
    Task<ApplicationZone?> GetByIdAsync(Guid id);
    Task<ApplicationZone> CreateAsync(ApplicationZone zone);
    Task<ApplicationZone> UpdateAsync(ApplicationZone zone);
    Task<bool> DeleteAsync(Guid id);
    Task ValidatePercentTotalAsync(Guid sprayfieldId, Guid? excludeZoneId = null);
    Task RecalculateForSprayfieldAsync(Guid sprayfieldId);
}

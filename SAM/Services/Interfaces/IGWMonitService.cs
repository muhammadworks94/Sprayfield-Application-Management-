using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for GWMonit (Groundwater Monitoring) entity operations.
/// </summary>
public interface IGWMonitService
{
    Task<IEnumerable<GWMonit>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? monitoringWellId = null);
    Task<GWMonit?> GetByIdAsync(Guid id);
    Task<GWMonit> CreateAsync(GWMonit gwMonit);
    Task<GWMonit> UpdateAsync(GWMonit gwMonit);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<GWMonit>> GetByMonitoringWellIdAsync(Guid monitoringWellId);
    Task<IEnumerable<GWMonit>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate);
}


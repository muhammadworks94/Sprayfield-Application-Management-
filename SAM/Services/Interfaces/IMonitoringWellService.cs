using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for MonitoringWell entity operations.
/// </summary>
public interface IMonitoringWellService
{
    Task<IEnumerable<MonitoringWell>> GetAllAsync(Guid? companyId = null);
    Task<MonitoringWell?> GetByIdAsync(Guid id);
    Task<MonitoringWell> CreateAsync(MonitoringWell monitoringWell);
    Task<MonitoringWell> UpdateAsync(MonitoringWell monitoringWell);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<MonitoringWell>> GetByCompanyIdAsync(Guid companyId);
}


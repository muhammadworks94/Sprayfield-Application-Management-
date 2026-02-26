using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Shared lookup queries for UI select-list data.
/// </summary>
public interface ILookupQueryService
{
    Task<IEnumerable<Company>> GetCompaniesAsync(Guid? effectiveCompanyId = null);
    Task<IEnumerable<Facility>> GetFacilitiesAsync(Guid? companyId = null);
    Task<IEnumerable<Sprayfield>> GetSprayfieldsAsync(Guid? companyId = null, Guid? facilityId = null);
    Task<IEnumerable<MonitoringWell>> GetMonitoringWellsAsync(Guid? companyId = null, Guid? facilityId = null);
}

using SAM.Domain.Entities;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for reusable lookup queries.
/// </summary>
public class LookupQueryService : ILookupQueryService
{
    private readonly ICompanyService _companyService;
    private readonly IFacilityService _facilityService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IMonitoringWellService _monitoringWellService;

    public LookupQueryService(
        ICompanyService companyService,
        IFacilityService facilityService,
        ISprayfieldService sprayfieldService,
        IMonitoringWellService monitoringWellService)
    {
        _companyService = companyService;
        _facilityService = facilityService;
        _sprayfieldService = sprayfieldService;
        _monitoringWellService = monitoringWellService;
    }

    public async Task<IEnumerable<Company>> GetCompaniesAsync(Guid? effectiveCompanyId = null)
    {
        var companies = await _companyService.GetAllAsync();
        if (effectiveCompanyId.HasValue)
        {
            companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
        }

        return companies;
    }

    public async Task<IEnumerable<Facility>> GetFacilitiesAsync(Guid? companyId = null)
    {
        return await _facilityService.GetAllAsync(companyId);
    }

    public async Task<IEnumerable<Sprayfield>> GetSprayfieldsAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var sprayfields = await _sprayfieldService.GetAllAsync(companyId);
        if (facilityId.HasValue)
        {
            sprayfields = sprayfields.Where(s => s.FacilityId == facilityId.Value);
        }

        return sprayfields;
    }

    public async Task<IEnumerable<MonitoringWell>> GetMonitoringWellsAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        return await _monitoringWellService.GetAllAsync(companyId);
    }
}

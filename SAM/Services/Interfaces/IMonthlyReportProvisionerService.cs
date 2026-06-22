using SAM.Services.Models;

namespace SAM.Services.Interfaces;

/// <summary>
/// Idempotently ensures monthly report records exist when operational data is saved.
/// GW-59 has no persisted monthly report entity; GWMonit records surface directly in Reports.
/// </summary>
public interface IMonthlyReportProvisionerService
{
    Task<NdarRefreshOutcome> EnsureNdar1ForMonthAsync(Guid facilityId, int month, int year);
    Task EnsureNdmrForMonthAsync(Guid facilityId, int month, int year);
    Task EnsureNdmlrForMonthAsync(Guid facilityId, int month, int year);
}

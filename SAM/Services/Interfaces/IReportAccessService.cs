namespace SAM.Services.Interfaces;

/// <summary>
/// Report types whose access can be controlled per facility permit.
/// </summary>
public enum ReportKind
{
    Ndar1,
    Ndmr,
    Ndmlr,
    Gw59
}

/// <summary>
/// Result of evaluating whether a facility may use a given report type today.
/// </summary>
public class FacilityReportAccess
{
    public bool HasValidPermit { get; init; }
    public bool ReportEnabled { get; init; }
    public string? Reason { get; init; }

    public bool IsAllowed => HasValidPermit && ReportEnabled;
}

/// <summary>
/// Which report types are available anywhere in a company (at least one facility
/// with a permit valid today and the report enabled).
/// </summary>
public class CompanyReportVisibility
{
    public bool ShowNdar1 { get; init; }
    public bool ShowNdmr { get; init; }
    public bool ShowNdmlr { get; init; }
    public bool ShowGw59 { get; init; }

    public bool AnyShown => ShowNdar1 || ShowNdmr || ShowNdmlr || ShowGw59;

    public bool IsShown(ReportKind kind) => kind switch
    {
        ReportKind.Ndar1 => ShowNdar1,
        ReportKind.Ndmr => ShowNdmr,
        ReportKind.Ndmlr => ShowNdmlr,
        ReportKind.Gw59 => ShowGw59,
        _ => true
    };
}

public interface IReportAccessService
{
    /// <summary>
    /// Evaluates report access for a single facility as of today.
    /// </summary>
    Task<FacilityReportAccess> GetFacilityAccessAsync(Guid facilityId, ReportKind kind);

    /// <summary>
    /// Evaluates report access for a set of facilities as of today (single query).
    /// Every requested facility id is present in the result.
    /// </summary>
    Task<Dictionary<Guid, FacilityReportAccess>> GetFacilityAccessMapAsync(IReadOnlyCollection<Guid> facilityIds, ReportKind kind);

    /// <summary>
    /// Evaluates, per report type, whether any facility in the company is allowed as of today.
    /// Companies with no permit records at all see every report type (not yet configured).
    /// </summary>
    Task<CompanyReportVisibility> GetCompanyReportVisibilityAsync(Guid companyId);
}

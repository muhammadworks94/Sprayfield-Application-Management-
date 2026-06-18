using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Represents an environmental monitoring or treatment facility managed by a company.
/// </summary>
public class Facility : CompanyScopedEntity
{
    /// <summary>
    /// Name of the facility.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Name of the permit holder.
    /// </summary>
    public string Permittee { get; set; } = string.Empty;

    /// <summary>
    /// Classification of the facility.
    /// </summary>
    public string FacilityClass { get; set; } = string.Empty;

    // Permit / facility contact & regulatory metadata (from Facility Data Entry Form)

    /// <summary>
    /// General permittee contact phone number.
    /// </summary>
    public string? PermitPhone { get; set; }

    /// <summary>
    /// Facility phone number.
    /// </summary>
    public string? FacilityPhone { get; set; } = string.Empty;

    /// <summary>
    /// Facility contact person for regulatory reports (e.g. GW-59).
    /// </summary>
    public string? FacilityContactPerson { get; set; }

    /// <summary>
    /// Phone number for the facility contact person.
    /// </summary>
    public string? FacilityContactPersonPhone { get; set; }

    /// <summary>
    /// Operator in Responsible Charge (ORC) name.
    /// </summary>
    public string? OrcName { get; set; } = string.Empty;

    /// <summary>
    /// ORC operator grade.
    /// </summary>
    public string? OperatorGrade { get; set; } = string.Empty;

    /// <summary>
    /// ORC operator number.
    /// </summary>
    public string? OperatorNumber { get; set; } = string.Empty;

    /// <summary>
    /// ORC operator phone number.
    /// </summary>
    public string? OperatorPhone { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether there was a change in ORC.
    /// </summary>
    public bool? ChangeInOrc { get; set; } = false;

    /// <summary>
    /// Person(s) collecting samples.
    /// </summary>
    public string? PersonsCollectingSamples { get; set; }

    /// <summary>
    /// Mineralization rate (%), used for PAN calculation. Default 40%.
    /// </summary>
    public decimal? MineralizationRatePercent { get; set; }

    /// <summary>
    /// Volatilization rate (%), used for PAN calculation. Default 50%.
    /// </summary>
    public decimal? VolatilizationRatePercent { get; set; }

    /// <summary>
    /// Default certified laboratory for GW-59 and related exports.
    /// </summary>
    public Guid? DefaultLabOptionId { get; set; }

    /// <summary>
    /// Default permit version assigned to this facility.
    /// </summary>
    public Guid? DefaultFacilityPermitId { get; set; }

    public CompanyLabOption? DefaultLabOption { get; set; }
    public FacilityPermit? DefaultFacilityPermit { get; set; }

    // Navigation properties
    public ICollection<WWChar> WWChars { get; set; } = new List<WWChar>();
    public ICollection<GWMonit> GWMonits { get; set; } = new List<GWMonit>();
    public ICollection<MonthlyApplication> MonthlyApplications { get; set; } = new List<MonthlyApplication>();
    public ICollection<IrrRprt> IrrRprts { get; set; } = new List<IrrRprt>();
    public ICollection<NDMLR> NDMLRs { get; set; } = new List<NDMLR>();
    public ICollection<OperatorLog> OperatorLogs { get; set; } = new List<OperatorLog>();
    public ICollection<Sprayfield> Sprayfields { get; set; } = new List<Sprayfield>();
    public ICollection<NDAR1> NDAR1s { get; set; } = new List<NDAR1>();
    public ICollection<FacilityPermit> FacilityPermits { get; set; } = new List<FacilityPermit>();
}



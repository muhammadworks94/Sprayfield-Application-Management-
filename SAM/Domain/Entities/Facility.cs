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
    /// Facility's permit number.
    /// </summary>
    public string PermitNumber { get; set; } = string.Empty;

    /// <summary>
    /// Name of the permit holder.
    /// </summary>
    public string Permittee { get; set; } = string.Empty;

    /// <summary>
    /// Classification of the facility.
    /// </summary>
    public string FacilityClass { get; set; } = string.Empty;

    /// <summary>
    /// Street address.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// City.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Zip code.
    /// </summary>
    public string ZipCode { get; set; } = string.Empty;

    /// <summary>
    /// County.
    /// </summary>
    public string County { get; set; } = string.Empty;

    // Permit / facility contact & regulatory metadata (from Facility Data Entry Form)

    /// <summary>
    /// Permit expiration date.
    /// </summary>
    public DateTime? PermitExpirationDate { get; set; }

    /// <summary>
    /// General permittee contact phone number.
    /// </summary>
    public string? PermitPhone { get; set; }

    /// <summary>
    /// Facility phone number.
    /// </summary>
    public string? FacilityPhone { get; set; } = string.Empty;

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
    /// Indicates whether there was a change in ORC.
    /// </summary>
    public bool? ChangeInOrc { get; set; } = false;

    /// <summary>
    /// Total number of sprayfields for this facility.
    /// </summary>
    public int? TotalNumberOfSprayfields { get; set; }

    /// <summary>
    /// Name of certified laboratory #1.
    /// </summary>
    public string? CertifiedLaboratory1Name { get; set; }

    /// <summary>
    /// Name of certified laboratory #2.
    /// </summary>
    public string? CertifiedLaboratory2Name { get; set; }

    /// <summary>
    /// Certification number for laboratory #1.
    /// </summary>
    public string? LabCertificationNumber1 { get; set; }

    /// <summary>
    /// Certification number for laboratory #2.
    /// </summary>
    public string? LabCertificationNumber2 { get; set; }

    /// <summary>
    /// Person(s) collecting samples.
    /// </summary>
    public string? PersonsCollectingSamples { get; set; }

    /// <summary>
    /// Permitted minimum freeboard in feet.
    /// </summary>
    public decimal? PermittedMinimumFreeboardFeet { get; set; }

    // Navigation properties
    public ICollection<WWChar> WWChars { get; set; } = new List<WWChar>();
    public ICollection<GWMonit> GWMonits { get; set; } = new List<GWMonit>();
    public ICollection<Irrigate> Irrigates { get; set; } = new List<Irrigate>();
    public ICollection<IrrRprt> IrrRprts { get; set; } = new List<IrrRprt>();
    public ICollection<OperatorLog> OperatorLogs { get; set; } = new List<OperatorLog>();
    public ICollection<Sprayfield> Sprayfields { get; set; } = new List<Sprayfield>();
    public ICollection<NDAR1> NDAR1s { get; set; } = new List<NDAR1>();
}



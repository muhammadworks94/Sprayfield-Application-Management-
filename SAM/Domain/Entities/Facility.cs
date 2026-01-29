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

    // Navigation properties
    public ICollection<WWChar> WWChars { get; set; } = new List<WWChar>();
    public ICollection<GWMonit> GWMonits { get; set; } = new List<GWMonit>();
    public ICollection<Irrigate> Irrigates { get; set; } = new List<Irrigate>();
    public ICollection<IrrRprt> IrrRprts { get; set; } = new List<IrrRprt>();
    public ICollection<OperatorLog> OperatorLogs { get; set; } = new List<OperatorLog>();
    public ICollection<Sprayfield> Sprayfields { get; set; } = new List<Sprayfield>();
    public ICollection<NDAR1> NDAR1s { get; set; } = new List<NDAR1>();
}



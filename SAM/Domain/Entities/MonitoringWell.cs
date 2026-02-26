using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores information about groundwater monitoring wells.
/// </summary>
public class MonitoringWell : CompanyScopedEntity
{
    /// <summary>
    /// Unique identifier for the well (Well Identifier #).
    /// </summary>
    public string WellId { get; set; } = string.Empty;

    /// <summary>
    /// Well permit number.
    /// </summary>
    public string? WellPermitNumber { get; set; }

    /// <summary>
    /// Description of the well's location (Well Location).
    /// </summary>
    public string LocationDescription { get; set; } = string.Empty;

    /// <summary>
    /// Diameter in inches.
    /// </summary>
    public decimal? DiameterInches { get; set; }

    /// <summary>
    /// Well depth in feet.
    /// </summary>
    public decimal? WellDepthFeet { get; set; }

    /// <summary>
    /// Depth to screen in feet.
    /// </summary>
    public decimal? DepthToScreenFeet { get; set; }

    /// <summary>
    /// Low screen depth in feet.
    /// </summary>
    public decimal? LowScreenDepthFeet { get; set; }

    /// <summary>
    /// High screen depth in feet.
    /// </summary>
    public decimal? HighScreenDepthFeet { get; set; }

    /// <summary>
    /// Top of casing elevation in msl.
    /// </summary>
    public decimal? TopOfCasingElevationMsl { get; set; }

    /// <summary>
    /// Location of well in regards to treatment system (Influent 98 / Effluent 99).
    /// </summary>
    public TreatmentSystemLocationEnum? TreatmentSystemLocation { get; set; }

    /// <summary>
    /// Number of wells to be sampled.
    /// </summary>
    public int? NumberOfWellsToBeSampled { get; set; }

    /// <summary>
    /// Latitude coordinate (optional).
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate (optional).
    /// </summary>
    public decimal? Longitude { get; set; }

    // Navigation properties
    public ICollection<GWMonit> GWMonits { get; set; } = new List<GWMonit>();
}



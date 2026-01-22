using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores information about groundwater monitoring wells.
/// </summary>
public class MonitoringWell : CompanyScopedEntity
{
    /// <summary>
    /// Unique identifier for the well.
    /// </summary>
    public string WellId { get; set; } = string.Empty;

    /// <summary>
    /// Description of the well's location.
    /// </summary>
    public string LocationDescription { get; set; } = string.Empty;

    /// <summary>
    /// Latitude coordinate.
    /// </summary>
    public decimal Latitude { get; set; }

    /// <summary>
    /// Longitude coordinate.
    /// </summary>
    public decimal Longitude { get; set; }

    // Navigation properties
    public ICollection<GWMonit> GWMonits { get; set; } = new List<GWMonit>();
}


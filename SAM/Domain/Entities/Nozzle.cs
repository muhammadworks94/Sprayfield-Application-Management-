using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores details about irrigation nozzles used in sprayfields.
/// </summary>
public class Nozzle : CompanyScopedEntity
{
    /// <summary>
    /// Nozzle model name or number.
    /// </summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Manufacturer of the nozzle.
    /// </summary>
    public string Manufacturer { get; set; } = string.Empty;

    /// <summary>
    /// Flow rate in gallons per minute.
    /// </summary>
    public decimal FlowRateGpm { get; set; }

    /// <summary>
    /// Operating pressure value.
    /// </summary>
    public decimal? Pressure { get; set; }

    /// <summary>
    /// Spray arc in degrees.
    /// </summary>
    public int SprayArc { get; set; }

    /// <summary>
    /// Optional comment or notes about the nozzle.
    /// </summary>
    public string? Comment { get; set; }

    // Navigation properties
    public ICollection<ApplicationZone> ApplicationZones { get; set; } = new List<ApplicationZone>();
}



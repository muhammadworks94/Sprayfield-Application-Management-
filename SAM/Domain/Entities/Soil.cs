using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Defines different soil types relevant to sprayfield operations.
/// </summary>
public class Soil : CompanyScopedEntity
{
    /// <summary>
    /// Name of the soil type (e.g., Cecil Sandy Loam).
    /// </summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the soil type.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Soil permeability rate in inches per hour.
    /// </summary>
    public decimal Permeability { get; set; }

    // Navigation properties
    public ICollection<Sprayfield> Sprayfields { get; set; } = new List<Sprayfield>();
}



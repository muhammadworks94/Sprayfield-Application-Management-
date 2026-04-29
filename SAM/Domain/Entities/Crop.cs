using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Defines types of crops grown in sprayfields, with relevant environmental parameters.
/// </summary>
public class Crop : CompanyScopedEntity
{
    /// <summary>
    /// Name of the crop (e.g., Fescue).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nitrogen uptake rate in lbs/acre/year.
    /// </summary>
    public decimal NUptake { get; set; }

    /// <summary>
    /// Annual PAN load limit (lbs/acre/year).
    /// </summary>
    public decimal? PANLimit { get; set; }

    // Navigation properties
    public ICollection<Sprayfield> Sprayfields { get; set; } = new List<Sprayfield>();
}



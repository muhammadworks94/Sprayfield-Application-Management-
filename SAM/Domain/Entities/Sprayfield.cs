using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Represents an individual land area where wastewater is irrigated.
/// Links to other foundational entities.
/// </summary>
public class Sprayfield : CompanyScopedEntity
{
    /// <summary>
    /// Unique identifier for the sprayfield.
    /// </summary>
    public string FieldId { get; set; } = string.Empty;

    /// <summary>
    /// Size of the field in acres.
    /// </summary>
    public decimal SizeAcres { get; set; }

    /// <summary>
    /// Reference to the Soil entity.
    /// </summary>
    public Guid SoilId { get; set; }

    /// <summary>
    /// Reference to the Crop entity.
    /// </summary>
    public Guid CropId { get; set; }

    /// <summary>
    /// Reference to the Nozzle entity.
    /// </summary>
    public Guid NozzleId { get; set; }

    /// <summary>
    /// Annual hydraulic loading limit in inches per year.
    /// </summary>
    public decimal HydraulicLoadingLimitInPerYr { get; set; }

    /// <summary>
    /// Hourly irrigation rate in inches.
    /// </summary>
    public decimal? HourlyRateInches { get; set; }

    /// <summary>
    /// Annual irrigation rate in inches.
    /// </summary>
    public decimal? AnnualRateInches { get; set; }

    // Navigation properties
    public Soil? Soil { get; set; }
    public Crop? Crop { get; set; }
    public Nozzle? Nozzle { get; set; }
    public Facility? Facility { get; set; }
    public Guid? FacilityId { get; set; }
    public ICollection<Irrigate> Irrigates { get; set; } = new List<Irrigate>();
}



using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Records details for each individual irrigation event.
/// </summary>
public class Irrigate : CompanyScopedEntity
{
    /// <summary>
    /// Reference to the Facility entity.
    /// </summary>
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Reference to the Sprayfield entity.
    /// </summary>
    public Guid SprayfieldId { get; set; }

    /// <summary>
    /// Date of irrigation.
    /// </summary>
    public DateTime IrrigationDate { get; set; }

    /// <summary>
    /// Start time of irrigation.
    /// </summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>
    /// End time of irrigation.
    /// </summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>
    /// Duration of irrigation in hours.
    /// </summary>
    public decimal DurationHours { get; set; }

    /// <summary>
    /// Flow rate in gallons per minute.
    /// </summary>
    public decimal FlowRateGpm { get; set; }

    /// <summary>
    /// Total volume of water applied in gallons.
    /// </summary>
    public decimal TotalVolumeGallons { get; set; }

    /// <summary>
    /// Application rate in inches.
    /// </summary>
    public decimal ApplicationRateInches { get; set; }

    /// <summary>
    /// Wind speed during irrigation.
    /// </summary>
    public decimal? WindSpeed { get; set; }

    /// <summary>
    /// Wind direction during irrigation.
    /// </summary>
    public string WindDirection { get; set; } = string.Empty;

    /// <summary>
    /// Weather conditions during irrigation.
    /// </summary>
    public string WeatherConditions { get; set; } = string.Empty;

    /// <summary>
    /// Name of the operator.
    /// </summary>
    public string Operator { get; set; } = string.Empty;

    /// <summary>
    /// Additional comments.
    /// </summary>
    public string Comments { get; set; } = string.Empty;

    // Navigation properties
    public Facility? Facility { get; set; }
    public Sprayfield? Sprayfield { get; set; }
}



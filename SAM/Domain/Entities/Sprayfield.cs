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
    /// Permit field name shown on NDMLR.
    /// </summary>
    public string? PermitFieldName { get; set; }

    /// <summary>
    /// Field-level permit reference.
    /// </summary>
    public string? PermitNumber { get; set; }

    /// <summary>
    /// Size of the wetted area in acres.
    /// </summary>
    public decimal SizeAcres { get; set; }

    /// <summary>
    /// Total acres for the field.
    /// </summary>
    public decimal? AcresTotal { get; set; }

    /// <summary>
    /// Active flag for historical field handling.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Annual hydraulic loading limit in inches per year.
    /// </summary>
    public decimal HydraulicLoadingLimitInPerYr { get; set; }

    /// <summary>
    /// Hourly irrigation rate in inches.
    /// </summary>
    public decimal? HourlyRateInches { get; set; }

    /// <summary>
    /// Weekly irrigation rate in inches per week.
    /// </summary>
    public decimal? WeeklyRateInches { get; set; }

    // Navigation properties
    public Facility? Facility { get; set; }
    public Guid? FacilityId { get; set; }
    public ICollection<ApplicationZone> ApplicationZones { get; set; } = new List<ApplicationZone>();
}



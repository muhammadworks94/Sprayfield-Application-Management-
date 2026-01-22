using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores monthly aggregated irrigation compliance report data.
/// </summary>
public class IrrRprt : CompanyScopedEntity
{
    /// <summary>
    /// Reference to the Facility entity.
    /// </summary>
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Month of the report.
    /// </summary>
    public MonthEnum Month { get; set; }

    /// <summary>
    /// Year of the report.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Total volume applied.
    /// </summary>
    public decimal TotalVolumeApplied { get; set; }

    /// <summary>
    /// Total application rate.
    /// </summary>
    public decimal TotalApplicationRate { get; set; }

    /// <summary>
    /// Hydraulic loading rate.
    /// </summary>
    public decimal HydraulicLoadingRate { get; set; }

    /// <summary>
    /// Nitrogen loading rate.
    /// </summary>
    public decimal NitrogenLoadingRate { get; set; }

    /// <summary>
    /// PAN uptake rate.
    /// </summary>
    public decimal PanUptakeRate { get; set; }

    /// <summary>
    /// Application efficiency.
    /// </summary>
    public decimal ApplicationEfficiency { get; set; }

    /// <summary>
    /// Summary of weather conditions.
    /// </summary>
    public string WeatherSummary { get; set; } = string.Empty;

    /// <summary>
    /// Operational notes.
    /// </summary>
    public string OperationalNotes { get; set; } = string.Empty;

    /// <summary>
    /// Compliance status.
    /// </summary>
    public ComplianceStatusEnum ComplianceStatus { get; set; }

    // Navigation properties
    public Facility? Facility { get; set; }
}


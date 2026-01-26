using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores individual groundwater sample monitoring data.
/// </summary>
public class GWMonit : CompanyScopedEntity
{
    /// <summary>
    /// Reference to the Facility entity.
    /// </summary>
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Reference to the MonitoringWell entity.
    /// </summary>
    public Guid MonitoringWellId { get; set; }

    /// <summary>
    /// Date of sample collection.
    /// </summary>
    public DateTime SampleDate { get; set; }

    /// <summary>
    /// Depth of sample collection.
    /// </summary>
    public decimal? SampleDepth { get; set; }

    /// <summary>
    /// Water level.
    /// </summary>
    public decimal? WaterLevel { get; set; }

    /// <summary>
    /// Temperature.
    /// </summary>
    public decimal? Temperature { get; set; }

    /// <summary>
    /// pH level.
    /// </summary>
    public decimal? PH { get; set; }

    /// <summary>
    /// Conductivity.
    /// </summary>
    public decimal? Conductivity { get; set; }

    /// <summary>
    /// Total Dissolved Solids.
    /// </summary>
    public decimal? TDS { get; set; }

    /// <summary>
    /// Turbidity.
    /// </summary>
    public decimal? Turbidity { get; set; }

    /// <summary>
    /// Biological Oxygen Demand (5-day).
    /// </summary>
    public decimal? BOD5 { get; set; }

    /// <summary>
    /// Chemical Oxygen Demand.
    /// </summary>
    public decimal? COD { get; set; }

    /// <summary>
    /// Total Suspended Solids.
    /// </summary>
    public decimal? TSS { get; set; }

    /// <summary>
    /// Ammonia-Nitrogen.
    /// </summary>
    public decimal? NH3N { get; set; }

    /// <summary>
    /// Nitrate-Nitrogen.
    /// </summary>
    public decimal? NO3N { get; set; }

    /// <summary>
    /// Total Kjeldahl Nitrogen.
    /// </summary>
    public decimal? TKN { get; set; }

    /// <summary>
    /// Total Phosphorus.
    /// </summary>
    public decimal? TotalPhosphorus { get; set; }

    /// <summary>
    /// Chloride.
    /// </summary>
    public decimal? Chloride { get; set; }

    /// <summary>
    /// Fecal Coliform count.
    /// </summary>
    public decimal? FecalColiform { get; set; }

    /// <summary>
    /// Total Coliform count.
    /// </summary>
    public decimal? TotalColiform { get; set; }

    /// <summary>
    /// Lab certification details.
    /// </summary>
    public string LabCertification { get; set; } = string.Empty;

    /// <summary>
    /// Name of the person who collected the sample.
    /// </summary>
    public string CollectedBy { get; set; } = string.Empty;

    /// <summary>
    /// Name of the person who analyzed the sample.
    /// </summary>
    public string AnalyzedBy { get; set; } = string.Empty;

    /// <summary>
    /// Additional comments.
    /// </summary>
    public string Comments { get; set; } = string.Empty;

    // Navigation properties
    public Facility? Facility { get; set; }
    public MonitoringWell? MonitoringWell { get; set; }
}



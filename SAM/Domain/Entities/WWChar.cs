using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Records monthly wastewater quality data for a facility.
/// Daily arrays are stored as JSON columns.
/// </summary>
public class WWChar : CompanyScopedEntity
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
    /// Array of daily BOD5 values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> BOD5Daily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily TSS values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> TSSDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily flow rate values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> FlowRateDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily pH values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> PHDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily Ammonia-Nitrogen values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> NH3NDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily Fecal Coliform values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> FecalColiformDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily Total Coliform values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> TotalColiformDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily Chloride values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> ChlorideDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily TDS values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> TDSDaily { get; set; } = new List<decimal?>();

    /// <summary>
    /// Array of daily composite sample times (max 31 items, stored as JSON).
    /// </summary>
    public List<string?> CompositeTime { get; set; } = new List<string?>();

    /// <summary>
    /// Array indicating if ORC was on site daily (max 31 items, stored as JSON).
    /// </summary>
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new List<ORCOnSiteEnum?>();

    /// <summary>
    /// Array of daily lagoon freeboard values (max 31 items, stored as JSON).
    /// </summary>
    public List<decimal?> LagoonFreeboard { get; set; } = new List<decimal?>();

    /// <summary>
    /// Lab certification details.
    /// </summary>
    public string LabCertification { get; set; } = string.Empty;

    /// <summary>
    /// Name of the person who collected samples.
    /// </summary>
    public string CollectedBy { get; set; } = string.Empty;

    /// <summary>
    /// Name of the person who analyzed samples.
    /// </summary>
    public string AnalyzedBy { get; set; } = string.Empty;

    // Navigation properties
    public Facility? Facility { get; set; }
}


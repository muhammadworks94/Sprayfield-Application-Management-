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
    /// Gallons pumped during sampling.
    /// </summary>
    public decimal? GallonsPumped { get; set; }

    /// <summary>
    /// Odor observation (None/Slight/Strong).
    /// </summary>
    public string Odor { get; set; } = string.Empty;

    /// <summary>
    /// Appearance observation (Clear/Turbid).
    /// </summary>
    public string Appearance { get; set; } = string.Empty;

    /// <summary>
    /// Specific conductance.
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
    /// Total Organic Carbon.
    /// </summary>
    public decimal? TOC { get; set; }

    /// <summary>
    /// Chloride.
    /// </summary>
    public decimal? Chloride { get; set; }

    /// <summary>
    /// Calcium.
    /// </summary>
    public decimal? Calcium { get; set; }

    /// <summary>
    /// Magnesium.
    /// </summary>
    public decimal? Magnesium { get; set; }

    /// <summary>
    /// Indicates if metals samples were collected unfiltered.
    /// </summary>
    public bool? MetalsSamplesCollectedUnfiltered { get; set; }

    /// <summary>
    /// Indicates if metal samples were field acidified.
    /// </summary>
    public bool? MetalSamplesFieldAcidified { get; set; }

    /// <summary>
    /// Fecal Coliform count.
    /// </summary>
    public decimal? FecalColiform { get; set; }

    /// <summary>
    /// Total Coliform count.
    /// </summary>
    public decimal? TotalColiform { get; set; }

    /// <summary>
    /// Indicates if VOC report is attached.
    /// </summary>
    public bool? VOCReportAttached { get; set; }

    /// <summary>
    /// VOC analytical method number, if tested.
    /// </summary>
    public string VOCMethodNumber { get; set; } = string.Empty;

    /// <summary>
    /// Stored relative path for uploaded VOC report (PDF).
    /// </summary>
    public string? VOCReportFileStoragePath { get; set; }

    /// <summary>
    /// Original uploaded VOC report file name.
    /// </summary>
    public string? VOCReportFileName { get; set; }

    /// <summary>
    /// Uploaded VOC report content type.
    /// </summary>
    public string? VOCReportContentType { get; set; }

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

    /// <summary>
    /// GW-59A questionnaire response for question 1 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion1Response { get; set; }

    /// <summary>
    /// GW-59A questionnaire response for question 2 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion2Response { get; set; }

    /// <summary>
    /// GW-59A questionnaire response for question 3 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion3Response { get; set; }

    /// <summary>
    /// GW-59A questionnaire response for question 4 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion4Response { get; set; }

    /// <summary>
    /// GW-59A questionnaire response for question 5 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion5Response { get; set; }

    /// <summary>
    /// GW-59A questionnaire response for question 6 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion6Response { get; set; }

    /// <summary>
    /// GW-59A questionnaire response for question 7 (true=Yes, false=No, null=unanswered).
    /// </summary>
    public bool? GW59AQuestion7Response { get; set; }

    /// <summary>
    /// Date monitoring results were due (GW-59A question 1).
    /// </summary>
    public DateTime? GW59ADueDate { get; set; }

    /// <summary>
    /// Details for GW-59A question 2.
    /// </summary>
    public string GW59AQuestion2Details { get; set; } = string.Empty;

    /// <summary>
    /// Details for GW-59A question 4.
    /// </summary>
    public string GW59AQuestion4Details { get; set; } = string.Empty;

    /// <summary>
    /// Details for GW-59A question 5.
    /// </summary>
    public string GW59AQuestion5Details { get; set; } = string.Empty;

    /// <summary>
    /// Details for GW-59A question 7.
    /// </summary>
    public string GW59AQuestion7Details { get; set; } = string.Empty;

    /// <summary>
    /// GW-59A section 8 signer name.
    /// </summary>
    public string GW59ASignerName { get; set; } = string.Empty;

    /// <summary>
    /// GW-59A section 8 signer title.
    /// </summary>
    public string GW59ASignerTitle { get; set; } = string.Empty;

    /// <summary>
    /// GW-59A section 8 signed date.
    /// </summary>
    public DateTime? GW59ASignedDate { get; set; }

    // Navigation properties
    public Facility? Facility { get; set; }
    public MonitoringWell? MonitoringWell { get; set; }
    public ICollection<GWMonitTemplateValue> TemplateValues { get; set; } = new List<GWMonitTemplateValue>();
}

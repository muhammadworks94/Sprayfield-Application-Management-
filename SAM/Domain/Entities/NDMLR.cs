using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores annual NDMLR report metadata and identity.
/// </summary>
public class NDMLR : CompanyScopedEntity
{
    /// <summary>
    /// Facility for this annual NDMLR record.
    /// </summary>
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Annual reporting year (calendar year).
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Optional notes for source or generation context.
    /// </summary>
    public string? SourceNotes { get; set; }

    public Facility? Facility { get; set; }
}

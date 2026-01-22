namespace SAM.Domain.Entities.Base;

/// <summary>
/// Base entity class providing audit fields for all entities.
/// </summary>
public abstract class AuditableEntity
{
    /// <summary>
    /// Unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the record was created (UTC).
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the record was last updated (UTC).
    /// </summary>
    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Email of the user who created the record.
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the record is soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;
}


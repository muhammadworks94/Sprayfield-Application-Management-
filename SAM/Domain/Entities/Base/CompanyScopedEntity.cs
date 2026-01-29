namespace SAM.Domain.Entities.Base;

/// <summary>
/// Base entity class for entities that are scoped to a company.
/// Provides CompanyId and navigation property.
/// </summary>
public abstract class CompanyScopedEntity : AuditableEntity
{
    /// <summary>
    /// The ID of the parent company.
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// Navigation property to the parent company.
    /// </summary>
    public Company? Company { get; set; }
}



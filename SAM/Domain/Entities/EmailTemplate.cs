using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Editable email template used by system notifications.
/// </summary>
public class EmailTemplate : AuditableEntity
{
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public bool IsSystemTemplate { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

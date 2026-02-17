namespace SAM.Services.Models;

/// <summary>
/// Input model for updating an editable email template.
/// </summary>
public class EmailTemplateUpdateModel
{
    public string TemplateKey { get; set; } = string.Empty;
    public string SubjectTemplate { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
}

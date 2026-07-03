using SAM.Domain.Enums;

namespace SAM.Services.Models;

public class EmailLogCreateModel
{
    public DateTime SentAtUtc { get; set; } = DateTime.UtcNow;
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
    public string? TemplateDisplayName { get; set; }
    public EmailLogStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HtmlBody { get; set; }
    public string? InitiatedByUserId { get; set; }
    public string? InitiatedByEmail { get; set; }
    public string? InitiatedByDisplayName { get; set; }
}

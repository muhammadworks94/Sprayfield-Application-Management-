using SAM.Domain.Enums;

namespace SAM.ViewModels.EmailLogs;

public class EmailLogItemViewModel
{
    public Guid Id { get; set; }
    public DateTime SentAtUtc { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string? TemplateKey { get; set; }
    public string? TemplateDisplayName { get; set; }
    public EmailLogStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public string? HtmlBody { get; set; }
    public string? InitiatedByDisplayName { get; set; }
    public string? InitiatedByEmail { get; set; }
}

using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Persistent record of an outbound email send attempt.
/// </summary>
public class EmailLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
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

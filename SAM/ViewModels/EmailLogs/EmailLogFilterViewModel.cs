using SAM.Domain.Enums;

namespace SAM.ViewModels.EmailLogs;

public class EmailLogFilterViewModel
{
    public string? TemplateKey { get; set; }
    public EmailLogStatus? Status { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

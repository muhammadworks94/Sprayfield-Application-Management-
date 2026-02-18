namespace SAM.ViewModels.ErrorLogs;

public class ErrorLogItemViewModel
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Module { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? RequestId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorEmail { get; set; }
    public Guid? CompanyId { get; set; }
    public string CompanyName { get; set; } = "N/A";
}


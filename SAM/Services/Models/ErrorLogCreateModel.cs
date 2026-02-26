namespace SAM.Services.Models;

public class ErrorLogCreateModel
{
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string Module { get; set; } = string.Empty;
    public string? Path { get; set; }
    public string? HttpMethod { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ActorUserId { get; set; }
    public string? ActorEmail { get; set; }
    public string? ActorDisplayName { get; set; }
    public Guid? CompanyId { get; set; }
    public string? IpAddress { get; set; }
    public string? QueryString { get; set; }
    public string? InnerExceptionType { get; set; }
    public string? InnerExceptionMessage { get; set; }
}


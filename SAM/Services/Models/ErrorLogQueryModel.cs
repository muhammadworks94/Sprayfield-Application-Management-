namespace SAM.Services.Models;

public class ErrorLogQueryModel
{
    public Guid? CompanyId { get; set; }
    public string? ActorUserId { get; set; }
    public string? Module { get; set; }
    public string? ExceptionType { get; set; }
    public int? StatusCode { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}


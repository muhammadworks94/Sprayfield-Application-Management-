using SAM.Domain.Enums;

namespace SAM.Services.Models;

public class UserActivityLogQueryModel
{
    public Guid? CompanyId { get; set; }
    public string? ActorUserId { get; set; }
    public UserActivityType? ActivityType { get; set; }
    public string? Module { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}


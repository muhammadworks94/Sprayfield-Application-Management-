using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Immutable activity log event for admin audit visibility.
/// </summary>
public class UserActivityLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public UserActivityType ActivityType { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? ActorUserId { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string? ActorDisplayName { get; set; }
    public string? HttpMethod { get; set; }
    public string? Path { get; set; }
    public string? IpAddress { get; set; }
    public string? ChangedFields { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}


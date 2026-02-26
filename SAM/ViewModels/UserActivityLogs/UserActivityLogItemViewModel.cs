using SAM.Domain.Enums;

namespace SAM.ViewModels.UserActivityLogs;

public class UserActivityLogItemViewModel
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public UserActivityType ActivityType { get; set; }
    public string Module { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string ActorEmail { get; set; } = string.Empty;
    public string? ActorDisplayName { get; set; }
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<string> ChangedFields { get; set; } = Array.Empty<string>();
}


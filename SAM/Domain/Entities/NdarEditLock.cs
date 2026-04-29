using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Row-level lock for NDAR day editing to prevent concurrent edits on the same facility/date.
/// </summary>
public class NdarEditLock : CompanyScopedEntity
{
    public Guid FacilityId { get; set; }
    public DateTime EditDate { get; set; }
    public string LockedByUserId { get; set; } = string.Empty;
    public string LockedByDisplayName { get; set; } = string.Empty;
    public Guid LockToken { get; set; }
    public DateTime LockedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? ReleasedAtUtc { get; set; }

    public Facility? Facility { get; set; }
    public ApplicationUser? LockedByUser { get; set; }
}


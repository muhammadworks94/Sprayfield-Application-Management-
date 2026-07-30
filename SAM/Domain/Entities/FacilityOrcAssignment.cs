using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Historical ORC (Operator in Responsible Charge) assignment for a facility,
/// optionally linked to a SAM user, with reporting tenure start/end dates.
/// </summary>
public class FacilityOrcAssignment : AuditableEntity
{
    public Guid FacilityId { get; set; }

    /// <summary>
    /// Optional SAM user assigned as ORC. Null when backfilled from free-text only.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Inclusive start date of this ORC tenure (date only).
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Inclusive end date of this ORC tenure. Null means the current open assignment.
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Snapshot of the ORC display name at assignment time.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? OperatorNumber { get; set; }

    public string? OperatorGrade { get; set; }

    public string? OperatorPhone { get; set; }

    public Facility? Facility { get; set; }

    public ApplicationUser? User { get; set; }
}

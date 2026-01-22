using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Records requests from global admins for creating or disabling other global admin accounts.
/// </summary>
public class AdminRequest : AuditableEntity
{
    /// <summary>
    /// The type of administrative action.
    /// </summary>
    public AdminRequestTypeEnum RequestType { get; set; }

    /// <summary>
    /// The email of the user to be created or disabled.
    /// </summary>
    public string TargetEmail { get; set; } = string.Empty;

    /// <summary>
    /// The full name of the user for new admin requests.
    /// </summary>
    public string TargetFullName { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the request.
    /// </summary>
    public string Justification { get; set; } = string.Empty;

    /// <summary>
    /// Email of the admin who submitted the request.
    /// </summary>
    public string RequestedByEmail { get; set; } = string.Empty;

    /// <summary>
    /// The status of the request.
    /// </summary>
    public RequestStatusEnum Status { get; set; } = RequestStatusEnum.Pending;
}


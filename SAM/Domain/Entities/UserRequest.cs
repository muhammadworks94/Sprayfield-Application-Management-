using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

/// <summary>
/// Records requests from company admins to add new users to their company, with specific roles.
/// </summary>
public class UserRequest : AuditableEntity
{
    /// <summary>
    /// The ID of the company this user request is for.
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// The name of the company for easy reference.
    /// </summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>
    /// Full name of the requested user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email of the requested user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The requested application role for the user.
    /// </summary>
    public AppRoleEnum AppRole { get; set; }

    /// <summary>
    /// Email of the user who submitted the request.
    /// </summary>
    public string RequestedByEmail { get; set; } = string.Empty;

    /// <summary>
    /// The status of the user request.
    /// </summary>
    public RequestStatusEnum Status { get; set; } = RequestStatusEnum.Pending;

    // Navigation properties
    public Company? Company { get; set; }
}



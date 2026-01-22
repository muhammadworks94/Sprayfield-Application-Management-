using Microsoft.AspNetCore.Identity;

namespace SAM.Domain.Entities;

/// <summary>
/// Extended Identity user with company association and additional properties.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// The ID of the company this user belongs to.
    /// Null for global administrators.
    /// </summary>
    public Guid? CompanyId { get; set; }

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the user account is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the company.
    /// </summary>
    public Company? Company { get; set; }
}


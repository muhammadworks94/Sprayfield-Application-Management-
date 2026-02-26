using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.UserManagement;

/// <summary>
/// ViewModel for the User Management Index page (users list).
/// </summary>
public class UserManagementIndexViewModel
{
    /// <summary>
    /// List of users to display in the Users tab.
    /// </summary>
    public List<UserViewModel> Users { get; set; } = new();

    /// <summary>
    /// Whether the current user is a global admin.
    /// </summary>
    public bool IsGlobalAdmin { get; set; }

    /// <summary>
    /// Whether the current user can manage users (admins and company admins).
    /// Controls visibility of the Users tab and user actions.
    /// </summary>
    public bool CanManageUsers { get; set; }

    /// <summary>
    /// Selected company ID for filtering (if applicable).
    /// </summary>
    public Guid? SelectedCompanyId { get; set; }

    /// <summary>
    /// SelectList of companies for dropdown filters.
    /// </summary>
    public SelectList? Companies { get; set; }

    /// <summary>
    /// Filter configuration for the page.
    /// </summary>
    public FilterViewModel? FilterViewModel { get; set; }
}


using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.ViewModels.Common;

namespace SAM.ViewModels.UserManagement;

/// <summary>
/// ViewModel for the User Management Index page with tabbed interface.
/// </summary>
public class UserManagementIndexViewModel
{
    /// <summary>
    /// List of users to display in the Users tab.
    /// </summary>
    public List<UserViewModel> Users { get; set; } = new();

    /// <summary>
    /// List of user requests to display in the User Requests tab.
    /// </summary>
    public List<UserRequestViewModel> UserRequests { get; set; } = new();

    /// <summary>
    /// Currently active tab ("users" or "requests").
    /// </summary>
    public string ActiveTab { get; set; } = "requests";

    /// <summary>
    /// Whether the current user is a global admin.
    /// </summary>
    public bool IsGlobalAdmin { get; set; }

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


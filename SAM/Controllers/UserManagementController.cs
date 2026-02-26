using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Controllers.Base;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.Services.Interfaces;
using SAM.ViewModels.Common;
using SAM.ViewModels.UserManagement;

namespace SAM.Controllers;

/// <summary>
/// Controller for User Management module - handling user and admin requests.
/// </summary>
[Authorize]
public partial class UserManagementController : BaseController
{
    private readonly IUserRequestService _userRequestService;
    private readonly ICompanyService _companyService;
    private readonly ILookupQueryService _lookupQueryService;
    private readonly IUserService _userService;

    public UserManagementController(
        IUserRequestService userRequestService,
        ICompanyService companyService,
        ILookupQueryService lookupQueryService,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserManagementController> logger)
        : base(userManager, logger)
    {
        _userRequestService = userRequestService;
        _companyService = companyService;
        _lookupQueryService = lookupQueryService;
        _userService = userService;
    }

    #region Index (Combined View)

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> Index()
    {
        Guid? companyId = null;
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var isCompanyAdminOnly = !isGlobalAdmin && await IsInRoleAsync("company_admin");
        var canManageUsers = isGlobalAdmin || isCompanyAdminOnly;
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Use effective company ID if no companyId specified (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        // Load users data for roles that can manage users
        var userViewModels = new List<UserViewModel>();
        if (canManageUsers)
        {
            IEnumerable<ApplicationUser> users;

            if (isGlobalAdmin)
            {
                if (companyId.HasValue)
                {
                    users = await _userService.GetUsersByCompanyAsync(companyId.Value);
                }
                else
                {
                    users = await _userService.GetAllUsersAsync();
                }
            }
            else // company admin only
            {
                if (!companyId.HasValue)
                {
                    companyId = effectiveCompanyId;
                }

                users = companyId.HasValue
                    ? await _userService.GetUsersByCompanyAsync(companyId.Value)
                    : Enumerable.Empty<ApplicationUser>();
            }

            userViewModels = await MapUsersWithRolesAsync(users);
        }

        var viewModel = new UserManagementIndexViewModel
        {
            Users = userViewModels,
            IsGlobalAdmin = isGlobalAdmin,
            CanManageUsers = canManageUsers,
            SelectedCompanyId = companyId,
            Companies = await GetCompanySelectListAsync(),
            FilterViewModel = null
        };

        return View(viewModel);
    }

    #endregion

    #region Helper Methods

    private async Task<SelectList> GetCompanySelectListAsync()
    {
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();
        var companies = await _lookupQueryService.GetCompaniesAsync(effectiveCompanyId);

        return new SelectList(companies, "Id", "Name");
    }

    private SelectList GetAppRoleSelectList(bool includeAdmin = false, bool includeDefaultOption = false)
    {
        // Exclude admin role by default (for company admin requests)
        // Include admin role when managing users (admin-only page)
        var roles = Enum.GetValues(typeof(AppRoleEnum))
            .Cast<AppRoleEnum>()
            .Where(r => includeAdmin || r != AppRoleEnum.admin)
            .Select(e => new SelectListItem
            {
                Value = e.ToString(),
                Text = e.ToString().Replace("_", " ").Replace("operator", "Operator")
            })
            .ToList();

        // Add default "Select Role" option if requested
        if (includeDefaultOption)
        {
            roles.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "Select Role"
            });
        }

        return new SelectList(roles, "Value", "Text");
    }

    private async Task<List<UserViewModel>> MapUsersWithRolesAsync(IEnumerable<ApplicationUser> users)
    {
        var viewModels = new List<UserViewModel>();
        foreach (var user in users)
        {
            var roles = await UserManager.GetRolesAsync(user);
            viewModels.Add(new UserViewModel
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                CompanyId = user.CompanyId,
                CompanyName = user.Company?.Name,
                Roles = roles.ToList(),
                IsActive = user.IsActive
            });
        }

        return viewModels;
    }

    #endregion
}

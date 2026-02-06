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
public class UserManagementController : BaseController
{
    private readonly IUserRequestService _userRequestService;
    private readonly ICompanyService _companyService;
    private readonly IUserService _userService;

    public UserManagementController(
        IUserRequestService userRequestService,
        ICompanyService companyService,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserManagementController> logger)
        : base(userManager, logger)
    {
        _userRequestService = userRequestService;
        _companyService = companyService;
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

            foreach (var user in users)
            {
                var roles = await UserManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
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

    #region User Requests

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequests(Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
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

        var requests = await _userRequestService.GetAllAsync(companyId);
        
        var viewModels = requests.Select(r => new UserRequestViewModel
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            CompanyName = r.CompanyName,
            FullName = r.FullName,
            Email = r.Email,
            AppRole = r.AppRole,
            RequestedByEmail = r.RequestedByEmail,
            Status = r.Status,
            CreatedDate = r.CreatedDate
        });

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "UserRequests",
            EnableSearch = false,
            Fields = new List<FilterField>()
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestApprove(Guid id)
    {
        var request = await _userRequestService.GetByIdAsync(id);
        if (request == null)
            return NotFound();

        var viewModel = new UserRequestApproveViewModel
        {
            Id = request.Id,
            FullName = request.FullName,
            Email = request.Email,
            CompanyName = request.CompanyName,
            AppRole = request.AppRole,
            RequestedRole = request.AppRole,
            RequestedByEmail = request.RequestedByEmail
        };

        ViewBag.Roles = GetAppRoleSelectList();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserRequestApprove(UserRequestApproveViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            // Reload request to get original requested role
            var request = await _userRequestService.GetByIdAsync(viewModel.Id);
            if (request != null)
            {
                viewModel.RequestedRole = request.AppRole;
            }
            ViewBag.Roles = GetAppRoleSelectList();
            return View(viewModel);
        }

        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.ApproveRequestAsync(viewModel.Id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", viewModel.AppRole);
            TempData["SuccessMessage"] = $"User request approved. {viewModel.AppRole} account created for {viewModel.FullName}.";
            return RedirectToAction(nameof(Index), new { tab = "requests" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            ViewBag.Roles = GetAppRoleSelectList();
            return RedirectToAction(nameof(UserRequestApprove), new { id = viewModel.Id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestReject(Guid id, string? reason = null)
    {
        try
        {
            var currentUser = await GetCurrentUserAsync();
            await _userRequestService.RejectRequestAsync(id, currentUser?.Email ?? CurrentUserEmail ?? "unknown", reason);
            TempData["SuccessMessage"] = "User request rejected.";
            return RedirectToAction(nameof(Index), new { tab = "requests" });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(UserRequests));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> UserRequestDelete(Guid id)
    {
        try
        {
            await _userRequestService.SoftDeleteAsync(id);
            TempData["SuccessMessage"] = "User request deleted.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "User request not found.";
            return RedirectToAction(nameof(UserRequests));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred while deleting the user request: {ex.Message}";
            return RedirectToAction(nameof(UserRequests));
        }
    }

    #endregion


    #region Users

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> Users(Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
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

        // Get users based on company filter
        IEnumerable<ApplicationUser> users;
        if (companyId.HasValue)
        {
            users = await _userService.GetUsersByCompanyAsync(companyId.Value);
        }
        else if (isGlobalAdmin)
        {
            users = await _userService.GetAllUsersAsync();
        }
        else
        {
            users = await _userService.GetUsersByCompanyAsync(effectiveCompanyId);
        }

        // Map to view models with roles
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

        // Create filter view model
        var filterViewModel = new FilterViewModel
        {
            PageName = "Users",
            EnableSearch = false,
            Fields = new List<FilterField>()
        };

        if (isGlobalAdmin)
        {
            var companies = await GetCompanySelectListAsync();
            filterViewModel.Fields.Add(new FilterField
            {
                Name = "companyId",
                Label = "Company",
                Type = FilterFieldType.Dropdown,
                Options = companies,
                Value = companyId,
                ColumnClass = "col-md-4",
                IconClass = "bi bi-building"
            });
        }

        ViewBag.IsGlobalAdmin = isGlobalAdmin;
        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.SelectedCompanyId = companyId;
        ViewBag.FilterViewModel = filterViewModel;

        return View(viewModels);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserDetails(string id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);
        var viewModel = new UserDetailsViewModel
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            CompanyId = user.CompanyId,
            CompanyName = user.Company?.Name,
            Roles = roles.ToList(),
            IsActive = user.IsActive,
            UserName = user.UserName ?? string.Empty,
            EmailConfirmed = user.EmailConfirmed,
            PhoneNumber = user.PhoneNumber,
            CreatedDate = null, // ApplicationUser doesn't have CreatedDate (it's IdentityUser)
            UpdatedDate = null // ApplicationUser doesn't have UpdatedDate (it's IdentityUser)
        };

        return View(viewModel);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserCreate(Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        // Set company ID if not provided (respects session selection for admins)
        if (!companyId.HasValue && effectiveCompanyId.HasValue)
        {
            companyId = effectiveCompanyId.Value;
        }

        if (companyId.HasValue)
        {
            await EnsureCompanyAccessAsync(companyId.Value);
        }

        var viewModel = new UserCreateViewModel
        {
            CompanyId = companyId
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserCreate(UserCreateViewModel viewModel)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();

        if (viewModel.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(viewModel.CompanyId.Value);
        }

        // Prevent company admins from assigning admin role
        if (!isGlobalAdmin && viewModel.AppRole == AppRoleEnum.admin)
        {
            ModelState.AddModelError("AppRole", "Company admins cannot assign the admin role.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin);
            return View(viewModel);
        }

        try
        {
            var user = await _userService.CreateUserAsync(
                viewModel.Email,
                viewModel.FullName,
                viewModel.CompanyId,
                viewModel.AppRole,
                generatePassword: true);

            TempData["SuccessMessage"] = $"User '{viewModel.FullName}' created successfully. A temporary password has been generated.";
            return RedirectToAction(nameof(UserDetails), new { id = user.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin);
            return View(viewModel);
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserEdit(string id)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound();

        if (user.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);
        AppRoleEnum? currentRole = null; // Default to null if no role
        if (roles.Any())
        {
            var roleName = roles.First();
            // Exclude admin role - only get company roles
            if (roleName != "admin")
            {
                currentRole = roleName switch
                {
                    "company_admin" => AppRoleEnum.company_admin,
                    "operator" => AppRoleEnum.@operator,
                    "technician" => AppRoleEnum.technician,
                    _ => null
                };
            }
        }

        var viewModel = new UserEditViewModel
        {
            Id = user.Id,
            CompanyId = user.CompanyId,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            AppRole = currentRole,
            IsActive = user.IsActive
        };

        ViewBag.Companies = await GetCompanySelectListAsync();
        ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin, includeDefaultOption: true);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserEdit(UserEditViewModel viewModel)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();

        if (viewModel.CompanyId.HasValue)
        {
            await EnsureCompanyAccessAsync(viewModel.CompanyId.Value);
        }

        // Validate that a role is selected
        if (!viewModel.AppRole.HasValue)
        {
            ModelState.AddModelError("AppRole", "Please select a role.");
        }

        // Prevent company admins from assigning admin role (should not be possible, but double-check)
        if (viewModel.AppRole.HasValue && !isGlobalAdmin && viewModel.AppRole.Value == AppRoleEnum.admin)
        {
            ModelState.AddModelError("AppRole", "Company admins cannot assign the admin role.");
        }

        // Prevent company admins from changing a user's role to admin if they already have it
        var existingUser = await _userService.GetUserByIdAsync(viewModel.Id);
        if (existingUser != null && !isGlobalAdmin)
        {
            var existingRoles = await UserManager.GetRolesAsync(existingUser);
            if (existingRoles.Contains("admin"))
            {
                ModelState.AddModelError("AppRole", "Company admins cannot modify users with the admin role.");
            }
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin, includeDefaultOption: true);
            return View(viewModel);
        }

        try
        {
            // Ensure AppRole has a value before calling UpdateUserAsync
            var roleToUpdate = viewModel.AppRole ?? AppRoleEnum.@operator; // Default to operator if null
            var user = await _userService.UpdateUserAsync(
                viewModel.Id,
                viewModel.FullName,
                viewModel.Email,
                viewModel.CompanyId,
                roleToUpdate,
                viewModel.IsActive);

            TempData["SuccessMessage"] = $"User '{viewModel.FullName}' updated successfully.";
            return RedirectToAction(nameof(UserDetails), new { id = user.Id });
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin);
            return View(viewModel);
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException ex)
        {
            ModelState.AddModelError("", ex.Message);
            ViewBag.Companies = await GetCompanySelectListAsync();
            ViewBag.Roles = GetAppRoleSelectList(includeAdmin: isGlobalAdmin);
            return View(viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UserDelete(string id)
    {
        ApplicationUser? user = null;

        try
        {
            user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            if (user.CompanyId.HasValue)
            {
                await EnsureCompanyAccessAsync(user.CompanyId.Value);
            }

            await _userService.DeleteUserAsync(id);
            TempData["SuccessMessage"] = $"User '{user.FullName}' deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException)
        {
            TempData["ErrorMessage"] = "User not found.";
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    #endregion

    #region Admin Management

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> ManageAdmins()
    {
        // Get all users with admin role
        var allUsers = await _userService.GetAllUsersAsync();
        var adminUsers = new List<ApplicationUser>();

        foreach (var user in allUsers)
        {
            var roles = await UserManager.GetRolesAsync(user);
            if (roles.Contains("admin"))
            {
                adminUsers.Add(user);
            }
        }

        var viewModels = adminUsers.Select(u => new AdminViewModel
        {
            Id = u.Id,
            Email = u.Email ?? string.Empty,
            FullName = u.FullName,
            IsActive = u.IsActive,
            CompanyName = u.Company?.Name
        }).ToList();

        return View(viewModels);
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public IActionResult AdminCreate()
    {
        var viewModel = new AdminCreateViewModel();
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminCreate(AdminCreateViewModel viewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(viewModel);
        }

        try
        {
            // Create admin user with no company (global admin)
            var user = await _userService.CreateUserAsync(
                viewModel.Email,
                viewModel.FullName,
                companyId: null, // Global admins don't have a company
                AppRoleEnum.admin,
                generatePassword: true);

            TempData["SuccessMessage"] = $"Admin '{viewModel.FullName}' created successfully. A temporary password has been generated.";
            return RedirectToAction(nameof(ManageAdmins));
        }
        catch (Infrastructure.Exceptions.BusinessRuleException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
        catch (Infrastructure.Exceptions.EntityNotFoundException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(viewModel);
        }
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> GetNonAdminUsers()
    {
        var allUsers = await _userService.GetAllUsersAsync();
        var nonAdminUsers = new List<object>();

        foreach (var user in allUsers)
        {
            var roles = await UserManager.GetRolesAsync(user);
            if (!roles.Contains("admin"))
            {
                nonAdminUsers.Add(new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email
                });
            }
        }

        return Json(nonAdminUsers);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminPromote(string userId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            // Check if user already has admin role
            var roles = await UserManager.GetRolesAsync(user);
            if (roles.Contains("admin"))
            {
                TempData["ErrorMessage"] = $"User '{user.FullName}' is already an administrator.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            // Remove company-related roles (company_admin, operator, technician)
            var companyRoles = new[] { "company_admin", "operator", "technician" };
            foreach (var role in companyRoles)
            {
                if (roles.Contains(role))
                {
                    var removeResult = await UserManager.RemoveFromRoleAsync(user, role);
                    if (!removeResult.Succeeded)
                    {
                        var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                        TempData["ErrorMessage"] = $"Failed to remove {role} role: {errors}";
                        return RedirectToAction(nameof(ManageAdmins));
                    }
                }
            }

            // Remove user from company (set CompanyId to null for global admins)
            if (user.CompanyId.HasValue)
            {
                user.CompanyId = null;
                var updateResult = await UserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = $"Failed to remove user from company: {errors}";
                    return RedirectToAction(nameof(ManageAdmins));
                }
            }

            // Add admin role
            var result = await UserManager.AddToRoleAsync(user, "admin");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Failed to promote user: {errors}";
                return RedirectToAction(nameof(ManageAdmins));
            }

            TempData["SuccessMessage"] = $"User '{user.FullName}' has been promoted to administrator and removed from their company.";
            return RedirectToAction(nameof(ManageAdmins));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            return RedirectToAction(nameof(ManageAdmins));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public async Task<IActionResult> AdminRemove(string adminId)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(adminId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            // Check if this is the current user
            var currentUser = await GetCurrentUserAsync();
            if (currentUser != null && currentUser.Id == adminId)
            {
                TempData["ErrorMessage"] = "You cannot remove your own administrator role.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            // Count total admins
            var allUsers = await _userService.GetAllUsersAsync();
            var adminCount = 0;
            foreach (var u in allUsers)
            {
                var roles = await UserManager.GetRolesAsync(u);
                if (roles.Contains("admin"))
                {
                    adminCount++;
                }
            }

            // Ensure at least one admin remains
            if (adminCount <= 1)
            {
                TempData["ErrorMessage"] = "Cannot remove the last administrator. At least one administrator must remain.";
                return RedirectToAction(nameof(ManageAdmins));
            }

            // Remove admin role
            var result = await UserManager.RemoveFromRoleAsync(user, "admin");
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = $"Failed to remove admin role: {errors}";
                return RedirectToAction(nameof(ManageAdmins));
            }

            TempData["SuccessMessage"] = $"Administrator role has been removed from '{user.FullName}'.";
            return RedirectToAction(nameof(ManageAdmins));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            return RedirectToAction(nameof(ManageAdmins));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> CompanyAdminAssign(string userId)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (!user.CompanyId.HasValue)
        {
            return BadRequest("User must be assigned to a company to be a company admin.");
        }

        if (!isGlobalAdmin)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);

        // Do not allow modifying system admins via this endpoint
        if (roles.Contains("admin"))
        {
            return BadRequest("Cannot modify roles for system administrators via this action.");
        }

        // Remove other company-level roles so user ends up only with company_admin at company level
        var companyRoles = new[] { "company_admin", "operator", "technician" };
        foreach (var role in companyRoles)
        {
            if (roles.Contains(role))
            {
                var removeResult = await UserManager.RemoveFromRoleAsync(user, role);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return StatusCode(500, $"Failed to remove {role} role: {errors}");
                }
            }
        }

        var addResult = await UserManager.AddToRoleAsync(user, "company_admin");
        if (!addResult.Succeeded)
        {
            var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
            return StatusCode(500, $"Failed to assign company_admin role: {errors}");
        }

        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> CompanyAdminRemove(string userId)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        if (user.CompanyId.HasValue && !isGlobalAdmin)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);

        // Do not allow modifying system admins via this endpoint
        if (roles.Contains("admin"))
        {
            return BadRequest("Cannot modify roles for system administrators via this action.");
        }

        if (roles.Contains("company_admin"))
        {
            var removeResult = await UserManager.RemoveFromRoleAsync(user, "company_admin");
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                return StatusCode(500, $"Failed to remove company_admin role: {errors}");
            }
        }

        return Ok();
    }

    #endregion

    #region Helper Methods

    private async Task<SelectList> GetCompanySelectListAsync()
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();
        var effectiveCompanyId = await GetEffectiveCompanyIdAsync();

        var companies = await _companyService.GetAllAsync();
        
        // Filter by effective company ID if session has a selection (for admins) or user has a company
        if (effectiveCompanyId.HasValue)
        {
            companies = companies.Where(c => c.Id == effectiveCompanyId.Value);
        }

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


    #endregion
}



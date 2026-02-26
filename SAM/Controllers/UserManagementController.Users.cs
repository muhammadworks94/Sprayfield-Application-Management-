using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.Common;
using SAM.ViewModels.UserManagement;

namespace SAM.Controllers;

public partial class UserManagementController
{
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
        var viewModels = await MapUsersWithRolesAsync(users);

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

}

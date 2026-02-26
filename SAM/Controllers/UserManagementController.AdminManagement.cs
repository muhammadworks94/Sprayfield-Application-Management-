using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;
using SAM.ViewModels.UserManagement;

namespace SAM.Controllers;

public partial class UserManagementController
{
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
    public async Task<IActionResult> CompanyAdminAssign(string userId, Guid? companyId = null)
    {
        var isGlobalAdmin = await IsGlobalAdminAsync();

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound("User not found.");
        }

        // If a companyId is supplied, assign the user to that company first
        if (companyId.HasValue)
        {
            // Validate target company exists
            var company = await _companyService.GetByIdAsync(companyId.Value);
            if (company == null)
            {
                return NotFound("Company not found.");
            }

            // Non-global admins can only assign within accessible company
            if (!isGlobalAdmin)
            {
                await EnsureCompanyAccessAsync(companyId.Value);
            }

            user.CompanyId = companyId.Value;
            var updateResult = await UserManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                return StatusCode(500, $"Failed to assign company to user: {errors}");
            }
        }

        // After optional assignment, ensure user has a company
        if (!user.CompanyId.HasValue)
        {
            return BadRequest("User must be assigned to a company to be a company admin.");
        }

        // Permission check for non-global admins based on the user's company
        if (!isGlobalAdmin)
        {
            await EnsureCompanyAccessAsync(user.CompanyId.Value);
        }

        var roles = await UserManager.GetRolesAsync(user);

        // Remove other company-level roles so user ends up only with company_admin at company level
        var companyRoles = new[] { "company_admin", "operator", "technician", "admin" };
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


}

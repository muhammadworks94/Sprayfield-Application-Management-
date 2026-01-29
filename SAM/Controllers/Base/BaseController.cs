using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SAM.Domain.Entities;

namespace SAM.Controllers.Base;

/// <summary>
/// Base controller providing common functionality for all controllers.
/// </summary>
[Authorize]
public abstract class BaseController : Controller
{
    protected readonly UserManager<ApplicationUser> UserManager;
    protected readonly ILogger Logger;

    protected BaseController(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        UserManager = userManager;
        Logger = logger;
    }

    /// <summary>
    /// Gets the current user's ID.
    /// </summary>
    protected string? CurrentUserId => UserManager.GetUserId(User);

    /// <summary>
    /// Gets the current user's email.
    /// </summary>
    protected string? CurrentUserEmail => User.Identity?.Name;

    /// <summary>
    /// Gets the current user's ApplicationUser entity.
    /// </summary>
    protected async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        if (CurrentUserId == null)
            return null;

        return await UserManager.GetUserAsync(User);
    }

    /// <summary>
    /// Gets the current user's company ID.
    /// </summary>
    protected async Task<Guid?> GetCurrentCompanyIdAsync()
    {
        var user = await GetCurrentUserAsync();
        return user?.CompanyId;
    }

    /// <summary>
    /// Checks if the current user is a global administrator.
    /// </summary>
    protected async Task<bool> IsGlobalAdminAsync()
    {
        if (CurrentUserId == null)
            return false;

        var user = await GetCurrentUserAsync();
        return user != null && await UserManager.IsInRoleAsync(user, "admin");
    }

    /// <summary>
    /// Checks if the current user has the specified role.
    /// </summary>
    protected async Task<bool> IsInRoleAsync(string role)
    {
        if (CurrentUserId == null)
            return false;

        var user = await GetCurrentUserAsync();
        return user != null && await UserManager.IsInRoleAsync(user, role);
    }

    /// <summary>
    /// Checks if the current user has access to the specified company.
    /// Global admins have access to all companies.
    /// </summary>
    protected async Task<bool> HasCompanyAccessAsync(Guid companyId)
    {
        if (await IsGlobalAdminAsync())
            return true;

        var userCompanyId = await GetCurrentCompanyIdAsync();
        return userCompanyId.HasValue && userCompanyId.Value == companyId;
    }

    /// <summary>
    /// Ensures the current user has access to the specified company.
    /// Throws UnauthorizedAccessException if access is denied.
    /// </summary>
    protected async Task EnsureCompanyAccessAsync(Guid companyId)
    {
        if (!await HasCompanyAccessAsync(companyId))
        {
            throw new Infrastructure.Exceptions.UnauthorizedResourceAccessException(
                $"User does not have access to company '{companyId}'.");
        }
    }

    /// <summary>
    /// Gets the effective company ID for the current user.
    /// Returns the user's CompanyId, or null if they are a global admin.
    /// </summary>
    protected async Task<Guid?> GetEffectiveCompanyIdAsync()
    {
        if (await IsGlobalAdminAsync())
            return null; // Global admins can see all companies

        return await GetCurrentCompanyIdAsync();
    }
}


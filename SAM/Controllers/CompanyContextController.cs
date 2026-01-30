using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAM.Controllers.Base;
using SAM.Infrastructure.Authorization;

namespace SAM.Controllers;

/// <summary>
/// Controller for managing company context selection for admins.
/// </summary>
[Authorize]
public class CompanyContextController : BaseController
{
    private const string SelectedCompanyIdSessionKey = "SelectedCompanyId";

    public CompanyContextController(
        Microsoft.AspNetCore.Identity.UserManager<Domain.Entities.ApplicationUser> userManager,
        ILogger<CompanyContextController> logger)
        : base(userManager, logger)
    {
    }

    /// <summary>
    /// Sets the company context for the current admin user.
    /// </summary>
    /// <param name="companyId">Company ID to filter by, or null for "All Companies"</param>
    /// <param name="returnUrl">URL to redirect to after setting context</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireAdmin)]
    public IActionResult SetCompanyContext(Guid? companyId, string? returnUrl = null)
    {
        if (companyId.HasValue)
        {
            HttpContext.Session.SetString(SelectedCompanyIdSessionKey, companyId.Value.ToString());
        }
        else
        {
            HttpContext.Session.Remove(SelectedCompanyIdSessionKey);
        }

        // Redirect to return URL or dashboard
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    /// <summary>
    /// Gets the selected company ID from session.
    /// </summary>
    public static Guid? GetSelectedCompanyIdFromSession(ISession session)
    {
        var companyIdString = session.GetString(SelectedCompanyIdSessionKey);
        if (string.IsNullOrEmpty(companyIdString))
            return null;

        if (Guid.TryParse(companyIdString, out var companyId))
            return companyId;

        return null;
    }
}


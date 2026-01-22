using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SAM.Domain.Entities;

namespace SAM.Infrastructure.Authorization;

/// <summary>
/// Authorization handler for company-scoped access.
/// Ensures users can only access data from their own company (unless they are global admins).
/// </summary>
public class CompanyAccessHandler : AuthorizationHandler<CompanyAccessRequirement, Guid?>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public CompanyAccessHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CompanyAccessRequirement requirement,
        Guid? companyId)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }

        var user = await _userManager.GetUserAsync(context.User);
        
        if (user == null)
        {
            context.Fail();
            return;
        }

        // Global admins can access all companies
        if (await _userManager.IsInRoleAsync(user, "admin"))
        {
            context.Succeed(requirement);
            return;
        }

        // If no company ID is specified, allow access (for listing operations)
        if (!companyId.HasValue)
        {
            context.Succeed(requirement);
            return;
        }

        // Company-scoped users can only access their own company
        if (user.CompanyId.HasValue && user.CompanyId.Value == companyId.Value)
        {
            context.Succeed(requirement);
            return;
        }

        // User doesn't have access to this company
        context.Fail();
    }
}

/// <summary>
/// Authorization requirement for company-scoped access.
/// </summary>
public class CompanyAccessRequirement : IAuthorizationRequirement
{
}


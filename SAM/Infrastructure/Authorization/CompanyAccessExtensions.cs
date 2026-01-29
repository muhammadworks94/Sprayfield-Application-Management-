using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SAM.Infrastructure.Authorization;

/// <summary>
/// Extension methods for company access authorization.
/// </summary>
public static class CompanyAccessExtensions
{
    /// <summary>
    /// Authorizes access to a specific company.
    /// Use this method in controllers to check company access before performing operations.
    /// </summary>
    public static async Task<AuthorizationResult> AuthorizeCompanyAccessAsync(
        this IAuthorizationService authorizationService,
        Microsoft.AspNetCore.Http.HttpContext httpContext,
        Guid? companyId)
    {
        return await authorizationService.AuthorizeAsync(
            httpContext.User,
            companyId,
            Policies.RequireCompanyAccess);
    }
}



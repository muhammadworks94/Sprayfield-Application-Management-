using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace SAM.Data.Seeders;

/// <summary>
/// Seeds application roles.
/// </summary>
public static class RoleSeeder
{
    /// <summary>
    /// Seeds all required roles if they don't exist.
    /// </summary>
    public static async Task SeedRolesAsync(
        RoleManager<IdentityRole> roleManager,
        ILogger logger)
    {
        var roles = new[]
        {
            "admin",
            "company_admin",
            "operator",
            "technician"
        };

        foreach (var roleName in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            
            if (!roleExists)
            {
                var role = new IdentityRole(roleName)
                {
                    NormalizedName = roleName.ToUpperInvariant()
                };

                var result = await roleManager.CreateAsync(role);
                
                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully.", roleName);
                }
                else
                {
                    logger.LogError("Failed to create role '{RoleName}'. Errors: {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogDebug("Role '{RoleName}' already exists.", roleName);
            }
        }
    }
}


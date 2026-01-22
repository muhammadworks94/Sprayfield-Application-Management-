using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SAM.Domain.Entities;

namespace SAM.Data.Seeders;

/// <summary>
/// Seeds default users for the application.
/// </summary>
public static class UserSeeder
{
    /// <summary>
    /// Seeds the default admin user if it doesn't exist.
    /// </summary>
    public static async Task SeedAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        string adminEmail = "admin@sam.local",
        string adminPassword = "Admin@123",
        string adminFullName = "System Administrator")
    {
        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (existingUser != null)
        {
            logger.LogDebug("Admin user '{Email}' already exists.", adminEmail);
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FullName = adminFullName,
            CompanyId = null, // Global admin has no company
            IsActive = true
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        
        if (result.Succeeded)
        {
            // Assign admin role
            var roleResult = await userManager.AddToRoleAsync(adminUser, "admin");
            
            if (roleResult.Succeeded)
            {
                logger.LogInformation("Admin user '{Email}' created successfully with admin role.", adminEmail);
            }
            else
            {
                logger.LogError("Failed to assign admin role to user '{Email}'. Errors: {Errors}",
                    adminEmail, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogError("Failed to create admin user '{Email}'. Errors: {Errors}",
                adminEmail, string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}


using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SAM.Domain.Entities;

namespace SAM.Data.Seeders;

/// <summary>
/// Main database seeder that orchestrates all seeding operations.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with initial data.
    /// Should be called during application startup in development environment.
    /// </summary>
    public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        
        var loggerFactory = services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DatabaseSeeder");
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        try
        {
            logger.LogInformation("Starting database seeding...");

            // Seed roles first
            await RoleSeeder.SeedRolesAsync(roleManager, logger);

            // Seed default admin user
            await UserSeeder.SeedAdminUserAsync(userManager, logger);

            // Seed companies and reference data
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            await CompanySeeder.SeedCompaniesAsync(dbContext, logger);

            logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }
}


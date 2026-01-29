using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAM.Data.Configurations;
using SAM.Domain.Entities;
using SAM.Domain.Entities.Base;
using SAM.Infrastructure;

namespace SAM.Data;

/// <summary>
/// Application database context with Identity support and audit automation.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Soil> Soils => Set<Soil>();
    public DbSet<Nozzle> Nozzles => Set<Nozzle>();
    public DbSet<Crop> Crops => Set<Crop>();
    public DbSet<Sprayfield> Sprayfields => Set<Sprayfield>();
    public DbSet<MonitoringWell> MonitoringWells => Set<MonitoringWell>();
    public DbSet<WWChar> WWChars => Set<WWChar>();
    public DbSet<GWMonit> GWMonits => Set<GWMonit>();
    public DbSet<Irrigate> Irrigates => Set<Irrigate>();
    public DbSet<IrrRprt> IrrRprts => Set<IrrRprt>();
    public DbSet<OperatorLog> OperatorLogs => Set<OperatorLog>();
    public DbSet<UserRequest> UserRequests => Set<UserRequest>();
    public DbSet<AdminRequest> AdminRequests => Set<AdminRequest>();
    public DbSet<CompanyRequest> CompanyRequests => Set<CompanyRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply global query filter for soft-deleted entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(SetGlobalQueryFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                    .MakeGenericMethod(entityType.ClrType);

                method?.Invoke(null, new object[] { builder, entityType });
            }
        }

        // Apply entity configurations
        builder.ApplyConfiguration(new CompanyConfiguration());
        builder.ApplyConfiguration(new FacilityConfiguration());
        builder.ApplyConfiguration(new SoilConfiguration());
        builder.ApplyConfiguration(new NozzleConfiguration());
        builder.ApplyConfiguration(new CropConfiguration());
        builder.ApplyConfiguration(new SprayfieldConfiguration());
        builder.ApplyConfiguration(new MonitoringWellConfiguration());
        builder.ApplyConfiguration(new WWCharConfiguration());
        builder.ApplyConfiguration(new GWMonitConfiguration());
        builder.ApplyConfiguration(new IrrigateConfiguration());
        builder.ApplyConfiguration(new IrrRprtConfiguration());
        builder.ApplyConfiguration(new OperatorLogConfiguration());
        builder.ApplyConfiguration(new UserRequestConfiguration());
        builder.ApplyConfiguration(new AdminRequestConfiguration());
        builder.ApplyConfiguration(new CompanyRequestConfiguration());

        // Configure Identity table names and ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.ToTable("Users");
            
            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.HasIndex(u => u.CompanyId);
            
            entity.HasOne(u => u.Company)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<IdentityRole>(entity =>
        {
            entity.ToTable("Roles");
        });

        builder.Entity<IdentityUserRole<string>>(entity =>
        {
            entity.ToTable("UserRoles");
        });

        builder.Entity<IdentityUserClaim<string>>(entity =>
        {
            entity.ToTable("UserClaims");
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.ToTable("UserLogins");
        });

        builder.Entity<IdentityRoleClaim<string>>(entity =>
        {
            entity.ToTable("RoleClaims");
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.ToTable("UserTokens");
        });
    }

    /// <summary>
    /// Sets global query filter for soft-deleted entities.
    /// </summary>
    private static void SetGlobalQueryFilter<T>(ModelBuilder builder, Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
        where T : AuditableEntity
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<AuditableEntity>();

        foreach (var entry in entries)
        {
            var currentUserEmail = GetCurrentUserEmail();

            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTime.UtcNow;
                    entry.Entity.CreatedBy = currentUserEmail;
                    entry.Entity.IsDeleted = false;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedDate = DateTime.UtcNow;
                    // Don't update CreatedBy on modification
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Gets the current user's email from the HTTP context.
    /// Uses a service locator pattern to access IHttpContextAccessor.
    /// </summary>
    private string GetCurrentUserEmail()
    {
        try
        {
            var httpContextAccessor = ServiceLocator.GetService<IHttpContextAccessor>();
            
            if (httpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContextAccessor.HttpContext.User.Identity.Name ?? "system";
            }
        }
        catch
        {
            // If service locator is not initialized (e.g., during migrations), return "system"
        }

        return "system";
    }
}


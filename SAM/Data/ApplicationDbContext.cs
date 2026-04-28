using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SAM.Data.Configurations;
using SAM.Domain.Entities;
using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;
using SAM.Infrastructure;
using System.Security.Claims;
using System.Text.Json;

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
    public DbSet<ApplicationZone> ApplicationZones => Set<ApplicationZone>();
    public DbSet<MonthlyApplication> MonthlyApplications => Set<MonthlyApplication>();
    public DbSet<LoadCalculation> LoadCalculations => Set<LoadCalculation>();
    public DbSet<MonitoringWell> MonitoringWells => Set<MonitoringWell>();
    public DbSet<WWChar> WWChars => Set<WWChar>();
    public DbSet<GWMonit> GWMonits => Set<GWMonit>();
    public DbSet<IrrRprt> IrrRprts => Set<IrrRprt>();
    public DbSet<NDAR1> NDAR1s => Set<NDAR1>();
    public DbSet<NDAR1Field> NDAR1Fields => Set<NDAR1Field>();
    public DbSet<NDAR1FieldDaily> NDAR1FieldDailies => Set<NDAR1FieldDaily>();
    public DbSet<OperatorLog> OperatorLogs => Set<OperatorLog>();
    public DbSet<UserRequest> UserRequests => Set<UserRequest>();
    public DbSet<CompanyRequest> CompanyRequests => Set<CompanyRequest>();
    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();

    private static readonly HashSet<string> IgnoredActivityFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "ConcurrencyStamp",
        "SecurityStamp",
        "PasswordHash",
        "NormalizedUserName",
        "NormalizedEmail",
        "LockoutEnd",
        "AccessFailedCount",
        "TwoFactorEnabled",
        "UpdatedDate",
        "CreatedDate"
    };

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
        builder.ApplyConfiguration(new ApplicationZoneConfiguration());
        builder.ApplyConfiguration(new MonthlyApplicationConfiguration());
        builder.ApplyConfiguration(new LoadCalculationConfiguration());
        builder.ApplyConfiguration(new MonitoringWellConfiguration());
        builder.ApplyConfiguration(new WWCharConfiguration());
        builder.ApplyConfiguration(new GWMonitConfiguration());
        builder.ApplyConfiguration(new IrrRprtConfiguration());
        builder.ApplyConfiguration(new NDAR1Configuration());
        builder.ApplyConfiguration(new NDAR1FieldConfiguration());
        builder.ApplyConfiguration(new NDAR1FieldDailyConfiguration());
        builder.ApplyConfiguration(new OperatorLogConfiguration());
        builder.ApplyConfiguration(new UserRequestConfiguration());
        builder.ApplyConfiguration(new CompanyRequestConfiguration());
        builder.ApplyConfiguration(new SmtpSettingsConfiguration());
        builder.ApplyConfiguration(new EmailTemplateConfiguration());
        builder.ApplyConfiguration(new UserActivityLogConfiguration());
        builder.ApplyConfiguration(new ErrorLogConfiguration());

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
        var activityEvents = BuildActivityLogEvents();
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

        if (activityEvents.Count > 0)
        {
            UserActivityLogs.AddRange(activityEvents);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private List<UserActivityLog> BuildActivityLogEvents()
    {
        var now = DateTime.UtcNow;
        var context = GetCurrentRequestContext();
        var events = new List<UserActivityLog>();

        var entries = ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => e.Entity is not UserActivityLog && e.Entity is not ErrorLog)
            .ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Metadata.ClrType.Name;

            var activityType = entry.State switch
            {
                EntityState.Added => UserActivityType.Create,
                EntityState.Deleted => UserActivityType.Delete,
                EntityState.Modified when IsSoftDelete(entry) => UserActivityType.Delete,
                EntityState.Modified => UserActivityType.Update,
                _ => (UserActivityType?)null
            };

            if (!activityType.HasValue)
            {
                continue;
            }

            var changedFields = activityType.Value == UserActivityType.Update || activityType.Value == UserActivityType.Delete
                ? GetChangedFieldNames(entry)
                : new List<string>();

            // For updates where no meaningful field changed, skip noisy events.
            if (activityType.Value == UserActivityType.Update && changedFields.Count == 0)
            {
                continue;
            }

            var companyId = ResolveCompanyId(entry);
            var entityId = ResolveEntityId(entry);
            var module = ResolveModuleName(context.Path);

            events.Add(new UserActivityLog
            {
                OccurredAtUtc = now,
                ActivityType = activityType.Value,
                Module = module,
                EntityName = entityName,
                EntityId = entityId,
                CompanyId = companyId,
                ActorUserId = context.ActorUserId,
                ActorEmail = context.ActorEmail,
                ActorDisplayName = context.ActorDisplayName,
                HttpMethod = context.HttpMethod,
                Path = context.Path,
                IpAddress = context.IpAddress,
                ChangedFields = changedFields.Count > 0 ? JsonSerializer.Serialize(changedFields) : null,
                Summary = BuildSummary(context.ActorEmail, activityType.Value, entityName, entityId),
                CorrelationId = context.CorrelationId
            });
        }

        return events;
    }

    private static bool IsSoftDelete(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var isDeletedProperty = entry.Properties.FirstOrDefault(x => x.Metadata.Name == nameof(AuditableEntity.IsDeleted));
        if (isDeletedProperty == null)
        {
            return false;
        }

        return isDeletedProperty.OriginalValue is bool original
            && isDeletedProperty.CurrentValue is bool current
            && !original
            && current;
    }

    private static List<string> GetChangedFieldNames(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var changedFields = new List<string>();

        foreach (var property in entry.Properties)
        {
            if (!property.IsModified)
            {
                continue;
            }

            var propertyName = property.Metadata.Name;
            if (IgnoredActivityFields.Contains(propertyName))
            {
                continue;
            }

            changedFields.Add(propertyName);
        }

        return changedFields;
    }

    private static string? ResolveEntityId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var pk = entry.Metadata.FindPrimaryKey();
        if (pk == null || pk.Properties.Count == 0)
        {
            return null;
        }

        var keyName = pk.Properties[0].Name;
        var current = entry.Property(keyName).CurrentValue;
        var original = entry.Property(keyName).OriginalValue;

        return current?.ToString() ?? original?.ToString();
    }

    private static Guid? ResolveCompanyId(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        if (entry.Entity is CompanyScopedEntity scoped)
        {
            return scoped.CompanyId;
        }

        if (entry.Entity is ApplicationUser user)
        {
            return user.CompanyId;
        }

        var companyIdProperty = entry.Properties.FirstOrDefault(x => x.Metadata.Name == "CompanyId");
        if (companyIdProperty?.CurrentValue is Guid current)
        {
            return current;
        }

        if (companyIdProperty?.OriginalValue is Guid original)
        {
            return original;
        }

        return null;
    }

    private static string ResolveModuleName(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Background";
        }

        var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 0 ? parts[0] : "Unknown";
    }

    private static string BuildSummary(string actorEmail, UserActivityType activityType, string entityName, string? entityId)
    {
        if (!string.IsNullOrWhiteSpace(entityId))
        {
            return $"{actorEmail} {activityType}d {entityName} ({entityId})";
        }

        return $"{actorEmail} {activityType}d {entityName}";
    }

    private RequestContext GetCurrentRequestContext()
    {
        try
        {
            var accessor = ServiceLocator.GetService<IHttpContextAccessor>();
            var httpContext = accessor?.HttpContext;
            var user = httpContext?.User;

            var actorUserId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var actorEmail = user?.Identity?.Name ?? "system";
            var actorDisplayName = user?.FindFirstValue("FullName") ?? user?.FindFirstValue(ClaimTypes.Name);

            return new RequestContext
            {
                ActorUserId = actorUserId,
                ActorEmail = actorEmail,
                ActorDisplayName = actorDisplayName,
                HttpMethod = httpContext?.Request?.Method,
                Path = httpContext?.Request?.Path.Value,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
                CorrelationId = httpContext?.TraceIdentifier
            };
        }
        catch
        {
            return new RequestContext
            {
                ActorEmail = "system"
            };
        }
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

    private sealed class RequestContext
    {
        public string? ActorUserId { get; set; }
        public string ActorEmail { get; set; } = "system";
        public string? ActorDisplayName { get; set; }
        public string? HttpMethod { get; set; }
        public string? Path { get; set; }
        public string? IpAddress { get; set; }
        public string? CorrelationId { get; set; }
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Data.Seeders;
using SAM.Domain.Entities;
using SAM.Infrastructure.Authorization;
using SAM.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add HttpContextAccessor for DbContext audit fields
builder.Services.AddHttpContextAccessor();

// Configure Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings from appsettings.json
    var passwordSection = builder.Configuration.GetSection("Identity:Password");
    options.Password.RequireDigit = passwordSection.GetValue<bool>("RequireDigit", true);
    options.Password.RequiredLength = passwordSection.GetValue<int>("RequiredLength", 8);
    options.Password.RequireNonAlphanumeric = passwordSection.GetValue<bool>("RequireNonAlphanumeric", true);
    options.Password.RequireUppercase = passwordSection.GetValue<bool>("RequireUppercase", true);
    options.Password.RequireLowercase = passwordSection.GetValue<bool>("RequireLowercase", true);

    // Lockout settings from appsettings.json
    var lockoutSection = builder.Configuration.GetSection("Identity:Lockout");
    options.Lockout.AllowedForNewUsers = lockoutSection.GetValue<bool>("AllowedForNewUsers", true);
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.Parse(lockoutSection.GetValue<string>("DefaultLockoutTimeSpan") ?? "00:15:00");
    options.Lockout.MaxFailedAccessAttempts = lockoutSection.GetValue<int>("MaxFailedAccessAttempts", 5);

    // User settings
    options.User.RequireUniqueEmail = true;

    // Sign-in settings
    options.SignIn.RequireConfirmedEmail = false; // Set to true if email confirmation is required
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure Cookie Authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, CompanyAccessHandler>();

// Register services
builder.Services.AddScoped<SAM.Services.Interfaces.ICompanyService, SAM.Services.Implementations.CompanyService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IFacilityService, SAM.Services.Implementations.FacilityService>();
builder.Services.AddScoped<SAM.Services.Interfaces.ISoilService, SAM.Services.Implementations.SoilService>();
builder.Services.AddScoped<SAM.Services.Interfaces.INozzleService, SAM.Services.Implementations.NozzleService>();
builder.Services.AddScoped<SAM.Services.Interfaces.ICropService, SAM.Services.Implementations.CropService>();
builder.Services.AddScoped<SAM.Services.Interfaces.ISprayfieldService, SAM.Services.Implementations.SprayfieldService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IMonitoringWellService, SAM.Services.Implementations.MonitoringWellService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IOperatorLogService, SAM.Services.Implementations.OperatorLogService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IIrrigateService, SAM.Services.Implementations.IrrigateService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IWWCharService, SAM.Services.Implementations.WWCharService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IGWMonitService, SAM.Services.Implementations.GWMonitService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IIrrRprtService, SAM.Services.Implementations.IrrRprtService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IUserRequestService, SAM.Services.Implementations.UserRequestService>();
builder.Services.AddScoped<SAM.Services.Interfaces.IAdminRequestService, SAM.Services.Implementations.AdminRequestService>();

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(Policies.RequireAdmin, policy => 
        policy.RequireRole("admin"));

    options.AddPolicy(Policies.RequireCompanyAdmin, policy => 
        policy.RequireRole("admin", "company_admin"));

    options.AddPolicy(Policies.RequireOperator, policy => 
        policy.RequireRole("admin", "company_admin", "operator"));

    options.AddPolicy(Policies.RequireTechnician, policy => 
        policy.RequireRole("admin", "company_admin", "technician"));

    // RequireCompanyAccess policy uses custom handler for company-scoped access
    options.AddPolicy(Policies.RequireCompanyAccess, policy => 
        policy.Requirements.Add(new CompanyAccessRequirement()));
});

// Add logging
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
    config.AddConfiguration(builder.Configuration.GetSection("Logging"));
});

var app = builder.Build();

// Initialize service locator for DbContext audit fields
SAM.Infrastructure.ServiceLocator.Initialize(app.Services);

// Seed database in development environment
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            await DatabaseSeeder.SeedDatabaseAsync(app.Services);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}

// Configure the HTTP request pipeline.
// Use custom exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (!app.Environment.IsDevelopment())
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

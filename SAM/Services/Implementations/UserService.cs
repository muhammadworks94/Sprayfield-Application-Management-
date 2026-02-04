using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for ApplicationUser entity operations.
/// Provides a single source of truth for user creation, updates, and management.
/// </summary>
public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;

    public UserService(
        ApplicationDbContext context,
        ILogger<UserService> logger,
        UserManager<ApplicationUser> userManager,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _emailService = emailService;
    }

    public async Task<ApplicationUser> CreateUserAsync(string email, string fullName, Guid? companyId, AppRoleEnum role, bool generatePassword = true, string? password = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty.", nameof(fullName));

        // Validate email uniqueness
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
            throw new BusinessRuleException($"A user with email '{email}' already exists.");

        // Validate company if provided
        if (companyId.HasValue)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId.Value);
            if (!companyExists)
                throw new EntityNotFoundException(nameof(Company), companyId.Value);
        }

        // Create the user
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            CompanyId = companyId,
            IsActive = true
        };

        // Generate or use provided password
        var userPassword = generatePassword ? GenerateTemporaryPassword() : password!;
        if (string.IsNullOrWhiteSpace(userPassword))
            throw new ArgumentException("Password is required when generatePassword is false.", nameof(password));

        var result = await _userManager.CreateAsync(user, userPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to create user: {errors}");
        }

        // Assign role
        var roleName = MapAppRoleToRoleName(role);
        var roleResult = await _userManager.AddToRoleAsync(user, roleName);
        if (!roleResult.Succeeded)
        {
            // Rollback user creation if role assignment fails
            await _userManager.DeleteAsync(user);
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to assign role: {errors}");
        }

        _logger.LogInformation("User created: {Email} (User ID: {UserId}, Role: {Role}, Company ID: {CompanyId})",
            email, user.Id, roleName, companyId);

        // Send email notification with temporary password if generated
        if (generatePassword)
        {
            try
            {
                // Load company name if companyId is provided
                string? companyName = null;
                if (companyId.HasValue)
                {
                    var company = await _context.Companies
                        .FirstOrDefaultAsync(c => c.Id == companyId.Value);
                    companyName = company?.Name;
                }

                // Format role for display
                var roleDisplayName = FormatRoleForDisplay(role);

                // Build and send email
                var emailBody = BuildUserCreationEmailBody(email, userPassword, companyName, roleDisplayName);
                var subject = "Your SAM Account Credentials";

                await _emailService.SendEmailAsync(email, subject, emailBody);

                _logger.LogInformation("User creation email sent to {Email}", email);
            }
            catch (Exception ex)
            {
                // Log error but don't fail user creation
                _logger.LogError(ex, "Failed to send user creation email to {Email}. User was created successfully.", email);
            }
        }

        return user;
    }

    public async Task<ApplicationUser> UpdateUserAsync(string userId, string fullName, string email, Guid? companyId, AppRoleEnum role, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or empty.", nameof(email));
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty.", nameof(fullName));

        var user = await GetUserByIdAsync(userId);
        if (user == null)
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        // Validate email uniqueness (excluding current user)
        if (user.Email != email)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser != null && existingUser.Id != userId)
                throw new BusinessRuleException($"A user with email '{email}' already exists.");
        }

        // Validate company if provided
        if (companyId.HasValue)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == companyId.Value);
            if (!companyExists)
                throw new EntityNotFoundException(nameof(Company), companyId.Value);
        }

        // Update user properties
        user.FullName = fullName;
        user.Email = email;
        user.UserName = email;
        user.CompanyId = companyId;
        user.IsActive = isActive;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to update user: {errors}");
        }

        // Handle role change
        var currentRoles = await _userManager.GetRolesAsync(user);
        var newRoleName = MapAppRoleToRoleName(role);

        // Remove all current roles
        if (currentRoles.Any())
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                throw new BusinessRuleException($"Failed to remove existing roles: {errors}");
            }
        }

        // Add new role
        var addRoleResult = await _userManager.AddToRoleAsync(user, newRoleName);
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join(", ", addRoleResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to assign new role: {errors}");
        }

        _logger.LogInformation("User updated: {Email} (User ID: {UserId}, Role: {Role})",
            email, userId, newRoleName);

        return user;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));

        var user = await GetUserByIdAsync(userId);
        if (user == null)
            throw new EntityNotFoundException(nameof(ApplicationUser), userId);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to delete user: {errors}");
        }

        _logger.LogInformation("User deleted: {Email} (User ID: {UserId})", user.Email, userId);
        return true;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IEnumerable<ApplicationUser>> GetUsersByCompanyAsync(Guid? companyId = null)
    {
        var query = _context.Users
            .Include(u => u.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .ToListAsync();
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        return await _context.Users
            .Include(u => u.Company)
            .OrderBy(u => u.FullName)
            .ThenBy(u => u.Email)
            .ToListAsync();
    }

    public string GenerateTemporaryPassword()
    {
        // Generate a secure temporary password
        // In production, this should be more sophisticated
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return password + "1"; // Ensure at least one digit
    }

    /// <summary>
    /// Maps AppRoleEnum to Identity role name string.
    /// </summary>
    private string MapAppRoleToRoleName(AppRoleEnum role)
    {
        return role switch
        {
            AppRoleEnum.admin => "admin",
            AppRoleEnum.company_admin => "company_admin",
            AppRoleEnum.@operator => "operator",
            AppRoleEnum.technician => "technician",
            _ => "operator" // Default fallback
        };
    }

    /// <summary>
    /// Formats AppRoleEnum to a display-friendly role name.
    /// </summary>
    private string FormatRoleForDisplay(AppRoleEnum role)
    {
        return role switch
        {
            AppRoleEnum.admin => "Admin",
            AppRoleEnum.company_admin => "Company Admin",
            AppRoleEnum.@operator => "Operator",
            AppRoleEnum.technician => "Technician",
            _ => "Operator" // Default fallback
        };
    }

    /// <summary>
    /// Builds HTML email body for user creation notification.
    /// </summary>
    private string BuildUserCreationEmailBody(string email, string password, string? companyName, string roleDisplayName)
    {
        var companyInfo = string.IsNullOrWhiteSpace(companyName) ? "N/A" : companyName;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #2563eb;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #f9fafb;
            padding: 20px;
            border: 1px solid #e5e7eb;
            border-top: none;
        }}
        .credentials {{
            background-color: white;
            padding: 15px;
            border-radius: 5px;
            margin: 15px 0;
            border-left: 4px solid #2563eb;
        }}
        .info-row {{
            margin: 10px 0;
            padding: 8px 0;
            border-bottom: 1px solid #e5e7eb;
        }}
        .info-row:last-child {{
            border-bottom: none;
        }}
        .label {{
            font-weight: bold;
            color: #4b5563;
            display: inline-block;
            width: 120px;
        }}
        .value {{
            color: #111827;
        }}
        .password {{
            font-family: monospace;
            font-size: 16px;
            font-weight: bold;
            color: #dc2626;
            background-color: #fee2e2;
            padding: 10px;
            border-radius: 4px;
            text-align: center;
            margin: 10px 0;
        }}
        .warning {{
            background-color: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 15px;
            margin: 15px 0;
            border-radius: 4px;
        }}
        .footer {{
            text-align: center;
            color: #6b7280;
            font-size: 12px;
            margin-top: 20px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <h1>Welcome to SAM</h1>
    </div>
    <div class=""content"">
        <p>Hello,</p>
        <p>Your account has been created in the SAM system. Please find your login credentials below:</p>
        
        <div class=""credentials"">
            <div class=""info-row"">
                <span class=""label"">Email:</span>
                <span class=""value"">{email}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Temporary Password:</span>
            </div>
            <div class=""password"">{password}</div>
            <div class=""info-row"">
                <span class=""label"">Company:</span>
                <span class=""value"">{companyInfo}</span>
            </div>
            <div class=""info-row"">
                <span class=""label"">Role:</span>
                <span class=""value"">{roleDisplayName}</span>
            </div>
        </div>

        <div class=""warning"">
            <strong>Important:</strong> This is a temporary password. Please change your password immediately after your first login for security purposes.
        </div>

        <p>You can now log in to the SAM system using the credentials provided above.</p>
        
        <p>If you have any questions or need assistance, please contact your system administrator.</p>
    </div>
    <div class=""footer"">
        <p>This is an automated message. Please do not reply to this email.</p>
    </div>
</body>
</html>";
    }
}


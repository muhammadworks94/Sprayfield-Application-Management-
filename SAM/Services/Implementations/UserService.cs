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

    public UserService(
        ApplicationDbContext context,
        ILogger<UserService> logger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
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

        // TODO: Send email notification with temporary password if generated

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

        return await _userManager.FindByIdAsync(userId);
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
}


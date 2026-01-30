using SAM.Domain.Entities;
using SAM.Domain.Enums;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for ApplicationUser entity operations.
/// Provides a single source of truth for user creation, updates, and management.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Creates a new user with the specified details.
    /// </summary>
    /// <param name="email">User's email address (also used as username)</param>
    /// <param name="fullName">User's full name</param>
    /// <param name="companyId">Company ID (null for global admins)</param>
    /// <param name="role">Application role to assign</param>
    /// <param name="generatePassword">Whether to auto-generate a temporary password</param>
    /// <param name="password">Password to use if generatePassword is false</param>
    /// <returns>Created ApplicationUser</returns>
    Task<ApplicationUser> CreateUserAsync(string email, string fullName, Guid? companyId, AppRoleEnum role, bool generatePassword = true, string? password = null);

    /// <summary>
    /// Updates an existing user's information and role.
    /// </summary>
    /// <param name="userId">User ID to update</param>
    /// <param name="fullName">Updated full name</param>
    /// <param name="email">Updated email address</param>
    /// <param name="companyId">Updated company ID</param>
    /// <param name="role">Updated application role</param>
    /// <param name="isActive">Updated active status</param>
    /// <returns>Updated ApplicationUser</returns>
    Task<ApplicationUser> UpdateUserAsync(string userId, string fullName, string email, Guid? companyId, AppRoleEnum role, bool isActive);

    /// <summary>
    /// Permanently deletes a user from the system.
    /// </summary>
    /// <param name="userId">User ID to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteUserAsync(string userId);

    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>ApplicationUser or null if not found</returns>
    Task<ApplicationUser?> GetUserByIdAsync(string userId);

    /// <summary>
    /// Gets all users filtered by company.
    /// </summary>
    /// <param name="companyId">Company ID to filter by (null returns all users)</param>
    /// <returns>List of ApplicationUsers</returns>
    Task<IEnumerable<ApplicationUser>> GetUsersByCompanyAsync(Guid? companyId = null);

    /// <summary>
    /// Gets all users in the system.
    /// </summary>
    /// <returns>List of all ApplicationUsers</returns>
    Task<IEnumerable<ApplicationUser>> GetAllUsersAsync();

    /// <summary>
    /// Generates a secure temporary password for new users.
    /// </summary>
    /// <returns>Generated password string</returns>
    string GenerateTemporaryPassword();
}


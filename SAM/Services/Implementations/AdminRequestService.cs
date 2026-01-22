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
/// Service implementation for AdminRequest entity operations.
/// </summary>
public class AdminRequestService : IAdminRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminRequestService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminRequestService(
        ApplicationDbContext context,
        ILogger<AdminRequestService> logger,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<IEnumerable<AdminRequest>> GetAllAsync()
    {
        return await _context.AdminRequests
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
    }

    public async Task<AdminRequest?> GetByIdAsync(Guid id)
    {
        return await _context.AdminRequests
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<AdminRequest> CreateAsync(AdminRequest adminRequest)
    {
        if (adminRequest == null)
            throw new ArgumentNullException(nameof(adminRequest));

        // Validate request type
        if (adminRequest.RequestType == AdminRequestTypeEnum.CreateAdmin)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(adminRequest.TargetEmail);
            if (existingUser != null)
                throw new BusinessRuleException($"A user with email '{adminRequest.TargetEmail}' already exists.");

            // Check if pending request already exists for this email
            var existingRequest = await _context.AdminRequests
                .FirstOrDefaultAsync(a => a.TargetEmail.ToLower() == adminRequest.TargetEmail.ToLower() && 
                                         a.Status == RequestStatusEnum.Pending &&
                                         a.RequestType == AdminRequestTypeEnum.CreateAdmin);
            if (existingRequest != null)
                throw new BusinessRuleException($"A pending admin creation request already exists for email '{adminRequest.TargetEmail}'.");
        }
        else if (adminRequest.RequestType == AdminRequestTypeEnum.DisableAdmin)
        {
            // Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(adminRequest.TargetEmail);
            if (existingUser == null)
                throw new BusinessRuleException($"User with email '{adminRequest.TargetEmail}' does not exist.");

            // Check if user is an admin
            var isAdmin = await _userManager.IsInRoleAsync(existingUser, "admin");
            if (!isAdmin)
                throw new BusinessRuleException($"User '{adminRequest.TargetEmail}' is not an administrator.");

            // Check if pending request already exists
            var existingRequest = await _context.AdminRequests
                .FirstOrDefaultAsync(a => a.TargetEmail.ToLower() == adminRequest.TargetEmail.ToLower() && 
                                         a.Status == RequestStatusEnum.Pending &&
                                         a.RequestType == AdminRequestTypeEnum.DisableAdmin);
            if (existingRequest != null)
                throw new BusinessRuleException($"A pending admin disable request already exists for email '{adminRequest.TargetEmail}'.");
        }

        adminRequest.Status = RequestStatusEnum.Pending;
        _context.AdminRequests.Add(adminRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin request created: {RequestType} for {TargetEmail} (ID: {RequestId})",
            adminRequest.RequestType, adminRequest.TargetEmail, adminRequest.Id);
        return adminRequest;
    }

    public async Task<AdminRequest> UpdateAsync(AdminRequest adminRequest)
    {
        if (adminRequest == null)
            throw new ArgumentNullException(nameof(adminRequest));

        var existing = await GetByIdAsync(adminRequest.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(AdminRequest), adminRequest.Id);

        existing.TargetEmail = adminRequest.TargetEmail;
        existing.TargetFullName = adminRequest.TargetFullName;
        existing.Justification = adminRequest.Justification;
        existing.Status = adminRequest.Status;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin request updated (ID: {RequestId})", adminRequest.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var adminRequest = await GetByIdAsync(id);
        if (adminRequest == null)
            throw new EntityNotFoundException(nameof(AdminRequest), id);

        // Soft delete
        adminRequest.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin request soft-deleted (ID: {RequestId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.AdminRequests.AnyAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<AdminRequest>> GetPendingRequestsAsync()
    {
        return await _context.AdminRequests
            .Where(a => a.Status == RequestStatusEnum.Pending)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
    }

    public async Task<AdminRequest> ApproveRequestAsync(Guid requestId, string approvedByEmail)
    {
        var request = await GetByIdAsync(requestId);
        if (request == null)
            throw new EntityNotFoundException(nameof(AdminRequest), requestId);

        if (request.Status != RequestStatusEnum.Pending)
            throw new BusinessRuleException("Only pending requests can be approved.");

        if (request.RequestType == AdminRequestTypeEnum.CreateAdmin)
        {
            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(request.TargetEmail);
            if (existingUser != null)
                throw new BusinessRuleException($"A user with email '{request.TargetEmail}' already exists.");

            // Create the admin user
            var user = new ApplicationUser
            {
                UserName = request.TargetEmail,
                Email = request.TargetEmail,
                EmailConfirmed = true,
                FullName = request.TargetFullName,
                CompanyId = null, // Global admins don't have a company
                IsActive = true
            };

            // Generate a temporary password
            var tempPassword = GenerateTemporaryPassword();
            var result = await _userManager.CreateAsync(user, tempPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new BusinessRuleException($"Failed to create admin user: {errors}");
            }

            // Assign admin role
            var roleResult = await _userManager.AddToRoleAsync(user, "admin");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                throw new BusinessRuleException($"Failed to assign admin role: {errors}");
            }

            _logger.LogInformation("Admin user created: {Email} (Request ID: {RequestId}, User ID: {UserId})",
                request.TargetEmail, requestId, user.Id);
        }
        else if (request.RequestType == AdminRequestTypeEnum.DisableAdmin)
        {
            var user = await _userManager.FindByEmailAsync(request.TargetEmail);
            if (user == null)
                throw new EntityNotFoundException("User", request.TargetEmail);

            // Disable the user
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin user disabled: {Email} (Request ID: {RequestId})",
                request.TargetEmail, requestId);
        }

        // Update request status
        request.Status = RequestStatusEnum.Processed;
        await _context.SaveChangesAsync();

        // TODO: Send email notification

        return request;
    }

    public async Task<AdminRequest> RejectRequestAsync(Guid requestId, string rejectedByEmail, string? reason = null)
    {
        var request = await GetByIdAsync(requestId);
        if (request == null)
            throw new EntityNotFoundException(nameof(AdminRequest), requestId);

        if (request.Status != RequestStatusEnum.Pending)
            throw new BusinessRuleException("Only pending requests can be rejected.");

        // Update request status
        request.Status = RequestStatusEnum.Processed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Admin request rejected: {RequestType} for {TargetEmail} (Request ID: {RequestId})",
            request.RequestType, request.TargetEmail, requestId);

        // TODO: Send email notification about rejection

        return request;
    }

    private string GenerateTemporaryPassword()
    {
        const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        var password = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        return password + "1";
    }
}


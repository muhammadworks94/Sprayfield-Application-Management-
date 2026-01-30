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
/// Service implementation for UserRequest entity operations.
/// </summary>
public class UserRequestService : IUserRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRequestService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserService _userService;

    public UserRequestService(
        ApplicationDbContext context,
        ILogger<UserRequestService> logger,
        UserManager<ApplicationUser> userManager,
        IUserService userService)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _userService = userService;
    }

    public async Task<IEnumerable<UserRequest>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.UserRequests
            .Include(u => u.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == companyId.Value);
        }

        return await query
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();
    }

    public async Task<UserRequest?> GetByIdAsync(Guid id)
    {
        return await _context.UserRequests
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<UserRequest> CreateAsync(UserRequest userRequest)
    {
        if (userRequest == null)
            throw new ArgumentNullException(nameof(userRequest));

        // Validate company exists (only if CompanyId is provided - self-signups may have null CompanyId)
        if (userRequest.CompanyId.HasValue)
        {
            var companyExists = await _context.Companies.AnyAsync(c => c.Id == userRequest.CompanyId.Value);
            if (!companyExists)
                throw new EntityNotFoundException(nameof(Company), userRequest.CompanyId.Value);
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(userRequest.Email);
        if (existingUser != null)
            throw new BusinessRuleException($"A user with email '{userRequest.Email}' already exists.");

        // Check if pending request already exists for this email
        var existingRequest = await _context.UserRequests
            .FirstOrDefaultAsync(u => u.Email.ToLower() == userRequest.Email.ToLower() && u.Status == RequestStatusEnum.Pending);
        if (existingRequest != null)
            throw new BusinessRuleException($"A pending user request already exists for email '{userRequest.Email}'.");

        userRequest.Status = RequestStatusEnum.Pending;
        _context.UserRequests.Add(userRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User request created for {Email} in company {CompanyName} (ID: {RequestId})",
            userRequest.Email, userRequest.CompanyName, userRequest.Id);
        return userRequest;
    }

    public async Task<UserRequest> UpdateAsync(UserRequest userRequest)
    {
        if (userRequest == null)
            throw new ArgumentNullException(nameof(userRequest));

        var existing = await GetByIdAsync(userRequest.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(UserRequest), userRequest.Id);

        existing.FullName = userRequest.FullName;
        existing.Email = userRequest.Email;
        existing.AppRole = userRequest.AppRole;
        existing.Status = userRequest.Status;

        await _context.SaveChangesAsync();

        _logger.LogInformation("User request updated (ID: {RequestId})", userRequest.Id);
        return existing;
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var userRequest = await GetByIdAsync(id);
        if (userRequest == null)
            throw new EntityNotFoundException(nameof(UserRequest), id);

        // Soft delete
        userRequest.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User request soft-deleted (ID: {RequestId})", id);
        return true;
    }

    public async Task<bool> HardDeleteAsync(Guid id)
    {
        var userRequest = await _context.UserRequests
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (userRequest == null)
            throw new EntityNotFoundException(nameof(UserRequest), id);

        // Hard delete - permanently remove from database
        _context.UserRequests.Remove(userRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User request hard-deleted (ID: {RequestId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.UserRequests.AnyAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<UserRequest>> GetPendingRequestsAsync(Guid? companyId = null)
    {
        var query = _context.UserRequests
            .Where(u => u.Status == RequestStatusEnum.Pending)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(u => u.CompanyId == companyId.Value);
        }

        return await query
            .OrderByDescending(u => u.CreatedDate)
            .ToListAsync();
    }

    public async Task<UserRequest> ApproveRequestAsync(Guid requestId, string approvedByEmail, AppRoleEnum? approvedRole = null)
    {
        var request = await GetByIdAsync(requestId);
        if (request == null)
            throw new EntityNotFoundException(nameof(UserRequest), requestId);

        if (request.Status != RequestStatusEnum.Pending)
            throw new BusinessRuleException("Only pending requests can be approved.");

        // Use approved role if provided, otherwise use requested role
        var roleToAssign = approvedRole ?? request.AppRole;

        // Create the user using the shared service (single source of truth)
        var user = await _userService.CreateUserAsync(
            request.Email,
            request.FullName,
            request.CompanyId,
            roleToAssign,
            generatePassword: true);

        // Update request status
        request.Status = RequestStatusEnum.Processed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User request approved and user created: {Email} (Request ID: {RequestId}, User ID: {UserId}, Role: {Role})",
            request.Email, requestId, user.Id, roleToAssign);

        // TODO: Send email notification with temporary password

        return request;
    }

    public async Task<UserRequest> RejectRequestAsync(Guid requestId, string rejectedByEmail, string? reason = null)
    {
        var request = await GetByIdAsync(requestId);
        if (request == null)
            throw new EntityNotFoundException(nameof(UserRequest), requestId);

        if (request.Status != RequestStatusEnum.Pending)
            throw new BusinessRuleException("Only pending requests can be rejected.");

        // Update request status (we'll use Processed status for rejected requests)
        // In a more sophisticated system, you might want a separate Rejected status
        request.Status = RequestStatusEnum.Processed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User request rejected: {Email} (Request ID: {RequestId})",
            request.Email, requestId);

        // TODO: Send email notification about rejection

        return request;
    }

}


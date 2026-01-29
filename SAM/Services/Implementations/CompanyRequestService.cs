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
/// Service implementation for CompanyRequest entity operations.
/// </summary>
public class CompanyRequestService : ICompanyRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<CompanyRequestService> _logger;

    public CompanyRequestService(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        ILogger<CompanyRequestService> logger)
    {
        _context = context;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IEnumerable<CompanyRequest>> GetAllAsync()
    {
        return await _context.CompanyRequests
            .Include(c => c.CreatedCompany)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();
    }

    public async Task<CompanyRequest?> GetByIdAsync(Guid id)
    {
        return await _context.CompanyRequests
            .Include(c => c.CreatedCompany)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CompanyRequest> CreateAsync(CompanyRequest companyRequest)
    {
        if (companyRequest == null)
            throw new ArgumentNullException(nameof(companyRequest));

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(companyRequest.RequesterEmail);
        if (existingUser != null)
            throw new BusinessRuleException($"A user with email '{companyRequest.RequesterEmail}' already exists.");

        // Check if company with same name already exists
        var existingCompany = await _context.Companies
            .AnyAsync(c => c.Name.ToLower() == companyRequest.CompanyName.ToLower());
        if (existingCompany)
            throw new BusinessRuleException($"A company with the name '{companyRequest.CompanyName}' already exists.");

        // Check if pending request already exists for this email
        var existingRequest = await _context.CompanyRequests
            .FirstOrDefaultAsync(c => c.RequesterEmail.ToLower() == companyRequest.RequesterEmail.ToLower() 
                && c.Status == RequestStatusEnum.Pending);
        if (existingRequest != null)
            throw new BusinessRuleException($"A pending company request already exists for email '{companyRequest.RequesterEmail}'.");

        // Check if pending request already exists for this company name
        var existingCompanyRequest = await _context.CompanyRequests
            .FirstOrDefaultAsync(c => c.CompanyName.ToLower() == companyRequest.CompanyName.ToLower() 
                && c.Status == RequestStatusEnum.Pending);
        if (existingCompanyRequest != null)
            throw new BusinessRuleException($"A pending request already exists for company '{companyRequest.CompanyName}'.");

        companyRequest.Status = RequestStatusEnum.Pending;
        _context.CompanyRequests.Add(companyRequest);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company request created for '{CompanyName}' by {RequesterEmail} (ID: {RequestId})",
            companyRequest.CompanyName, companyRequest.RequesterEmail, companyRequest.Id);
        return companyRequest;
    }

    public async Task<CompanyRequest> ApproveRequestAsync(Guid requestId, string approvedByEmail)
    {
        var request = await GetByIdAsync(requestId);
        if (request == null)
            throw new EntityNotFoundException(nameof(CompanyRequest), requestId);

        if (request.Status != RequestStatusEnum.Pending)
            throw new BusinessRuleException("Only pending requests can be approved.");

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.RequesterEmail);
        if (existingUser != null)
            throw new BusinessRuleException($"A user with email '{request.RequesterEmail}' already exists.");

        // Check if company already exists
        var existingCompany = await _context.Companies
            .AnyAsync(c => c.Name.ToLower() == request.CompanyName.ToLower());
        if (existingCompany)
            throw new BusinessRuleException($"A company with the name '{request.CompanyName}' already exists.");

        // Create the company
        var company = new Company
        {
            Name = request.CompanyName,
            ContactEmail = request.ContactEmail,
            PhoneNumber = request.PhoneNumber,
            Website = request.Website,
            Description = request.Description,
            TaxId = request.TaxId,
            FaxNumber = request.FaxNumber,
            LicenseNumber = request.LicenseNumber,
            IsActive = true,
            IsVerified = true
        };

        _context.Companies.Add(company);
        await _context.SaveChangesAsync(); // Save to get the company ID

        // Create the user with company_admin role
        var user = new ApplicationUser
        {
            UserName = request.RequesterEmail,
            Email = request.RequesterEmail,
            EmailConfirmed = true,
            FullName = request.RequesterFullName,
            CompanyId = company.Id,
            IsActive = true
        };

        // Generate a temporary password (in production, send via email)
        var tempPassword = GenerateTemporaryPassword();
        var result = await _userManager.CreateAsync(user, tempPassword);

        if (!result.Succeeded)
        {
            // Rollback company creation if user creation fails
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to create user: {errors}");
        }

        // Assign company_admin role
        var roleResult = await _userManager.AddToRoleAsync(user, "company_admin");
        if (!roleResult.Succeeded)
        {
            // Rollback user and company creation if role assignment fails
            await _userManager.DeleteAsync(user);
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
            throw new BusinessRuleException($"Failed to assign role: {errors}");
        }

        // Update request status and link created entities
        request.Status = RequestStatusEnum.Processed;
        request.CreatedCompanyId = company.Id;
        Guid.TryParse(user.Id, out var GuidUserId);
        request.CreatedUserId = GuidUserId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company request approved: '{CompanyName}' (Request ID: {RequestId}, Company ID: {CompanyId}, User ID: {UserId})",
            request.CompanyName, requestId, company.Id, user.Id);

        // TODO: Send email notification with temporary password

        return request;
    }

    public async Task<CompanyRequest> RejectRequestAsync(Guid requestId, string rejectedByEmail, string? reason = null)
    {
        var request = await GetByIdAsync(requestId);
        if (request == null)
            throw new EntityNotFoundException(nameof(CompanyRequest), requestId);

        if (request.Status != RequestStatusEnum.Pending)
            throw new BusinessRuleException("Only pending requests can be rejected.");

        request.Status = RequestStatusEnum.Processed; // Using Processed for rejected as well
        request.RejectionReason = reason;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company request rejected: '{CompanyName}' (Request ID: {RequestId}) by {RejectedBy}",
            request.CompanyName, requestId, rejectedByEmail);

        // TODO: Send email notification about rejection

        return request;
    }

    public async Task<IEnumerable<CompanyRequest>> GetPendingRequestsAsync()
    {
        return await _context.CompanyRequests
            .Where(c => c.Status == RequestStatusEnum.Pending)
            .OrderByDescending(c => c.CreatedDate)
            .ToListAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.CompanyRequests.AnyAsync(c => c.Id == id);
    }

    private string GenerateTemporaryPassword()
    {
        // Generate a random password (in production, use a more secure method)
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}


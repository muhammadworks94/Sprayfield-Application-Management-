using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Company entity operations.
/// </summary>
public class CompanyService : ICompanyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(ApplicationDbContext context, ILogger<CompanyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Company>> GetAllAsync()
    {
        return await _context.Companies
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company?> GetByIdAsync(Guid id)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company> CreateAsync(Company company)
    {
        if (company == null)
            throw new ArgumentNullException(nameof(company));

        // Check if company with same name exists
        var exists = await _context.Companies
            .AnyAsync(c => c.Name.ToLower() == company.Name.ToLower());

        if (exists)
            throw new BusinessRuleException($"A company with the name '{company.Name}' already exists.");

        _context.Companies.Add(company);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company '{CompanyName}' created with ID {CompanyId}", company.Name, company.Id);
        return company;
    }

    public async Task<Company> UpdateAsync(Company company)
    {
        if (company == null)
            throw new ArgumentNullException(nameof(company));

        var existing = await GetByIdAsync(company.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Company), company.Id);

        // Check if another company with same name exists
        var duplicate = await _context.Companies
            .AnyAsync(c => c.Id != company.Id && c.Name.ToLower() == company.Name.ToLower());

        if (duplicate)
            throw new BusinessRuleException($"A company with the name '{company.Name}' already exists.");

        existing.Name = company.Name;
        existing.ContactEmail = company.ContactEmail;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Company '{CompanyName}' updated (ID: {CompanyId})", company.Name, company.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var company = await GetByIdAsync(id);
        if (company == null)
            throw new EntityNotFoundException(nameof(Company), id);

        // Soft delete
        company.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Company '{CompanyName}' soft-deleted (ID: {CompanyId})", company.Name, company.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Companies.AnyAsync(c => c.Id == id);
    }
}


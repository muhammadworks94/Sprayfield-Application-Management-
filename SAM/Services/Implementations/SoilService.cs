using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Soil entity operations.
/// </summary>
public class SoilService : ISoilService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SoilService> _logger;

    public SoilService(ApplicationDbContext context, ILogger<SoilService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Soil>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.Soils
            .Include(s => s.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(s => s.TypeName)
            .ToListAsync();
    }

    public async Task<Soil?> GetByIdAsync(Guid id)
    {
        return await _context.Soils
            .Include(s => s.Company)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Soil> CreateAsync(Soil soil)
    {
        if (soil == null)
            throw new ArgumentNullException(nameof(soil));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == soil.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), soil.CompanyId);

        _context.Soils.Add(soil);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soil '{SoilType}' created with ID {SoilId}", soil.TypeName, soil.Id);
        return soil;
    }

    public async Task<Soil> UpdateAsync(Soil soil)
    {
        if (soil == null)
            throw new ArgumentNullException(nameof(soil));

        var existing = await GetByIdAsync(soil.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Soil), soil.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == soil.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), soil.CompanyId);

        existing.TypeName = soil.TypeName;
        existing.Description = soil.Description;
        existing.Permeability = soil.Permeability;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Soil '{SoilType}' updated (ID: {SoilId})", soil.TypeName, soil.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var soil = await GetByIdAsync(id);
        if (soil == null)
            throw new EntityNotFoundException(nameof(Soil), id);

        // Check if soil is referenced by sprayfields
        var hasReferences = await _context.Sprayfields.AnyAsync(s => s.SoilId == id);

        if (hasReferences)
            throw new BusinessRuleException("Cannot delete soil because it is used by one or more sprayfields.");

        // Soft delete
        soil.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Soil '{SoilType}' soft-deleted (ID: {SoilId})", soil.TypeName, soil.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Soils.AnyAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Soil>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.Soils
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.TypeName)
            .ToListAsync();
    }
}


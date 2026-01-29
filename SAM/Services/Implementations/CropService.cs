using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Crop entity operations.
/// </summary>
public class CropService : ICropService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CropService> _logger;

    public CropService(ApplicationDbContext context, ILogger<CropService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Crop>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.Crops
            .Include(c => c.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(c => c.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Crop?> GetByIdAsync(Guid id)
    {
        return await _context.Crops
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Crop> CreateAsync(Crop crop)
    {
        if (crop == null)
            throw new ArgumentNullException(nameof(crop));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == crop.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), crop.CompanyId);

        _context.Crops.Add(crop);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Crop '{CropName}' created with ID {CropId}", crop.Name, crop.Id);
        return crop;
    }

    public async Task<Crop> UpdateAsync(Crop crop)
    {
        if (crop == null)
            throw new ArgumentNullException(nameof(crop));

        var existing = await GetByIdAsync(crop.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Crop), crop.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == crop.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), crop.CompanyId);

        existing.Name = crop.Name;
        existing.PanFactor = crop.PanFactor;
        existing.NUptake = crop.NUptake;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Crop '{CropName}' updated (ID: {CropId})", crop.Name, crop.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var crop = await GetByIdAsync(id);
        if (crop == null)
            throw new EntityNotFoundException(nameof(Crop), id);

        // Check if crop is referenced by sprayfields
        var hasReferences = await _context.Sprayfields.AnyAsync(s => s.CropId == id);

        if (hasReferences)
            throw new BusinessRuleException("Cannot delete crop because it is used by one or more sprayfields.");

        // Soft delete
        crop.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Crop '{CropName}' soft-deleted (ID: {CropId})", crop.Name, crop.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Crops.AnyAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Crop>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.Crops
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}



using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Nozzle entity operations.
/// </summary>
public class NozzleService : INozzleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NozzleService> _logger;

    public NozzleService(ApplicationDbContext context, ILogger<NozzleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Nozzle>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.Nozzles
            .Include(n => n.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(n => n.Manufacturer)
            .ThenBy(n => n.Model)
            .ToListAsync();
    }

    public async Task<Nozzle?> GetByIdAsync(Guid id)
    {
        return await _context.Nozzles
            .Include(n => n.Company)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<Nozzle> CreateAsync(Nozzle nozzle)
    {
        if (nozzle == null)
            throw new ArgumentNullException(nameof(nozzle));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == nozzle.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), nozzle.CompanyId);

        _context.Nozzles.Add(nozzle);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nozzle '{Manufacturer} {Model}' created with ID {NozzleId}", nozzle.Manufacturer, nozzle.Model, nozzle.Id);
        return nozzle;
    }

    public async Task<Nozzle> UpdateAsync(Nozzle nozzle)
    {
        if (nozzle == null)
            throw new ArgumentNullException(nameof(nozzle));

        var existing = await GetByIdAsync(nozzle.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Nozzle), nozzle.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == nozzle.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), nozzle.CompanyId);

        existing.Model = nozzle.Model;
        existing.Manufacturer = nozzle.Manufacturer;
        existing.FlowRateGpm = nozzle.FlowRateGpm;
        existing.SprayArc = nozzle.SprayArc;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Nozzle '{Manufacturer} {Model}' updated (ID: {NozzleId})", nozzle.Manufacturer, nozzle.Model, nozzle.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var nozzle = await GetByIdAsync(id);
        if (nozzle == null)
            throw new EntityNotFoundException(nameof(Nozzle), id);

        // Check if nozzle is referenced by sprayfields
        var hasReferences = await _context.Sprayfields.AnyAsync(s => s.NozzleId == id);

        if (hasReferences)
            throw new BusinessRuleException("Cannot delete nozzle because it is used by one or more sprayfields.");

        // Soft delete
        nozzle.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Nozzle '{Manufacturer} {Model}' soft-deleted (ID: {NozzleId})", nozzle.Manufacturer, nozzle.Model, nozzle.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Nozzles.AnyAsync(n => n.Id == id);
    }

    public async Task<IEnumerable<Nozzle>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.Nozzles
            .Where(n => n.CompanyId == companyId)
            .OrderBy(n => n.Manufacturer)
            .ThenBy(n => n.Model)
            .ToListAsync();
    }
}



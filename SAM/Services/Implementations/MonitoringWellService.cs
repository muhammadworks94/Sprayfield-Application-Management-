using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for MonitoringWell entity operations.
/// </summary>
public class MonitoringWellService : IMonitoringWellService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<MonitoringWellService> _logger;

    public MonitoringWellService(ApplicationDbContext context, ILogger<MonitoringWellService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<MonitoringWell>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.MonitoringWells
            .Include(m => m.Company)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(m => m.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(m => m.WellId)
            .ToListAsync();
    }

    public async Task<MonitoringWell?> GetByIdAsync(Guid id)
    {
        return await _context.MonitoringWells
            .Include(m => m.Company)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<MonitoringWell> CreateAsync(MonitoringWell monitoringWell)
    {
        if (monitoringWell == null)
            throw new ArgumentNullException(nameof(monitoringWell));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == monitoringWell.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), monitoringWell.CompanyId);

        // Check if WellId is unique for this company
        var wellIdExists = await _context.MonitoringWells
            .AnyAsync(m => m.CompanyId == monitoringWell.CompanyId && m.WellId == monitoringWell.WellId);
        if (wellIdExists)
            throw new BusinessRuleException($"A monitoring well with Well ID '{monitoringWell.WellId}' already exists for this company.");

        _context.MonitoringWells.Add(monitoringWell);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Monitoring well '{WellId}' created with ID {MonitoringWellId}", monitoringWell.WellId, monitoringWell.Id);
        return monitoringWell;
    }

    public async Task<MonitoringWell> UpdateAsync(MonitoringWell monitoringWell)
    {
        if (monitoringWell == null)
            throw new ArgumentNullException(nameof(monitoringWell));

        var existing = await GetByIdAsync(monitoringWell.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(MonitoringWell), monitoringWell.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == monitoringWell.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), monitoringWell.CompanyId);

        // Check if WellId is unique for this company (excluding current record)
        var wellIdExists = await _context.MonitoringWells
            .AnyAsync(m => m.Id != monitoringWell.Id && m.CompanyId == monitoringWell.CompanyId && m.WellId == monitoringWell.WellId);
        if (wellIdExists)
            throw new BusinessRuleException($"A monitoring well with Well ID '{monitoringWell.WellId}' already exists for this company.");

        existing.WellId = monitoringWell.WellId;
        existing.LocationDescription = monitoringWell.LocationDescription;
        existing.Latitude = monitoringWell.Latitude;
        existing.Longitude = monitoringWell.Longitude;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Monitoring well '{WellId}' updated (ID: {MonitoringWellId})", monitoringWell.WellId, monitoringWell.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var monitoringWell = await GetByIdAsync(id);
        if (monitoringWell == null)
            throw new EntityNotFoundException(nameof(MonitoringWell), id);

        // Check if monitoring well is referenced
        var hasReferences = await _context.GWMonits.AnyAsync(g => g.MonitoringWellId == id);

        if (hasReferences)
            throw new BusinessRuleException("Cannot delete monitoring well because it has associated groundwater monitoring records.");

        // Soft delete
        monitoringWell.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Monitoring well '{WellId}' soft-deleted (ID: {MonitoringWellId})", monitoringWell.WellId, monitoringWell.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.MonitoringWells.AnyAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<MonitoringWell>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.MonitoringWells
            .Where(m => m.CompanyId == companyId)
            .OrderBy(m => m.WellId)
            .ToListAsync();
    }
}


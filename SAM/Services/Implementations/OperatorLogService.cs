using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for OperatorLog entity operations.
/// </summary>
public class OperatorLogService : IOperatorLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OperatorLogService> _logger;

    public OperatorLogService(ApplicationDbContext context, ILogger<OperatorLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<OperatorLog>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var query = _context.OperatorLogs
            .Include(o => o.Company)
            .Include(o => o.Facility)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(o => o.FacilityId == facilityId.Value);
        }

        return await query
            .OrderByDescending(o => o.LogDate)
            .ThenByDescending(o => o.CreatedDate)
            .ToListAsync();
    }

    public async Task<OperatorLog?> GetByIdAsync(Guid id)
    {
        return await _context.OperatorLogs
            .Include(o => o.Company)
            .Include(o => o.Facility)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<OperatorLog> CreateAsync(OperatorLog operatorLog)
    {
        if (operatorLog == null)
            throw new ArgumentNullException(nameof(operatorLog));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == operatorLog.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), operatorLog.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == operatorLog.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), operatorLog.FacilityId);
        if (facility.CompanyId != operatorLog.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        _context.OperatorLogs.Add(operatorLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Operator log created for facility '{FacilityName}' on {LogDate} (ID: {LogId})", 
            facility.Name, operatorLog.LogDate, operatorLog.Id);
        return operatorLog;
    }

    public async Task<OperatorLog> UpdateAsync(OperatorLog operatorLog)
    {
        if (operatorLog == null)
            throw new ArgumentNullException(nameof(operatorLog));

        var existing = await GetByIdAsync(operatorLog.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(OperatorLog), operatorLog.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == operatorLog.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), operatorLog.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == operatorLog.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), operatorLog.FacilityId);
        if (facility.CompanyId != operatorLog.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        existing.LogDate = operatorLog.LogDate;
        existing.OperatorName = operatorLog.OperatorName;
        existing.Shift = operatorLog.Shift;
        existing.WeatherConditions = operatorLog.WeatherConditions;
        existing.SystemStatus = operatorLog.SystemStatus;
        existing.MaintenancePerformed = operatorLog.MaintenancePerformed;
        existing.EquipmentInspected = operatorLog.EquipmentInspected;
        existing.IssuesNoted = operatorLog.IssuesNoted;
        existing.CorrectiveActions = operatorLog.CorrectiveActions;
        existing.NextShiftNotes = operatorLog.NextShiftNotes;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Operator log updated (ID: {LogId})", operatorLog.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var operatorLog = await GetByIdAsync(id);
        if (operatorLog == null)
            throw new EntityNotFoundException(nameof(OperatorLog), id);

        // Soft delete
        operatorLog.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Operator log soft-deleted (ID: {LogId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.OperatorLogs.AnyAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<OperatorLog>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.OperatorLogs
            .Where(o => o.FacilityId == facilityId)
            .OrderByDescending(o => o.LogDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<OperatorLog>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate)
    {
        var query = _context.OperatorLogs.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        return await query
            .Where(o => o.LogDate >= startDate && o.LogDate <= endDate)
            .OrderByDescending(o => o.LogDate)
            .ToListAsync();
    }
}


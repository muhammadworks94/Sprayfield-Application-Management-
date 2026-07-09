using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for OperatorLog entity operations.
/// </summary>
public class OperatorLogService : IOperatorLogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OperatorLogService> _logger;
    private readonly INDAR1Service _ndar1Service;
    private readonly IMonthlyReportProvisionerService _reportProvisioner;

    public OperatorLogService(
        ApplicationDbContext context,
        ILogger<OperatorLogService> logger,
        INDAR1Service ndar1Service,
        IMonthlyReportProvisionerService reportProvisioner)
    {
        _context = context;
        _logger = logger;
        _ndar1Service = ndar1Service;
        _reportProvisioner = reportProvisioner;
    }

    public async Task<IEnumerable<OperatorLog>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var query = _context.OperatorLogs
            .Include(o => o.Company)
            .Include(o => o.Facility)
            .AsNoTracking()
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
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Guid> GetCompanyIdAsync(Guid id)
    {
        var companyId = await _context.OperatorLogs
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => (Guid?)o.CompanyId)
            .FirstOrDefaultAsync();

        if (!companyId.HasValue)
        {
            throw new EntityNotFoundException(nameof(OperatorLog), id);
        }

        return companyId.Value;
    }

    public async Task<OperatorLog> CreateAsync(OperatorLog operatorLog)
    {
        var result = await CreateWithNdarRefreshAsync(operatorLog);
        return result.OperatorLog;
    }

    public async Task<OperatorLogMutationResult> CreateWithNdarRefreshAsync(OperatorLog operatorLog)
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
        var outcome = await SyncOperatorLogMonthReportsAsync(operatorLog.FacilityId, operatorLog.LogDate);

        _logger.LogInformation("Operator log created for facility '{FacilityName}' on {LogDate} (ID: {LogId})",
            facility.Name, operatorLog.LogDate, operatorLog.Id);
        return new OperatorLogMutationResult
        {
            OperatorLog = operatorLog,
            NdarRefreshOutcomes = new List<NdarRefreshOutcome> { outcome }
        };
    }

    public async Task<OperatorLog> UpdateAsync(OperatorLog operatorLog)
    {
        var result = await UpdateWithNdarRefreshAsync(operatorLog);
        return result.OperatorLog;
    }

    public async Task<OperatorLogMutationResult> UpdateWithNdarRefreshAsync(OperatorLog operatorLog)
    {
        if (operatorLog == null)
            throw new ArgumentNullException(nameof(operatorLog));

        var existing = await _context.OperatorLogs
            .FirstOrDefaultAsync(o => o.Id == operatorLog.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(OperatorLog), operatorLog.Id);
        var oldFacilityId = existing.FacilityId;
        var oldLogDate = existing.LogDate;

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
        existing.WeatherConditions = operatorLog.WeatherConditions;
        existing.TemperatureF = operatorLog.TemperatureF;
        existing.PrecipitationIn = operatorLog.PrecipitationIn;
        existing.ORCOnSite = operatorLog.ORCOnSite;
        existing.WaterDepthFt = operatorLog.WaterDepthFt;
        existing.StorageFt = operatorLog.StorageFt;
        existing.FiveDayUpsetFt = operatorLog.FiveDayUpsetFt;
        existing.ArrivalTime = operatorLog.ArrivalTime;
        existing.TimeOnSiteHours = operatorLog.TimeOnSiteHours;
        existing.MaintenancePerformed = operatorLog.MaintenancePerformed;
        existing.EquipmentInspected = operatorLog.EquipmentInspected;
        existing.IssuesNoted = operatorLog.IssuesNoted;
        existing.CorrectiveActions = operatorLog.CorrectiveActions;
        existing.NextShiftNotes = operatorLog.NextShiftNotes;

        await _context.SaveChangesAsync();
        var outcomes = new List<NdarRefreshOutcome>
        {
            await SyncOperatorLogMonthReportsAsync(oldFacilityId, oldLogDate)
        };

        if (oldFacilityId != existing.FacilityId ||
            oldLogDate.Month != existing.LogDate.Month ||
            oldLogDate.Year != existing.LogDate.Year)
        {
            outcomes.Add(await SyncOperatorLogMonthReportsAsync(existing.FacilityId, existing.LogDate));
        }

        _logger.LogInformation("Operator log updated (ID: {LogId})", operatorLog.Id);
        return new OperatorLogMutationResult
        {
            OperatorLog = existing,
            NdarRefreshOutcomes = outcomes
        };
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await DeleteWithNdarRefreshAsync(id);
        return result.Deleted;
    }

    public async Task<(bool Deleted, Guid CompanyId, List<NdarRefreshOutcome> NdarRefreshOutcomes)> DeleteWithNdarRefreshAsync(Guid id)
    {
        var operatorLog = await _context.OperatorLogs
            .FirstOrDefaultAsync(o => o.Id == id);
        if (operatorLog == null)
            throw new EntityNotFoundException(nameof(OperatorLog), id);

        var companyId = operatorLog.CompanyId;

        // Soft delete
        operatorLog.IsDeleted = true;
        await _context.SaveChangesAsync();
        var outcome = await RefreshWeatherNdar1ForMonthAsync(operatorLog.FacilityId, operatorLog.LogDate);

        _logger.LogInformation("Operator log soft-deleted (ID: {LogId})", id);
        return (true, companyId, new List<NdarRefreshOutcome> { outcome });
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.OperatorLogs.AnyAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<OperatorLog>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.OperatorLogs
            .AsNoTracking()
            .Where(o => o.FacilityId == facilityId)
            .OrderByDescending(o => o.LogDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<OperatorLog>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate)
    {
        var query = _context.OperatorLogs
            .AsNoTracking()
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(o => o.CompanyId == companyId.Value);
        }

        return await query
            .Where(o => o.LogDate >= startDate && o.LogDate <= endDate)
            .OrderByDescending(o => o.LogDate)
            .ToListAsync();
    }

    private async Task<NdarRefreshOutcome> SyncOperatorLogMonthReportsAsync(Guid facilityId, DateTime date)
    {
        var existingReport = await _ndar1Service.GetByFacilityMonthYearAsync(facilityId, date.Month, date.Year);
        NdarRefreshOutcome outcome;
        try
        {
            outcome = existingReport != null
                ? await _ndar1Service.RefreshWeatherSnapshotForMonthAsync(facilityId, date.Month, date.Year)
                : await _reportProvisioner.EnsureNdar1ForMonthAsync(facilityId, date.Month, date.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "NDAR-1 sync failed after operator log change for facility {FacilityId} month {Month} year {Year}.",
                facilityId, date.Month, date.Year);
            return new NdarRefreshOutcome
            {
                FacilityId = facilityId,
                Month = date.Month,
                Year = date.Year,
                Status = NdarRefreshStatus.Failed,
                Message = $"NDAR-1 sync for {new DateTime(date.Year, date.Month, 1):MMM yyyy} failed."
            };
        }

        try
        {
            await _reportProvisioner.EnsureNdmrForMonthAsync(facilityId, date.Month, date.Year);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "NDMR auto-provision failed after operator log change for facility {FacilityId} month {Month} year {Year}.",
                facilityId, date.Month, date.Year);
            outcome.SecondaryWarnings.Add(
                $"NDMR for {new DateTime(date.Year, date.Month, 1):MMM yyyy} could not be auto-created.");
        }

        return outcome;
    }

    private async Task<NdarRefreshOutcome> RefreshWeatherNdar1ForMonthAsync(Guid facilityId, DateTime date)
    {
        try
        {
            return await _ndar1Service.RefreshWeatherSnapshotForMonthAsync(facilityId, date.Month, date.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "NDAR-1 weather refresh failed after operator log change for facility {FacilityId} month {Month} year {Year}.",
                facilityId, date.Month, date.Year);
            return new NdarRefreshOutcome
            {
                FacilityId = facilityId,
                Month = date.Month,
                Year = date.Year,
                Status = NdarRefreshStatus.Failed,
                Message = $"NDAR-1 refresh for {new DateTime(date.Year, date.Month, 1):MMM yyyy} failed."
            };
        }
    }
}


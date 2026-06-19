using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for WWChar (Wastewater Characteristics) entity operations.
/// </summary>
public class WWCharService : IWWCharService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WWCharService> _logger;

    public WWCharService(ApplicationDbContext context, ILogger<WWCharService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<WWChar>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var query = _context.WWChars
            .Include(w => w.Company)
            .Include(w => w.Facility)
            .Include(w => w.FacilityPermit)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(w => w.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(w => w.FacilityId == facilityId.Value);
        }

        return await query
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.Month)
            .ToListAsync();
    }

    public async Task<WWChar?> GetByIdAsync(Guid id)
    {
        return await _context.WWChars
            .Include(w => w.Company)
            .Include(w => w.Facility)
            .Include(w => w.FacilityPermit)
            .FirstOrDefaultAsync(w => w.Id == id);
    }

    public async Task<WWChar> CreateAsync(WWChar wwChar)
    {
        if (wwChar == null)
            throw new ArgumentNullException(nameof(wwChar));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == wwChar.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), wwChar.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == wwChar.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), wwChar.FacilityId);
        if (facility.CompanyId != wwChar.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Validate daily arrays don't exceed 31 items
        ValidateDailyArray(wwChar.BOD5Daily, "BOD5Daily");
        ValidateDailyArray(wwChar.TSSDaily, "TSSDaily");
        ValidateDailyArray(wwChar.FlowRateDaily, "FlowRateDaily");
        ValidateDailyArray(wwChar.PHDaily, "PHDaily");
        ValidateDailyArray(wwChar.NH3NDaily, "NH3NDaily");
        ValidateDailyArray(wwChar.FecalColiformDaily, "FecalColiformDaily");
        ValidateDailyArray(wwChar.ChlorideDaily, "ChlorideDaily");
        ValidateDailyArray(wwChar.CaDaily, "CaDaily");
        ValidateDailyArray(wwChar.MgDaily, "MgDaily");
        ValidateDailyArray(wwChar.NaDaily, "NaDaily");
        ValidateDailyArray(wwChar.SARDaily, "SARDaily");
        ValidateDailyArray(wwChar.TNDaily, "TNDaily");
        ValidateDailyArray(wwChar.CompositeTime, "CompositeTime");

        // Query including soft-deleted rows because the unique index also includes them.
        var existingAnyState = await _context.WWChars
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.FacilityId == wwChar.FacilityId && w.Month == wwChar.Month && w.Year == wwChar.Year);

        if (existingAnyState != null)
        {
            if (!existingAnyState.IsDeleted)
            {
                throw new BusinessRuleException($"A wastewater characteristics record already exists for facility {facility.Name} for {wwChar.Month} {wwChar.Year}.");
            }

            // Restore soft-deleted record by updating it with the new payload.
            existingAnyState.IsDeleted = false;
            existingAnyState.CompanyId = wwChar.CompanyId;
            existingAnyState.FacilityId = wwChar.FacilityId;
            existingAnyState.Month = wwChar.Month;
            existingAnyState.Year = wwChar.Year;
            existingAnyState.BOD5Daily = wwChar.BOD5Daily;
            existingAnyState.TSSDaily = wwChar.TSSDaily;
            existingAnyState.FlowRateDaily = wwChar.FlowRateDaily;
            existingAnyState.PHDaily = wwChar.PHDaily;
            existingAnyState.NH3NDaily = wwChar.NH3NDaily;
            existingAnyState.FecalColiformDaily = wwChar.FecalColiformDaily;
            existingAnyState.ChlorideDaily = wwChar.ChlorideDaily;
            existingAnyState.CaDaily = wwChar.CaDaily;
            existingAnyState.MgDaily = wwChar.MgDaily;
            existingAnyState.NaDaily = wwChar.NaDaily;
            existingAnyState.SARDaily = wwChar.SARDaily;
            existingAnyState.TNDaily = wwChar.TNDaily;
            existingAnyState.CompositeTime = wwChar.CompositeTime;
            existingAnyState.LabOptionId = wwChar.LabOptionId;
            existingAnyState.CollectedBy = wwChar.CollectedBy;
            existingAnyState.AnalyzedBy = wwChar.AnalyzedBy;
            existingAnyState.NO2N = wwChar.NO2N;
            existingAnyState.TKNN = wwChar.TKNN;
            existingAnyState.NO3N = wwChar.NO3N;
            existingAnyState.FlowMeasuringPoint = wwChar.FlowMeasuringPoint;
            existingAnyState.ParameterMonitoringPoint = wwChar.ParameterMonitoringPoint;
            existingAnyState.FacilityPermitId = wwChar.FacilityPermitId;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Soft-deleted wastewater characteristics record restored for facility '{FacilityName}' for {Month} {Year} (ID: {WWCharId})",
                facility.Name, existingAnyState.Month, existingAnyState.Year, existingAnyState.Id);
            return existingAnyState;
        }

        try
        {
            _context.WWChars.Add(wwChar);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            throw new BusinessRuleException($"A wastewater characteristics record already exists for facility {facility.Name} for {wwChar.Month} {wwChar.Year}.");
        }

        _logger.LogInformation("Wastewater characteristics record created for facility '{FacilityName}' for {Month} {Year} (ID: {WWCharId})",
            facility.Name, wwChar.Month, wwChar.Year, wwChar.Id);
        return wwChar;
    }

    public async Task<WWChar> UpdateAsync(WWChar wwChar)
    {
        if (wwChar == null)
            throw new ArgumentNullException(nameof(wwChar));

        var existing = await GetByIdAsync(wwChar.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(WWChar), wwChar.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == wwChar.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), wwChar.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == wwChar.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), wwChar.FacilityId);
        if (facility.CompanyId != wwChar.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if another record exists for this facility/month/year (excluding current)
        var duplicate = await _context.WWChars
            .FirstOrDefaultAsync(w => w.Id != wwChar.Id && w.FacilityId == wwChar.FacilityId && w.Month == wwChar.Month && w.Year == wwChar.Year);
        if (duplicate != null)
            throw new BusinessRuleException($"A wastewater characteristics record already exists for facility {facility.Name} for {wwChar.Month} {wwChar.Year}.");

        // Validate daily arrays don't exceed 31 items
        ValidateDailyArray(wwChar.BOD5Daily, "BOD5Daily");
        ValidateDailyArray(wwChar.TSSDaily, "TSSDaily");
        ValidateDailyArray(wwChar.FlowRateDaily, "FlowRateDaily");
        ValidateDailyArray(wwChar.PHDaily, "PHDaily");
        ValidateDailyArray(wwChar.NH3NDaily, "NH3NDaily");
        ValidateDailyArray(wwChar.FecalColiformDaily, "FecalColiformDaily");
        ValidateDailyArray(wwChar.ChlorideDaily, "ChlorideDaily");
        ValidateDailyArray(wwChar.CaDaily, "CaDaily");
        ValidateDailyArray(wwChar.MgDaily, "MgDaily");
        ValidateDailyArray(wwChar.NaDaily, "NaDaily");
        ValidateDailyArray(wwChar.SARDaily, "SARDaily");
        ValidateDailyArray(wwChar.TNDaily, "TNDaily");
        ValidateDailyArray(wwChar.CompositeTime, "CompositeTime");

        existing.Month = wwChar.Month;
        existing.Year = wwChar.Year;
        existing.BOD5Daily = wwChar.BOD5Daily;
        existing.TSSDaily = wwChar.TSSDaily;
        existing.FlowRateDaily = wwChar.FlowRateDaily;
        existing.PHDaily = wwChar.PHDaily;
        existing.NH3NDaily = wwChar.NH3NDaily;
        existing.FecalColiformDaily = wwChar.FecalColiformDaily;
        existing.ChlorideDaily = wwChar.ChlorideDaily;
        existing.CaDaily = wwChar.CaDaily;
        existing.MgDaily = wwChar.MgDaily;
        existing.NaDaily = wwChar.NaDaily;
        existing.SARDaily = wwChar.SARDaily;
        existing.TNDaily = wwChar.TNDaily;
        existing.CompositeTime = wwChar.CompositeTime;
        existing.LabOptionId = wwChar.LabOptionId;
        existing.CollectedBy = wwChar.CollectedBy;
        existing.AnalyzedBy = wwChar.AnalyzedBy;
        existing.NO2N = wwChar.NO2N;
        existing.TKNN = wwChar.TKNN;
        existing.NO3N = wwChar.NO3N;
        existing.FlowMeasuringPoint = wwChar.FlowMeasuringPoint;
        existing.ParameterMonitoringPoint = wwChar.ParameterMonitoringPoint;
        existing.FacilityPermitId = wwChar.FacilityPermitId;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Wastewater characteristics record updated (ID: {WWCharId})", wwChar.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var wwChar = await GetByIdAsync(id);
        if (wwChar == null)
            throw new EntityNotFoundException(nameof(WWChar), id);

        // Soft delete
        wwChar.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Wastewater characteristics record soft-deleted (ID: {WWCharId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.WWChars.AnyAsync(w => w.Id == id);
    }

    public async Task<WWChar?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year)
    {
        return await _context.WWChars
            .FirstOrDefaultAsync(w => w.FacilityId == facilityId && (int)w.Month == month && w.Year == year);
    }

    public async Task<IEnumerable<WWChar>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.WWChars
            .Where(w => w.FacilityId == facilityId)
            .OrderByDescending(w => w.Year)
            .ThenByDescending(w => w.Month)
            .ToListAsync();
    }

    private void ValidateDailyArray<T>(List<T>? array, string fieldName)
    {
        if (array != null && array.Count > 31)
        {
            throw new BusinessRuleException($"{fieldName} cannot exceed 31 items (one per day).");
        }
    }
}


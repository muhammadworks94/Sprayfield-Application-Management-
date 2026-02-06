using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Irrigate (Irrigation Log) entity operations.
/// </summary>
public class IrrigateService : IIrrigateService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IrrigateService> _logger;

    public IrrigateService(ApplicationDbContext context, ILogger<IrrigateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Irrigate>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? sprayfieldId = null)
    {
        var query = _context.Irrigates
            .Include(i => i.Company)
            .Include(i => i.Facility)
            .Include(i => i.Sprayfield)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(i => i.FacilityId == facilityId.Value);
        }

        if (sprayfieldId.HasValue)
        {
            query = query.Where(i => i.SprayfieldId == sprayfieldId.Value);
        }

        return await query
            .OrderByDescending(i => i.IrrigationDate)
            .ThenByDescending(i => i.StartTime)
            .ToListAsync();
    }

    public async Task<Irrigate?> GetByIdAsync(Guid id)
    {
        return await _context.Irrigates
            .Include(i => i.Company)
            .Include(i => i.Facility)
            .Include(i => i.Sprayfield)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<Irrigate> CreateAsync(Irrigate irrigate)
    {
        if (irrigate == null)
            throw new ArgumentNullException(nameof(irrigate));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == irrigate.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), irrigate.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == irrigate.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), irrigate.FacilityId);
        if (facility.CompanyId != irrigate.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Validate sprayfield exists and belongs to same company
        var sprayfield = await _context.Sprayfields.FirstOrDefaultAsync(s => s.Id == irrigate.SprayfieldId);
        if (sprayfield == null)
            throw new EntityNotFoundException(nameof(Sprayfield), irrigate.SprayfieldId);
        if (sprayfield.CompanyId != irrigate.CompanyId)
            throw new BusinessRuleException("Sprayfield must belong to the same company.");

        // Auto-calculate duration if not provided (default is 0)
        if (irrigate.DurationHours == 0)
        {
            var duration = irrigate.EndTime - irrigate.StartTime;
            if (duration.TotalHours < 0)
            {
                duration = duration.Add(TimeSpan.FromDays(1)); // Handle overnight irrigation
            }
            irrigate.DurationHours = (decimal)duration.TotalHours;
        }

        // Auto-calculate total volume if flow rate and duration are available
        if (irrigate.TotalVolumeGallons == 0 && irrigate.FlowRateGpm > 0 && irrigate.DurationHours > 0)
        {
            irrigate.TotalVolumeGallons = irrigate.FlowRateGpm * irrigate.DurationHours * 60;
        }

        _context.Irrigates.Add(irrigate);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Irrigation log created for sprayfield '{SprayfieldId}' on {IrrigationDate} (ID: {IrrigateId})", 
            sprayfield.FieldId, irrigate.IrrigationDate, irrigate.Id);
        return irrigate;
    }

    public async Task<Irrigate> UpdateAsync(Irrigate irrigate)
    {
        if (irrigate == null)
            throw new ArgumentNullException(nameof(irrigate));

        var existing = await GetByIdAsync(irrigate.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Irrigate), irrigate.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == irrigate.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), irrigate.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == irrigate.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), irrigate.FacilityId);
        if (facility.CompanyId != irrigate.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Validate sprayfield exists and belongs to same company
        var sprayfield = await _context.Sprayfields.FirstOrDefaultAsync(s => s.Id == irrigate.SprayfieldId);
        if (sprayfield == null)
            throw new EntityNotFoundException(nameof(Sprayfield), irrigate.SprayfieldId);
        if (sprayfield.CompanyId != irrigate.CompanyId)
            throw new BusinessRuleException("Sprayfield must belong to the same company.");

        // Auto-calculate duration if not provided (default is 0)
        if (irrigate.DurationHours == 0)
        {
            var duration = irrigate.EndTime - irrigate.StartTime;
            if (duration.TotalHours < 0)
            {
                duration = duration.Add(TimeSpan.FromDays(1));
            }
            irrigate.DurationHours = (decimal)duration.TotalHours;
        }

        // Auto-calculate total volume if flow rate and duration are available
        if (irrigate.TotalVolumeGallons == 0 && irrigate.FlowRateGpm > 0 && irrigate.DurationHours > 0)
        {
            irrigate.TotalVolumeGallons = irrigate.FlowRateGpm * irrigate.DurationHours * 60;
        }

        existing.IrrigationDate = irrigate.IrrigationDate;
        existing.StartTime = irrigate.StartTime;
        existing.EndTime = irrigate.EndTime;
        existing.DurationHours = irrigate.DurationHours;
        existing.FlowRateGpm = irrigate.FlowRateGpm;
        existing.TotalVolumeGallons = irrigate.TotalVolumeGallons;
        existing.ApplicationRateInches = irrigate.ApplicationRateInches;
        existing.TemperatureF = irrigate.TemperatureF;
        existing.PrecipitationIn = irrigate.PrecipitationIn;
        existing.WeatherConditions = irrigate.WeatherConditions;
        existing.Comments = irrigate.Comments;
        existing.ModifiedBy = irrigate.ModifiedBy;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Irrigation log updated (ID: {IrrigateId})", irrigate.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var irrigate = await GetByIdAsync(id);
        if (irrigate == null)
            throw new EntityNotFoundException(nameof(Irrigate), id);

        // Soft delete
        irrigate.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Irrigation log soft-deleted (ID: {IrrigateId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Irrigates.AnyAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Irrigate>> GetBySprayfieldIdAsync(Guid sprayfieldId)
    {
        return await _context.Irrigates
            .Where(i => i.SprayfieldId == sprayfieldId)
            .OrderByDescending(i => i.IrrigationDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Irrigate>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate)
    {
        var query = _context.Irrigates.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(i => i.CompanyId == companyId.Value);
        }

        return await query
            .Where(i => i.IrrigationDate >= startDate && i.IrrigationDate <= endDate)
            .OrderByDescending(i => i.IrrigationDate)
            .ToListAsync();
    }
}


using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Sprayfield entity operations.
/// </summary>
public class SprayfieldService : ISprayfieldService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SprayfieldService> _logger;

    public SprayfieldService(ApplicationDbContext context, ILogger<SprayfieldService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Sprayfield>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.Sprayfields
            .Include(s => s.Company)
            .Include(s => s.Soil)
            .Include(s => s.Crop)
            .Include(s => s.Nozzle)
            .Include(s => s.Facility)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(s => s.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(s => s.FieldId)
            .ToListAsync();
    }

    public async Task<Sprayfield?> GetByIdAsync(Guid id)
    {
        return await _context.Sprayfields
            .Include(s => s.Company)
            .Include(s => s.Soil)
            .Include(s => s.Crop)
            .Include(s => s.Nozzle)
            .Include(s => s.Facility)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<Sprayfield> CreateAsync(Sprayfield sprayfield)
    {
        if (sprayfield == null)
            throw new ArgumentNullException(nameof(sprayfield));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == sprayfield.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), sprayfield.CompanyId);

        // Validate facility exists and belongs to same company
        if (sprayfield.FacilityId.HasValue)
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == sprayfield.FacilityId.Value);
            if (facility == null)
                throw new EntityNotFoundException(nameof(Facility), sprayfield.FacilityId.Value);
            if (facility.CompanyId != sprayfield.CompanyId)
                throw new BusinessRuleException("Facility must belong to the same company as the sprayfield.");
        }

        // Validate soil exists and belongs to same company
        var soil = await _context.Soils.FirstOrDefaultAsync(s => s.Id == sprayfield.SoilId);
        if (soil == null)
            throw new EntityNotFoundException(nameof(Soil), sprayfield.SoilId);
        if (soil.CompanyId != sprayfield.CompanyId)
            throw new BusinessRuleException("Soil must belong to the same company as the sprayfield.");

        // Validate crop exists and belongs to same company
        var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == sprayfield.CropId);
        if (crop == null)
            throw new EntityNotFoundException(nameof(Crop), sprayfield.CropId);
        if (crop.CompanyId != sprayfield.CompanyId)
            throw new BusinessRuleException("Crop must belong to the same company as the sprayfield.");

        // Validate nozzle exists and belongs to same company
        var nozzle = await _context.Nozzles.FirstOrDefaultAsync(n => n.Id == sprayfield.NozzleId);
        if (nozzle == null)
            throw new EntityNotFoundException(nameof(Nozzle), sprayfield.NozzleId);
        if (nozzle.CompanyId != sprayfield.CompanyId)
            throw new BusinessRuleException("Nozzle must belong to the same company as the sprayfield.");

        // Check if FieldId is unique for this company
        var fieldIdExists = await _context.Sprayfields
            .AnyAsync(s => s.CompanyId == sprayfield.CompanyId && s.FieldId == sprayfield.FieldId);
        if (fieldIdExists)
            throw new BusinessRuleException($"A sprayfield with Field ID '{sprayfield.FieldId}' already exists for this company.");

        _context.Sprayfields.Add(sprayfield);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sprayfield '{FieldId}' created with ID {SprayfieldId}", sprayfield.FieldId, sprayfield.Id);
        return sprayfield;
    }

    public async Task<Sprayfield> UpdateAsync(Sprayfield sprayfield)
    {
        if (sprayfield == null)
            throw new ArgumentNullException(nameof(sprayfield));

        var existing = await GetByIdAsync(sprayfield.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Sprayfield), sprayfield.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == sprayfield.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), sprayfield.CompanyId);

        // Validate facility exists and belongs to same company
        if (sprayfield.FacilityId.HasValue)
        {
            var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == sprayfield.FacilityId.Value);
            if (facility == null)
                throw new EntityNotFoundException(nameof(Facility), sprayfield.FacilityId.Value);
            if (facility.CompanyId != sprayfield.CompanyId)
                throw new BusinessRuleException("Facility must belong to the same company as the sprayfield.");
        }

        // Validate soil exists and belongs to same company
        var soil = await _context.Soils.FirstOrDefaultAsync(s => s.Id == sprayfield.SoilId);
        if (soil == null)
            throw new EntityNotFoundException(nameof(Soil), sprayfield.SoilId);
        if (soil.CompanyId != sprayfield.CompanyId)
            throw new BusinessRuleException("Soil must belong to the same company as the sprayfield.");

        // Validate crop exists and belongs to same company
        var crop = await _context.Crops.FirstOrDefaultAsync(c => c.Id == sprayfield.CropId);
        if (crop == null)
            throw new EntityNotFoundException(nameof(Crop), sprayfield.CropId);
        if (crop.CompanyId != sprayfield.CompanyId)
            throw new BusinessRuleException("Crop must belong to the same company as the sprayfield.");

        // Validate nozzle exists and belongs to same company
        var nozzle = await _context.Nozzles.FirstOrDefaultAsync(n => n.Id == sprayfield.NozzleId);
        if (nozzle == null)
            throw new EntityNotFoundException(nameof(Nozzle), sprayfield.NozzleId);
        if (nozzle.CompanyId != sprayfield.CompanyId)
            throw new BusinessRuleException("Nozzle must belong to the same company as the sprayfield.");

        // Check if FieldId is unique for this company (excluding current record)
        var fieldIdExists = await _context.Sprayfields
            .AnyAsync(s => s.Id != sprayfield.Id && s.CompanyId == sprayfield.CompanyId && s.FieldId == sprayfield.FieldId);
        if (fieldIdExists)
            throw new BusinessRuleException($"A sprayfield with Field ID '{sprayfield.FieldId}' already exists for this company.");

        existing.FieldId = sprayfield.FieldId;
        existing.SizeAcres = sprayfield.SizeAcres;
        existing.SoilId = sprayfield.SoilId;
        existing.CropId = sprayfield.CropId;
        existing.NozzleId = sprayfield.NozzleId;
        existing.FacilityId = sprayfield.FacilityId;
        existing.HydraulicLoadingLimitInPerYr = sprayfield.HydraulicLoadingLimitInPerYr;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Sprayfield '{FieldId}' updated (ID: {SprayfieldId})", sprayfield.FieldId, sprayfield.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var sprayfield = await GetByIdAsync(id);
        if (sprayfield == null)
            throw new EntityNotFoundException(nameof(Sprayfield), id);

        // Check if sprayfield is referenced
        var hasReferences = await _context.Irrigates.AnyAsync(i => i.SprayfieldId == id);

        if (hasReferences)
            throw new BusinessRuleException("Cannot delete sprayfield because it has associated irrigation records.");

        // Soft delete
        sprayfield.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Sprayfield '{FieldId}' soft-deleted (ID: {SprayfieldId})", sprayfield.FieldId, sprayfield.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Sprayfields.AnyAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Sprayfield>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.Sprayfields
            .Where(s => s.CompanyId == companyId)
            .OrderBy(s => s.FieldId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Sprayfield>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.Sprayfields
            .Where(s => s.FacilityId == facilityId)
            .OrderBy(s => s.FieldId)
            .ToListAsync();
    }
}



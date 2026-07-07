using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for Facility entity operations.
/// </summary>
public class FacilityService : IFacilityService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FacilityService> _logger;

    public FacilityService(ApplicationDbContext context, ILogger<FacilityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Facility>> GetAllAsync(Guid? companyId = null)
    {
        var query = _context.Facilities
            .Include(f => f.Company)
            .AsNoTracking()
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(f => f.CompanyId == companyId.Value);
        }

        return await query
            .OrderBy(f => f.Name)
            .ToListAsync();
    }

    public async Task<Facility?> GetByIdAsync(Guid id)
    {
        return await _context.Facilities
            .Include(f => f.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<Facility> CreateAsync(Facility facility)
    {
        if (facility == null)
            throw new ArgumentNullException(nameof(facility));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == facility.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), facility.CompanyId);

        _context.Facilities.Add(facility);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Facility '{FacilityName}' created with ID {FacilityId}", facility.Name, facility.Id);
        return facility;
    }

    public async Task<Facility> UpdateAsync(Facility facility)
    {
        if (facility == null)
            throw new ArgumentNullException(nameof(facility));

        var existing = await _context.Facilities
            .FirstOrDefaultAsync(f => f.Id == facility.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(Facility), facility.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == facility.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), facility.CompanyId);

        existing.Name = facility.Name;
        existing.Permittee = facility.Permittee;
        existing.FacilityClass = facility.FacilityClass;
        existing.PermitPhone = facility.PermitPhone;
        existing.FacilityPhone = facility.FacilityPhone;
        existing.FacilityContactPerson = facility.FacilityContactPerson;
        existing.FacilityContactPersonPhone = facility.FacilityContactPersonPhone;
        existing.OrcName = facility.OrcName;
        existing.OperatorGrade = facility.OperatorGrade;
        existing.OperatorNumber = facility.OperatorNumber;
        existing.OperatorPhone = facility.OperatorPhone;
        existing.ChangeInOrc = facility.ChangeInOrc;
        existing.PersonsCollectingSamples = facility.PersonsCollectingSamples;
        existing.MineralizationRatePercent = facility.MineralizationRatePercent;
        existing.VolatilizationRatePercent = facility.VolatilizationRatePercent;
        existing.DefaultFacilityPermitId = facility.DefaultFacilityPermitId;
        existing.LagoonBermHeightFeet = facility.LagoonBermHeightFeet;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Facility '{FacilityName}' updated (ID: {FacilityId})", facility.Name, facility.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var facility = await _context.Facilities
            .FirstOrDefaultAsync(f => f.Id == id);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), id);

        // Check if facility is referenced
        var hasReferences = await _context.WWChars.AnyAsync(w => w.FacilityId == id) ||
                           await _context.GWMonits.AnyAsync(g => g.FacilityId == id) ||
                           await _context.MonthlyApplications.AnyAsync(i => i.FacilityId == id) ||
                           await _context.IrrRprts.AnyAsync(r => r.FacilityId == id) ||
                           await _context.OperatorLogs.AnyAsync(o => o.FacilityId == id) ||
                           await _context.Sprayfields.AnyAsync(s => s.FacilityId == id);

        if (hasReferences)
            throw new BusinessRuleException("Cannot delete facility because it has associated records.");

        // Soft delete
        facility.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Facility '{FacilityName}' soft-deleted (ID: {FacilityId})", facility.Name, facility.Id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Facilities.AnyAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<Facility>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.Facilities
            .AsNoTracking()
            .Where(f => f.CompanyId == companyId)
            .OrderBy(f => f.Name)
            .ToListAsync();
    }
}



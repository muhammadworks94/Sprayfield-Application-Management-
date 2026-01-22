using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for GWMonit (Groundwater Monitoring) entity operations.
/// </summary>
public class GWMonitService : IGWMonitService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GWMonitService> _logger;

    public GWMonitService(ApplicationDbContext context, ILogger<GWMonitService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<GWMonit>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? monitoringWellId = null)
    {
        var query = _context.GWMonits
            .Include(g => g.Company)
            .Include(g => g.Facility)
            .Include(g => g.MonitoringWell)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(g => g.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(g => g.FacilityId == facilityId.Value);
        }

        if (monitoringWellId.HasValue)
        {
            query = query.Where(g => g.MonitoringWellId == monitoringWellId.Value);
        }

        return await query
            .OrderByDescending(g => g.SampleDate)
            .ToListAsync();
    }

    public async Task<GWMonit?> GetByIdAsync(Guid id)
    {
        return await _context.GWMonits
            .Include(g => g.Company)
            .Include(g => g.Facility)
            .Include(g => g.MonitoringWell)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<GWMonit> CreateAsync(GWMonit gwMonit)
    {
        if (gwMonit == null)
            throw new ArgumentNullException(nameof(gwMonit));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == gwMonit.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), gwMonit.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == gwMonit.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), gwMonit.FacilityId);
        if (facility.CompanyId != gwMonit.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Validate monitoring well exists and belongs to same company
        var monitoringWell = await _context.MonitoringWells.FirstOrDefaultAsync(m => m.Id == gwMonit.MonitoringWellId);
        if (monitoringWell == null)
            throw new EntityNotFoundException(nameof(MonitoringWell), gwMonit.MonitoringWellId);
        if (monitoringWell.CompanyId != gwMonit.CompanyId)
            throw new BusinessRuleException("Monitoring well must belong to the same company.");

        _context.GWMonits.Add(gwMonit);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Groundwater monitoring record created for well '{WellId}' on {SampleDate} (ID: {GWMonitId})", 
            monitoringWell.WellId, gwMonit.SampleDate, gwMonit.Id);
        return gwMonit;
    }

    public async Task<GWMonit> UpdateAsync(GWMonit gwMonit)
    {
        if (gwMonit == null)
            throw new ArgumentNullException(nameof(gwMonit));

        var existing = await GetByIdAsync(gwMonit.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(GWMonit), gwMonit.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == gwMonit.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), gwMonit.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == gwMonit.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), gwMonit.FacilityId);
        if (facility.CompanyId != gwMonit.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Validate monitoring well exists and belongs to same company
        var monitoringWell = await _context.MonitoringWells.FirstOrDefaultAsync(m => m.Id == gwMonit.MonitoringWellId);
        if (monitoringWell == null)
            throw new EntityNotFoundException(nameof(MonitoringWell), gwMonit.MonitoringWellId);
        if (monitoringWell.CompanyId != gwMonit.CompanyId)
            throw new BusinessRuleException("Monitoring well must belong to the same company.");

        existing.SampleDate = gwMonit.SampleDate;
        existing.SampleDepth = gwMonit.SampleDepth;
        existing.WaterLevel = gwMonit.WaterLevel;
        existing.Temperature = gwMonit.Temperature;
        existing.PH = gwMonit.PH;
        existing.Conductivity = gwMonit.Conductivity;
        existing.TDS = gwMonit.TDS;
        existing.Turbidity = gwMonit.Turbidity;
        existing.BOD5 = gwMonit.BOD5;
        existing.COD = gwMonit.COD;
        existing.TSS = gwMonit.TSS;
        existing.NH3N = gwMonit.NH3N;
        existing.NO3N = gwMonit.NO3N;
        existing.TKN = gwMonit.TKN;
        existing.TotalPhosphorus = gwMonit.TotalPhosphorus;
        existing.Chloride = gwMonit.Chloride;
        existing.FecalColiform = gwMonit.FecalColiform;
        existing.TotalColiform = gwMonit.TotalColiform;
        existing.LabCertification = gwMonit.LabCertification;
        existing.CollectedBy = gwMonit.CollectedBy;
        existing.AnalyzedBy = gwMonit.AnalyzedBy;
        existing.Comments = gwMonit.Comments;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Groundwater monitoring record updated (ID: {GWMonitId})", gwMonit.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var gwMonit = await GetByIdAsync(id);
        if (gwMonit == null)
            throw new EntityNotFoundException(nameof(GWMonit), id);

        // Soft delete
        gwMonit.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Groundwater monitoring record soft-deleted (ID: {GWMonitId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.GWMonits.AnyAsync(g => g.Id == id);
    }

    public async Task<IEnumerable<GWMonit>> GetByMonitoringWellIdAsync(Guid monitoringWellId)
    {
        return await _context.GWMonits
            .Where(g => g.MonitoringWellId == monitoringWellId)
            .OrderByDescending(g => g.SampleDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<GWMonit>> GetByDateRangeAsync(Guid? companyId, DateTime startDate, DateTime endDate)
    {
        var query = _context.GWMonits.AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(g => g.CompanyId == companyId.Value);
        }

        return await query
            .Where(g => g.SampleDate >= startDate && g.SampleDate <= endDate)
            .OrderByDescending(g => g.SampleDate)
            .ToListAsync();
    }
}


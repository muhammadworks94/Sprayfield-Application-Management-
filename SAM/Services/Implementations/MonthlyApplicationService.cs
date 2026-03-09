using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

public class MonthlyApplicationService : IMonthlyApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILoadCalculationService _loadCalculationService;

    public MonthlyApplicationService(ApplicationDbContext context, ILoadCalculationService loadCalculationService)
    {
        _context = context;
        _loadCalculationService = loadCalculationService;
    }

    public async Task<IEnumerable<MonthlyApplication>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? zoneId = null)
    {
        var query = _context.MonthlyApplications
            .Include(a => a.Zone)
                .ThenInclude(z => z!.Sprayfield)
            .Include(a => a.Facility)
            .Include(a => a.Company)
            .AsQueryable();

        if (companyId.HasValue) query = query.Where(a => a.CompanyId == companyId.Value);
        if (facilityId.HasValue) query = query.Where(a => a.FacilityId == facilityId.Value);
        if (zoneId.HasValue) query = query.Where(a => a.ZoneId == zoneId.Value);

        return await query
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();
    }

    public async Task<MonthlyApplication?> GetByIdAsync(Guid id)
    {
        return await _context.MonthlyApplications
            .Include(a => a.Zone)
                .ThenInclude(z => z!.Sprayfield)
            .Include(a => a.Facility)
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<MonthlyApplication> CreateAsync(MonthlyApplication application)
    {
        _context.MonthlyApplications.Add(application);
        await _context.SaveChangesAsync();
        await _loadCalculationService.RecalculateForApplicationAsync(application.Id);
        return application;
    }

    public async Task<MonthlyApplication> UpdateAsync(MonthlyApplication application)
    {
        var existing = await _context.MonthlyApplications.FirstOrDefaultAsync(a => a.Id == application.Id)
            ?? throw new EntityNotFoundException(nameof(MonthlyApplication), application.Id);

        existing.FacilityId = application.FacilityId;
        existing.ZoneId = application.ZoneId;
        existing.ApplicationDate = application.ApplicationDate;
        existing.VolumeGallons = application.VolumeGallons;
        existing.NitrogenMgL = application.NitrogenMgL;
        existing.OperatorUserId = application.OperatorUserId;
        existing.OperatorSnapshotName = application.OperatorSnapshotName;
        existing.Comments = application.Comments;

        await _context.SaveChangesAsync();
        await _loadCalculationService.RecalculateForApplicationAsync(existing.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _context.MonthlyApplications.FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new EntityNotFoundException(nameof(MonthlyApplication), id);

        _context.MonthlyApplications.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AnyByZoneIdAsync(Guid zoneId)
    {
        return await _context.MonthlyApplications.AnyAsync(a => a.ZoneId == zoneId);
    }

    public async Task<int> DeleteByZoneIdAsync(Guid zoneId)
    {
        var applications = await _context.MonthlyApplications
            .Where(a => a.ZoneId == zoneId)
            .ToListAsync();

        if (applications.Count == 0)
        {
            return 0;
        }

        _context.MonthlyApplications.RemoveRange(applications);
        await _context.SaveChangesAsync();
        return applications.Count;
    }

    public async Task<int> ReassignZoneAsync(Guid fromZoneId, Guid toZoneId)
    {
        var applications = await _context.MonthlyApplications
            .Where(a => a.ZoneId == fromZoneId)
            .ToListAsync();

        if (applications.Count == 0)
        {
            return 0;
        }

        foreach (var application in applications)
        {
            application.ZoneId = toZoneId;
        }

        await _context.SaveChangesAsync();

        foreach (var application in applications)
        {
            await _loadCalculationService.RecalculateForApplicationAsync(application.Id);
        }

        return applications.Count;
    }
}

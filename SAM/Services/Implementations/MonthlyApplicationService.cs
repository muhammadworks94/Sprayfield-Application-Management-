using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class MonthlyApplicationService : IMonthlyApplicationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILoadCalculationService _loadCalculationService;
    private readonly INDAR1Service _ndar1Service;
    private readonly IMonthlyReportProvisionerService _reportProvisioner;
    private readonly ILogger<MonthlyApplicationService> _logger;

    public MonthlyApplicationService(
        ApplicationDbContext context,
        ILoadCalculationService loadCalculationService,
        INDAR1Service ndar1Service,
        IMonthlyReportProvisionerService reportProvisioner,
        ILogger<MonthlyApplicationService> logger)
    {
        _context = context;
        _loadCalculationService = loadCalculationService;
        _ndar1Service = ndar1Service;
        _reportProvisioner = reportProvisioner;
        _logger = logger;
    }

    public async Task<IEnumerable<MonthlyApplication>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null, Guid? sprayfieldId = null)
    {
        var query = _context.MonthlyApplications
            .Include(a => a.Sprayfield)
            .Include(a => a.Facility)
            .Include(a => a.Company)
            .AsQueryable();

        if (companyId.HasValue) query = query.Where(a => a.CompanyId == companyId.Value);
        if (facilityId.HasValue) query = query.Where(a => a.FacilityId == facilityId.Value);
        if (sprayfieldId.HasValue) query = query.Where(a => a.SprayfieldId == sprayfieldId.Value);

        return await query
            .OrderByDescending(a => a.ApplicationDate)
            .ToListAsync();
    }

    public async Task<MonthlyApplication?> GetByIdAsync(Guid id)
    {
        return await _context.MonthlyApplications
            .Include(a => a.Sprayfield)
            .Include(a => a.Facility)
            .Include(a => a.Company)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<MonthlyApplication> CreateAsync(MonthlyApplication application)
    {
        var result = await CreateWithNdarRefreshAsync(application);
        return result.Application;
    }

    public async Task<MonthlyApplication> UpdateAsync(MonthlyApplication application)
    {
        var result = await UpdateWithNdarRefreshAsync(application);
        return result.Application;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var result = await DeleteWithNdarRefreshAsync(id);
        return result.Deleted;
    }

    public async Task<MonthlyApplicationMutationResult> CreateWithNdarRefreshAsync(MonthlyApplication application)
    {
        _context.MonthlyApplications.Add(application);
        await _context.SaveChangesAsync();
        await _loadCalculationService.RecalculateForApplicationAsync(application.Id);

        var outcome = await ProvisionReportsForApplicationMonthAsync(application.FacilityId, application.ApplicationDate);
        return new MonthlyApplicationMutationResult
        {
            Application = application,
            NdarRefreshOutcomes = new List<NdarRefreshOutcome> { outcome }
        };
    }

    public async Task<MonthlyApplicationMutationResult> UpdateWithNdarRefreshAsync(MonthlyApplication application)
    {
        var existing = await _context.MonthlyApplications.FirstOrDefaultAsync(a => a.Id == application.Id)
            ?? throw new EntityNotFoundException(nameof(MonthlyApplication), application.Id);
        var oldDate = existing.ApplicationDate;
        var oldFacilityId = existing.FacilityId;

        existing.FacilityId = application.FacilityId;
        existing.SprayfieldId = application.SprayfieldId;
        existing.ApplicationDate = application.ApplicationDate;
        existing.VolumeGallons = application.VolumeGallons;
        existing.TimeIrrigatedMinutes = application.TimeIrrigatedMinutes;
        existing.MaximumHourlyLoadingInchesPerAcre = application.MaximumHourlyLoadingInchesPerAcre;
        existing.OperatorUserId = application.OperatorUserId;
        existing.OperatorSnapshotName = application.OperatorSnapshotName;
        existing.Comments = application.Comments;

        await _context.SaveChangesAsync();
        await _loadCalculationService.RecalculateForApplicationAsync(existing.Id);

        var outcomes = new List<NdarRefreshOutcome>
        {
            await ProvisionReportsForApplicationMonthAsync(oldFacilityId, oldDate)
        };

        var oldMonth = oldDate.Month;
        var oldYear = oldDate.Year;
        if (oldFacilityId != existing.FacilityId || oldMonth != existing.ApplicationDate.Month || oldYear != existing.ApplicationDate.Year)
        {
            outcomes.Add(await ProvisionReportsForApplicationMonthAsync(existing.FacilityId, existing.ApplicationDate));
        }

        return new MonthlyApplicationMutationResult
        {
            Application = existing,
            NdarRefreshOutcomes = outcomes
        };
    }

    public async Task<(bool Deleted, List<NdarRefreshOutcome> NdarRefreshOutcomes)> DeleteWithNdarRefreshAsync(Guid id)
    {
        var existing = await _context.MonthlyApplications.FirstOrDefaultAsync(a => a.Id == id)
            ?? throw new EntityNotFoundException(nameof(MonthlyApplication), id);

        _context.MonthlyApplications.Remove(existing);
        await _context.SaveChangesAsync();
        var outcome = await RefreshNdar1ForMonthAsync(existing.FacilityId, existing.ApplicationDate);
        return (true, new List<NdarRefreshOutcome> { outcome });
    }

    private async Task<NdarRefreshOutcome> ProvisionReportsForApplicationMonthAsync(Guid facilityId, DateTime date)
    {
        NdarRefreshOutcome outcome;
        try
        {
            outcome = await _reportProvisioner.EnsureNdar1ForMonthAsync(facilityId, date.Month, date.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "NDAR-1 provisioning failed after application log change for facility {FacilityId} month {Month} year {Year}.",
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
            await _reportProvisioner.EnsureNdmlrForMonthAsync(facilityId, date.Month, date.Year);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "NDMLR auto-provision failed after application log change for facility {FacilityId} month {Month} year {Year}.",
                facilityId, date.Month, date.Year);
            outcome.SecondaryWarnings.Add(
                $"NDMLR for {new DateTime(date.Year, date.Month, 1):MMM yyyy} could not be auto-created.");
        }

        return outcome;
    }

    private async Task<NdarRefreshOutcome> RefreshNdar1ForMonthAsync(Guid facilityId, DateTime date)
    {
        try
        {
            return await _ndar1Service.RefreshExistingReportForMonthAsync(facilityId, date.Month, date.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "NDAR-1 refresh failed after monthly application change for facility {FacilityId} month {Month} year {Year}.",
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

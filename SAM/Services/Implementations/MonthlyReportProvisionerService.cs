using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;

namespace SAM.Services.Implementations;

public class MonthlyReportProvisionerService : IMonthlyReportProvisionerService
{
    private readonly ApplicationDbContext _context;
    private readonly INDAR1Service _ndar1Service;
    private readonly IIrrRprtService _irrRprtService;
    private readonly ILogger<MonthlyReportProvisionerService> _logger;

    public MonthlyReportProvisionerService(
        ApplicationDbContext context,
        INDAR1Service ndar1Service,
        IIrrRprtService irrRprtService,
        ILogger<MonthlyReportProvisionerService> logger)
    {
        _context = context;
        _ndar1Service = ndar1Service;
        _irrRprtService = irrRprtService;
        _logger = logger;
    }

    public Task<NdarRefreshOutcome> EnsureNdar1ForMonthAsync(Guid facilityId, int month, int year) =>
        _ndar1Service.EnsureAndRefreshForMonthAsync(facilityId, month, year);

    public async Task EnsureNdmrForMonthAsync(Guid facilityId, int month, int year)
    {
        var existing = await _irrRprtService.GetByFacilityMonthYearAsync(facilityId, month, year);
        if (existing != null)
        {
            return;
        }

        try
        {
            var report = await _irrRprtService.GenerateMonthlyReportAsync(facilityId, month, year);
            await _irrRprtService.CreateAsync(report);
            _logger.LogInformation(
                "Auto-created NDMR report for facility {FacilityId} month {Month} year {Year}.",
                facilityId,
                month,
                year);
        }
        catch (BusinessRuleException ex) when (IsDuplicateReportMessage(ex.Message) || IsNoIrrigationRecordsMessage(ex.Message))
        {
            if (IsNoIrrigationRecordsMessage(ex.Message))
            {
                _logger.LogInformation(
                    "Skipped NDMR auto-create for facility {FacilityId} month {Month} year {Year}: no irrigation records for the month.",
                    facilityId,
                    month,
                    year);
            }
            else
            {
                _logger.LogDebug(
                    ex,
                    "NDMR report already exists for facility {FacilityId} month {Month} year {Year} (race).",
                    facilityId,
                    month,
                    year);
            }
        }
        catch (DbUpdateException ex)
        {
            _logger.LogDebug(
                ex,
                "NDMR report insert conflict for facility {FacilityId} month {Month} year {Year}.",
                facilityId,
                month,
                year);
        }
    }

    public async Task EnsureNdmlrForMonthAsync(Guid facilityId, int month, int year)
    {
        if (month is < 1 or > 12)
        {
            return;
        }

        var monthEnum = (MonthEnum)month;
        var exists = await _context.NDMLRs.AnyAsync(x =>
            x.FacilityId == facilityId &&
            x.Year == year &&
            x.Month == monthEnum);
        if (exists)
        {
            return;
        }

        var facility = await _context.Facilities
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null)
        {
            _logger.LogWarning(
                "Skipped NDMLR auto-create: facility {FacilityId} not found for {Month}/{Year}.",
                facilityId,
                month,
                year);
            return;
        }

        try
        {
            _context.NDMLRs.Add(new NDMLR
            {
                CompanyId = facility.CompanyId,
                FacilityId = facilityId,
                Year = year,
                Month = monthEnum
            });
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Auto-created NDMLR report for facility {FacilityId} through {Month} {Year}.",
                facilityId,
                monthEnum,
                year);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogDebug(
                ex,
                "NDMLR report insert conflict for facility {FacilityId} month {Month} year {Year}.",
                facilityId,
                month,
                year);
        }
    }

    private static bool IsDuplicateReportMessage(string message) =>
        message.Contains("already exists", StringComparison.OrdinalIgnoreCase);

    private static bool IsNoIrrigationRecordsMessage(string message) =>
        message.Contains("No irrigation records", StringComparison.OrdinalIgnoreCase);
}

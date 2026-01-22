using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Domain.Entities.Base;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for IrrRprt (Monthly Irrigation Report) entity operations.
/// </summary>
public class IrrRprtService : IIrrRprtService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IrrRprtService> _logger;
    private readonly IIrrigateService _irrigateService;
    private readonly ISprayfieldService _sprayfieldService;

    public IrrRprtService(
        ApplicationDbContext context,
        ILogger<IrrRprtService> logger,
        IIrrigateService irrigateService,
        ISprayfieldService sprayfieldService)
    {
        _context = context;
        _logger = logger;
        _irrigateService = irrigateService;
        _sprayfieldService = sprayfieldService;
    }

    public async Task<IEnumerable<IrrRprt>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var query = _context.IrrRprts
            .Include(r => r.Company)
            .Include(r => r.Facility)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(r => r.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(r => r.FacilityId == facilityId.Value);
        }

        return await query
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ToListAsync();
    }

    public async Task<IrrRprt?> GetByIdAsync(Guid id)
    {
        return await _context.IrrRprts
            .Include(r => r.Company)
            .Include(r => r.Facility)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IrrRprt> CreateAsync(IrrRprt irrRprt)
    {
        if (irrRprt == null)
            throw new ArgumentNullException(nameof(irrRprt));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == irrRprt.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), irrRprt.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == irrRprt.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), irrRprt.FacilityId);
        if (facility.CompanyId != irrRprt.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if report already exists for this facility/month/year
        var existing = await GetByFacilityMonthYearAsync(irrRprt.FacilityId, (int)irrRprt.Month, irrRprt.Year);
        if (existing != null)
            throw new BusinessRuleException($"A monthly irrigation report already exists for facility {facility.Name} for {irrRprt.Month} {irrRprt.Year}.");

        _context.IrrRprts.Add(irrRprt);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Monthly irrigation report created for facility '{FacilityName}' for {Month} {Year} (ID: {ReportId})",
            facility.Name, irrRprt.Month, irrRprt.Year, irrRprt.Id);
        return irrRprt;
    }

    public async Task<IrrRprt> UpdateAsync(IrrRprt irrRprt)
    {
        if (irrRprt == null)
            throw new ArgumentNullException(nameof(irrRprt));

        var existing = await GetByIdAsync(irrRprt.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(IrrRprt), irrRprt.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == irrRprt.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), irrRprt.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == irrRprt.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), irrRprt.FacilityId);
        if (facility.CompanyId != irrRprt.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if another report exists for this facility/month/year (excluding current)
        var duplicate = await _context.IrrRprts
            .FirstOrDefaultAsync(r => r.Id != irrRprt.Id && r.FacilityId == irrRprt.FacilityId && r.Month == irrRprt.Month && r.Year == irrRprt.Year);
        if (duplicate != null)
            throw new BusinessRuleException($"A monthly irrigation report already exists for facility {facility.Name} for {irrRprt.Month} {irrRprt.Year}.");

        existing.Month = irrRprt.Month;
        existing.Year = irrRprt.Year;
        existing.TotalVolumeApplied = irrRprt.TotalVolumeApplied;
        existing.TotalApplicationRate = irrRprt.TotalApplicationRate;
        existing.HydraulicLoadingRate = irrRprt.HydraulicLoadingRate;
        existing.NitrogenLoadingRate = irrRprt.NitrogenLoadingRate;
        existing.PanUptakeRate = irrRprt.PanUptakeRate;
        existing.ApplicationEfficiency = irrRprt.ApplicationEfficiency;
        existing.WeatherSummary = irrRprt.WeatherSummary;
        existing.OperationalNotes = irrRprt.OperationalNotes;
        existing.ComplianceStatus = irrRprt.ComplianceStatus;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Monthly irrigation report updated (ID: {ReportId})", irrRprt.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var irrRprt = await GetByIdAsync(id);
        if (irrRprt == null)
            throw new EntityNotFoundException(nameof(IrrRprt), id);

        // Soft delete
        irrRprt.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Monthly irrigation report soft-deleted (ID: {ReportId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.IrrRprts.AnyAsync(r => r.Id == id);
    }

    public async Task<IrrRprt?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year)
    {
        return await _context.IrrRprts
            .FirstOrDefaultAsync(r => r.FacilityId == facilityId && (int)r.Month == month && r.Year == year);
    }

    public async Task<IEnumerable<IrrRprt>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.IrrRprts
            .Where(r => r.FacilityId == facilityId)
            .OrderByDescending(r => r.Year)
            .ThenByDescending(r => r.Month)
            .ToListAsync();
    }

    /// <summary>
    /// Generates a monthly irrigation report by aggregating irrigation data for the specified facility, month, and year.
    /// </summary>
    public async Task<IrrRprt> GenerateMonthlyReportAsync(Guid facilityId, int month, int year)
    {
        var facility = await _context.Facilities
            .Include(f => f.Company)
            .FirstOrDefaultAsync(f => f.Id == facilityId);

        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), facilityId);

        // Get all irrigation records for this facility in the specified month/year
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var irrigations = await _context.Irrigates
            .Include(i => i.Sprayfield)
            .ThenInclude(s => s.Crop)
            .Where(i => i.FacilityId == facilityId &&
                       i.IrrigationDate >= startDate &&
                       i.IrrigationDate <= endDate)
            .ToListAsync();

        if (!irrigations.Any())
            throw new BusinessRuleException($"No irrigation records found for facility {facility.Name} for {((MonthEnum)month)} {year}.");

        // Get sprayfields for this facility
        var sprayfields = await _sprayfieldService.GetByFacilityIdAsync(facilityId);
        var sprayfieldList = sprayfields.ToList();

        // Calculate aggregations
        var totalVolumeApplied = irrigations.Sum(i => i.TotalVolumeGallons);
        var totalAcres = sprayfieldList.Sum(s => s.SizeAcres);
        var totalApplicationRate = totalAcres > 0 ? totalVolumeApplied / (totalAcres * 27.154m) : 0; // Convert gallons to inches (1 acre-inch = 27,154 gallons)

        // Calculate hydraulic loading rate (inches per year, annualized from monthly)
        var hydraulicLoadingRate = totalApplicationRate * 12;

        // Calculate nitrogen loading rate from WWChar data if available
        var wwChar = await _context.WWChars
            .FirstOrDefaultAsync(w => w.FacilityId == facilityId && (int)w.Month == month && w.Year == year);

        decimal nitrogenLoadingRate = 0m;
        decimal panUptakeRate = 0m;

        if (wwChar != null && wwChar.NH3NDaily != null && wwChar.NH3NDaily.Any())
        {
            // Calculate average NH3N concentration (mg/L)
            var avgNH3N = wwChar.NH3NDaily.Where(v => v.HasValue).Average(v => v.Value);
            
            // Convert to lbs/acre/year: (mg/L * gallons * 8.34) / (acres * 1,000,000) * 12 months
            // Simplified: assuming average concentration applies to all applied volume
            if (totalAcres > 0)
            {
                nitrogenLoadingRate = (avgNH3N * totalVolumeApplied * 8.34m) / (totalAcres * 1000000m) * 12m;
            }
        }

        // Calculate PAN uptake rate from crop data
        if (sprayfieldList.Any())
        {
            var weightedPanUptake = sprayfieldList
                .Where(s => s.Crop != null)
                .Select(s => new
                {
                    Acres = s.SizeAcres,
                    PanFactor = s.Crop!.PanFactor,
                    NUptake = s.Crop!.NUptake
                })
                .ToList();

            if (weightedPanUptake.Any())
            {
                var totalWeightedAcres = weightedPanUptake.Sum(w => w.Acres);
                if (totalWeightedAcres > 0)
                {
                    panUptakeRate = weightedPanUptake
                        .Sum(w => w.Acres * w.NUptake * w.PanFactor) / totalWeightedAcres;
                }
            }
        }

        // Calculate application efficiency
        // Efficiency = (Actual application rate / Target application rate) * 100
        // Simplified calculation - can be enhanced with more sophisticated logic
        var targetRate = sprayfieldList.Any() 
            ? sprayfieldList.Average(s => s.HydraulicLoadingLimitInPerYr / 12m) 
            : 0m;
        var applicationEfficiency = targetRate > 0 
            ? Math.Min(100, (totalApplicationRate / targetRate) * 100) 
            : 0m;

        // Determine compliance status
        var complianceStatus = DetermineComplianceStatus(
            hydraulicLoadingRate,
            sprayfieldList.Select(s => s.HydraulicLoadingLimitInPerYr).DefaultIfEmpty(0).Max(),
            applicationEfficiency);

        // Aggregate weather conditions
        var weatherConditions = irrigations
            .Where(i => !string.IsNullOrEmpty(i.WeatherConditions))
            .Select(i => i.WeatherConditions)
            .Distinct()
            .ToList();

        var weatherSummary = string.Join("; ", weatherConditions.Take(5)); // Limit to 5 most common

        // Create the report
        var report = new IrrRprt
        {
            CompanyId = facility.CompanyId,
            FacilityId = facilityId,
            Month = (MonthEnum)month,
            Year = year,
            TotalVolumeApplied = totalVolumeApplied,
            TotalApplicationRate = totalApplicationRate,
            HydraulicLoadingRate = hydraulicLoadingRate,
            NitrogenLoadingRate = nitrogenLoadingRate,
            PanUptakeRate = panUptakeRate,
            ApplicationEfficiency = applicationEfficiency,
            WeatherSummary = weatherSummary,
            OperationalNotes = $"Generated from {irrigations.Count} irrigation record(s).",
            ComplianceStatus = complianceStatus
        };

        return report;
    }

    /// <summary>
    /// Determines compliance status based on hydraulic loading rate and application efficiency.
    /// </summary>
    private ComplianceStatusEnum DetermineComplianceStatus(decimal hydraulicLoadingRate, decimal hydraulicLoadingLimit, decimal applicationEfficiency)
    {
        // Check if hydraulic loading exceeds limit
        if (hydraulicLoadingRate > hydraulicLoadingLimit)
            return ComplianceStatusEnum.NonCompliant;

        // Check if application efficiency is below acceptable threshold (e.g., 70%)
        if (applicationEfficiency < 70m)
            return ComplianceStatusEnum.NonCompliant;

        // If within limits and efficient, check if close to limits (within 10%)
        if (hydraulicLoadingRate > hydraulicLoadingLimit * 0.9m || applicationEfficiency < 75m)
            return ComplianceStatusEnum.UnderReview;

        return ComplianceStatusEnum.Compliant;
    }
}

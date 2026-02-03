using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using System.Text.Json;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for NDAR1 (Non-Discharge Application Report) entity operations.
/// </summary>
public class NDAR1Service : INDAR1Service
{
    private const decimal GALLONS_PER_ACRE_INCH = 27152m;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<NDAR1Service> _logger;
    private readonly IIrrigateService _irrigateService;
    private readonly ISprayfieldService _sprayfieldService;
    private readonly IFacilityService _facilityService;
    private readonly IWebHostEnvironment _environment;

    public NDAR1Service(
        ApplicationDbContext context,
        ILogger<NDAR1Service> logger,
        IIrrigateService irrigateService,
        ISprayfieldService sprayfieldService,
        IFacilityService facilityService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _irrigateService = irrigateService;
        _sprayfieldService = sprayfieldService;
        _facilityService = facilityService;
        _environment = environment;
    }

    public async Task<IEnumerable<NDAR1>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null)
    {
        var query = _context.NDAR1s
            .Include(n => n.Company)
            .Include(n => n.Facility)
            .Include(n => n.Field1)
            .Include(n => n.Field2)
            .Include(n => n.Field3)
            .Include(n => n.Field4)
            .AsQueryable();

        if (companyId.HasValue)
        {
            query = query.Where(n => n.CompanyId == companyId.Value);
        }

        if (facilityId.HasValue)
        {
            query = query.Where(n => n.FacilityId == facilityId.Value);
        }

        return await query
            .OrderByDescending(n => n.Year)
            .ThenByDescending(n => n.Month)
            .ToListAsync();
    }

    public async Task<NDAR1?> GetByIdAsync(Guid id)
    {
        return await _context.NDAR1s
            .Include(n => n.Company)
            .Include(n => n.Facility)
            .Include(n => n.Field1)
                .ThenInclude(f => f.Crop)
            .Include(n => n.Field2)
                .ThenInclude(f => f.Crop)
            .Include(n => n.Field3)
                .ThenInclude(f => f.Crop)
            .Include(n => n.Field4)
                .ThenInclude(f => f.Crop)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<NDAR1> CreateAsync(NDAR1 ndar1)
    {
        if (ndar1 == null)
            throw new ArgumentNullException(nameof(ndar1));

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == ndar1.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), ndar1.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == ndar1.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), ndar1.FacilityId);
        if (facility.CompanyId != ndar1.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if report already exists for this facility/month/year
        var existing = await GetByFacilityMonthYearAsync(ndar1.FacilityId, (int)ndar1.Month, ndar1.Year);
        if (existing != null)
            throw new BusinessRuleException($"An NDAR-1 report already exists for facility {facility.Name} for {ndar1.Month} {ndar1.Year}.");

        // Initialize daily arrays if empty
        InitializeDailyArrays(ndar1);

        _context.NDAR1s.Add(ndar1);
        await _context.SaveChangesAsync();

        _logger.LogInformation("NDAR-1 report created for facility '{FacilityName}' for {Month} {Year} (ID: {ReportId})",
            facility.Name, ndar1.Month, ndar1.Year, ndar1.Id);
        return ndar1;
    }

    public async Task<NDAR1> UpdateAsync(NDAR1 ndar1)
    {
        if (ndar1 == null)
            throw new ArgumentNullException(nameof(ndar1));

        var existing = await GetByIdAsync(ndar1.Id);
        if (existing == null)
            throw new EntityNotFoundException(nameof(NDAR1), ndar1.Id);

        // Validate company exists
        var companyExists = await _context.Companies.AnyAsync(c => c.Id == ndar1.CompanyId);
        if (!companyExists)
            throw new EntityNotFoundException(nameof(Company), ndar1.CompanyId);

        // Validate facility exists and belongs to same company
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == ndar1.FacilityId);
        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), ndar1.FacilityId);
        if (facility.CompanyId != ndar1.CompanyId)
            throw new BusinessRuleException("Facility must belong to the same company.");

        // Check if another report exists for this facility/month/year (excluding current)
        var duplicate = await _context.NDAR1s
            .FirstOrDefaultAsync(n => n.Id != ndar1.Id && n.FacilityId == ndar1.FacilityId && n.Month == ndar1.Month && n.Year == ndar1.Year);
        if (duplicate != null)
            throw new BusinessRuleException($"An NDAR-1 report already exists for facility {facility.Name} for {ndar1.Month} {ndar1.Year}.");

        // Update all properties
        existing.Month = ndar1.Month;
        existing.Year = ndar1.Year;
        existing.DidIrrigationOccur = ndar1.DidIrrigationOccur;
        existing.WeatherCodeDaily = ndar1.WeatherCodeDaily;
        existing.TemperatureDaily = ndar1.TemperatureDaily;
        existing.PrecipitationDaily = ndar1.PrecipitationDaily;
        existing.StorageDaily = ndar1.StorageDaily;
        existing.FiveDayUpsetDaily = ndar1.FiveDayUpsetDaily;

        // Field 1
        existing.Field1Id = ndar1.Field1Id;
        existing.Field1VolumeAppliedDaily = ndar1.Field1VolumeAppliedDaily;
        existing.Field1TimeIrrigatedDaily = ndar1.Field1TimeIrrigatedDaily;
        existing.Field1DailyLoadingDaily = ndar1.Field1DailyLoadingDaily;
        existing.Field1MaxHourlyLoadingDaily = ndar1.Field1MaxHourlyLoadingDaily;
        existing.Field1MonthlyLoading = ndar1.Field1MonthlyLoading;
        existing.Field1MaxHourlyLoading = ndar1.Field1MaxHourlyLoading;
        existing.Field1TwelveMonthFloatingTotal = ndar1.Field1TwelveMonthFloatingTotal;

        // Field 2
        existing.Field2Id = ndar1.Field2Id;
        existing.Field2VolumeAppliedDaily = ndar1.Field2VolumeAppliedDaily;
        existing.Field2TimeIrrigatedDaily = ndar1.Field2TimeIrrigatedDaily;
        existing.Field2DailyLoadingDaily = ndar1.Field2DailyLoadingDaily;
        existing.Field2MaxHourlyLoadingDaily = ndar1.Field2MaxHourlyLoadingDaily;
        existing.Field2MonthlyLoading = ndar1.Field2MonthlyLoading;
        existing.Field2MaxHourlyLoading = ndar1.Field2MaxHourlyLoading;
        existing.Field2TwelveMonthFloatingTotal = ndar1.Field2TwelveMonthFloatingTotal;

        // Field 3
        existing.Field3Id = ndar1.Field3Id;
        existing.Field3VolumeAppliedDaily = ndar1.Field3VolumeAppliedDaily;
        existing.Field3TimeIrrigatedDaily = ndar1.Field3TimeIrrigatedDaily;
        existing.Field3DailyLoadingDaily = ndar1.Field3DailyLoadingDaily;
        existing.Field3MaxHourlyLoadingDaily = ndar1.Field3MaxHourlyLoadingDaily;
        existing.Field3MonthlyLoading = ndar1.Field3MonthlyLoading;
        existing.Field3MaxHourlyLoading = ndar1.Field3MaxHourlyLoading;
        existing.Field3TwelveMonthFloatingTotal = ndar1.Field3TwelveMonthFloatingTotal;

        // Field 4
        existing.Field4Id = ndar1.Field4Id;
        existing.Field4VolumeAppliedDaily = ndar1.Field4VolumeAppliedDaily;
        existing.Field4TimeIrrigatedDaily = ndar1.Field4TimeIrrigatedDaily;
        existing.Field4DailyLoadingDaily = ndar1.Field4DailyLoadingDaily;
        existing.Field4MaxHourlyLoadingDaily = ndar1.Field4MaxHourlyLoadingDaily;
        existing.Field4MonthlyLoading = ndar1.Field4MonthlyLoading;
        existing.Field4MaxHourlyLoading = ndar1.Field4MaxHourlyLoading;
        existing.Field4TwelveMonthFloatingTotal = ndar1.Field4TwelveMonthFloatingTotal;

        await _context.SaveChangesAsync();

        _logger.LogInformation("NDAR-1 report updated (ID: {ReportId})", ndar1.Id);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var ndar1 = await GetByIdAsync(id);
        if (ndar1 == null)
            throw new EntityNotFoundException(nameof(NDAR1), id);

        // Hard delete - permanently remove the report from the database
        _context.NDAR1s.Remove(ndar1);
        await _context.SaveChangesAsync();

        _logger.LogInformation("NDAR-1 report hard-deleted (ID: {ReportId})", id);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.NDAR1s.AnyAsync(n => n.Id == id);
    }

    public async Task<NDAR1?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year)
    {
        return await _context.NDAR1s
            .FirstOrDefaultAsync(n => n.FacilityId == facilityId && (int)n.Month == month && n.Year == year);
    }

    public async Task<IEnumerable<NDAR1>> GetByFacilityIdAsync(Guid facilityId)
    {
        return await _context.NDAR1s
            .Where(n => n.FacilityId == facilityId)
            .OrderByDescending(n => n.Year)
            .ThenByDescending(n => n.Month)
            .ToListAsync();
    }

    /// <summary>
    /// Generates a monthly NDAR-1 report by aggregating irrigation data for the specified facility, month, and year.
    /// </summary>
    public async Task<NDAR1> GenerateMonthlyReportAsync(Guid facilityId, int month, int year)
    {
        var facility = await _context.Facilities
            .Include(f => f.Company)
            .FirstOrDefaultAsync(f => f.Id == facilityId);

        if (facility == null)
            throw new EntityNotFoundException(nameof(Facility), facilityId);

        // Get all irrigation records for this facility in the specified month/year
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        var irrigations = await _context.Irrigates
            .Include(i => i.Sprayfield)
                .ThenInclude(s => s.Crop)
            .Where(i => i.FacilityId == facilityId &&
                       i.IrrigationDate >= startDate &&
                       i.IrrigationDate <= endDate)
            .ToListAsync();

        // Get sprayfields for this facility
        var sprayfields = await _sprayfieldService.GetByFacilityIdAsync(facilityId);
        var sprayfieldList = sprayfields.Take(4).ToList(); // Limit to 4 fields

        // Get operator logs for weather data
        var operatorLogs = await _context.OperatorLogs
            .Where(o => o.FacilityId == facilityId &&
                       o.LogDate >= startDate &&
                       o.LogDate <= endDate)
            .ToListAsync();

        // Initialize the report
        var report = new NDAR1
        {
            CompanyId = facility.CompanyId,
            FacilityId = facilityId,
            Month = (MonthEnum)month,
            Year = year,
            DidIrrigationOccur = irrigations.Any()
        };

        // Initialize daily arrays
        InitializeDailyArrays(report);

        // Populate field references
        if (sprayfieldList.Count > 0) report.Field1Id = sprayfieldList[0].Id;
        if (sprayfieldList.Count > 1) report.Field2Id = sprayfieldList[1].Id;
        if (sprayfieldList.Count > 2) report.Field3Id = sprayfieldList[2].Id;
        if (sprayfieldList.Count > 3) report.Field4Id = sprayfieldList[3].Id;

        // Aggregate daily weather data from operator logs
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);
            var dayIndex = day - 1;

            var logForDay = operatorLogs.FirstOrDefault(o => o.LogDate.Date == currentDate.Date);
            if (logForDay != null && !string.IsNullOrEmpty(logForDay.WeatherConditions))
            {
                report.WeatherCodeDaily[dayIndex] = logForDay.WeatherConditions;
            }
        }

        // Aggregate daily irrigation data by field
        var fieldIrrigations = new Dictionary<Guid, List<Irrigate>>();
        foreach (var irrigation in irrigations)
        {
            if (!fieldIrrigations.ContainsKey(irrigation.SprayfieldId))
            {
                fieldIrrigations[irrigation.SprayfieldId] = new List<Irrigate>();
            }
            fieldIrrigations[irrigation.SprayfieldId].Add(irrigation);
        }

        // Process each field
        ProcessFieldData(report.Field1Id, fieldIrrigations, report.Field1VolumeAppliedDaily, 
            report.Field1TimeIrrigatedDaily, report.Field1DailyLoadingDaily, 
            report.Field1MaxHourlyLoadingDaily, startDate, daysInMonth);
        ProcessFieldData(report.Field2Id, fieldIrrigations, report.Field2VolumeAppliedDaily, 
            report.Field2TimeIrrigatedDaily, report.Field2DailyLoadingDaily, 
            report.Field2MaxHourlyLoadingDaily, startDate, daysInMonth);
        ProcessFieldData(report.Field3Id, fieldIrrigations, report.Field3VolumeAppliedDaily, 
            report.Field3TimeIrrigatedDaily, report.Field3DailyLoadingDaily, 
            report.Field3MaxHourlyLoadingDaily, startDate, daysInMonth);
        ProcessFieldData(report.Field4Id, fieldIrrigations, report.Field4VolumeAppliedDaily, 
            report.Field4TimeIrrigatedDaily, report.Field4DailyLoadingDaily, 
            report.Field4MaxHourlyLoadingDaily, startDate, daysInMonth);

        // Calculate monthly totals
        report.Field1MonthlyLoading = report.Field1DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field1MaxHourlyLoading = report.Field1MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();
        report.Field2MonthlyLoading = report.Field2DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field2MaxHourlyLoading = report.Field2MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();
        report.Field3MonthlyLoading = report.Field3DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field3MaxHourlyLoading = report.Field3MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();
        report.Field4MonthlyLoading = report.Field4DailyLoadingDaily.Where(v => v.HasValue).Sum(v => v.Value);
        report.Field4MaxHourlyLoading = report.Field4MaxHourlyLoadingDaily.Where(v => v.HasValue)
            .Select(v => v.Value)
            .DefaultIfEmpty(0m)
            .Max();

        // Calculate 12-month floating totals (from previous 12 months)
        await CalculateTwelveMonthFloatingTotals(report);

        return report;
    }

    private void ProcessFieldData(Guid? fieldId, Dictionary<Guid, List<Irrigate>> fieldIrrigations,
        List<decimal?> volumeDaily, List<decimal?> timeDaily, List<decimal?> loadingDaily,
        List<decimal?> maxHourlyLoadingDaily, DateTime startDate, int daysInMonth)
    {
        if (!fieldId.HasValue || !fieldIrrigations.ContainsKey(fieldId.Value))
            return;

        var irrigations = fieldIrrigations[fieldId.Value];
        var sprayfield = irrigations.First().Sprayfield;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(startDate.Year, startDate.Month, day);
            var dayIndex = day - 1;

            var dayIrrigations = irrigations.Where(i => i.IrrigationDate.Date == currentDate.Date).ToList();
            if (dayIrrigations.Any())
            {
                // Get sprayfield area
                var areaAcres = sprayfield.SizeAcres;

                // Calculate volume and time
                var volumeApplied = dayIrrigations.Sum(i => i.TotalVolumeGallons);
                var timeIrrigatedMinutes = dayIrrigations.Sum(i => (decimal)(i.EndTime - i.StartTime).TotalMinutes);

                volumeDaily[dayIndex] = volumeApplied;
                timeDaily[dayIndex] = timeIrrigatedMinutes;

                // Calculate daily loading: Volume Applied / (Area × 27,152)
                if (areaAcres > 0 && volumeApplied > 0)
                {
                    loadingDaily[dayIndex] = volumeApplied / (areaAcres * GALLONS_PER_ACRE_INCH);
                }
                else
                {
                    loadingDaily[dayIndex] = null;
                }

                // Calculate maximum hourly loading based on time irrigated
                var dailyLoading = loadingDaily[dayIndex];
                if (timeIrrigatedMinutes > 0 && dailyLoading.HasValue)
                {
                    if (timeIrrigatedMinutes < 60)
                    {
                        // If Time Irrigated < 60 minutes: Maximum Hourly Loading = Daily Loading
                        maxHourlyLoadingDaily[dayIndex] = dailyLoading.Value;
                    }
                    else
                    {
                        // If Time Irrigated ≥ 60 minutes: Maximum Hourly Loading = (Daily Loading / Time Irrigated) × 60
                        maxHourlyLoadingDaily[dayIndex] = (dailyLoading.Value / timeIrrigatedMinutes) * 60m;
                    }
                }
                else
                {
                    maxHourlyLoadingDaily[dayIndex] = null;
                }
            }
        }
    }

    private async Task CalculateTwelveMonthFloatingTotals(NDAR1 report)
    {
        var reportDate = new DateTime(report.Year, (int)report.Month, 1);
        var startDate = reportDate.AddMonths(-11); // 12 months including current

        var previousReports = await _context.NDAR1s
            .Where(n => n.FacilityId == report.FacilityId &&
                       n.Month >= (MonthEnum)startDate.Month && n.Year >= startDate.Year &&
                       n.Month <= report.Month && n.Year <= report.Year &&
                       n.Id != report.Id)
            .OrderBy(n => n.Year)
            .ThenBy(n => n.Month)
            .ToListAsync();

        // Calculate floating totals for each field
        report.Field1TwelveMonthFloatingTotal = CalculateFieldFloatingTotal(
            report.Field1MonthlyLoading, previousReports, f => f.Field1MonthlyLoading);
        report.Field2TwelveMonthFloatingTotal = CalculateFieldFloatingTotal(
            report.Field2MonthlyLoading, previousReports, f => f.Field2MonthlyLoading);
        report.Field3TwelveMonthFloatingTotal = CalculateFieldFloatingTotal(
            report.Field3MonthlyLoading, previousReports, f => f.Field3MonthlyLoading);
        report.Field4TwelveMonthFloatingTotal = CalculateFieldFloatingTotal(
            report.Field4MonthlyLoading, previousReports, f => f.Field4MonthlyLoading);
    }

    private decimal CalculateFieldFloatingTotal(decimal currentMonthly, 
        List<NDAR1> previousReports, Func<NDAR1, decimal> fieldSelector)
    {
        var total = currentMonthly;
        var months = previousReports.Take(11).Sum(fieldSelector);
        return total + months;
    }

    private void InitializeDailyArrays(NDAR1 ndar1)
    {
        // Initialize all daily arrays with 31 null values
        while (ndar1.WeatherCodeDaily.Count < 31) ndar1.WeatherCodeDaily.Add(null);
        while (ndar1.TemperatureDaily.Count < 31) ndar1.TemperatureDaily.Add(null);
        while (ndar1.PrecipitationDaily.Count < 31) ndar1.PrecipitationDaily.Add(null);
        while (ndar1.StorageDaily.Count < 31) ndar1.StorageDaily.Add(null);
        while (ndar1.FiveDayUpsetDaily.Count < 31) ndar1.FiveDayUpsetDaily.Add(null);

        while (ndar1.Field1VolumeAppliedDaily.Count < 31) ndar1.Field1VolumeAppliedDaily.Add(null);
        while (ndar1.Field1TimeIrrigatedDaily.Count < 31) ndar1.Field1TimeIrrigatedDaily.Add(null);
        while (ndar1.Field1DailyLoadingDaily.Count < 31) ndar1.Field1DailyLoadingDaily.Add(null);
        while (ndar1.Field1MaxHourlyLoadingDaily.Count < 31) ndar1.Field1MaxHourlyLoadingDaily.Add(null);

        while (ndar1.Field2VolumeAppliedDaily.Count < 31) ndar1.Field2VolumeAppliedDaily.Add(null);
        while (ndar1.Field2TimeIrrigatedDaily.Count < 31) ndar1.Field2TimeIrrigatedDaily.Add(null);
        while (ndar1.Field2DailyLoadingDaily.Count < 31) ndar1.Field2DailyLoadingDaily.Add(null);
        while (ndar1.Field2MaxHourlyLoadingDaily.Count < 31) ndar1.Field2MaxHourlyLoadingDaily.Add(null);

        while (ndar1.Field3VolumeAppliedDaily.Count < 31) ndar1.Field3VolumeAppliedDaily.Add(null);
        while (ndar1.Field3TimeIrrigatedDaily.Count < 31) ndar1.Field3TimeIrrigatedDaily.Add(null);
        while (ndar1.Field3DailyLoadingDaily.Count < 31) ndar1.Field3DailyLoadingDaily.Add(null);
        while (ndar1.Field3MaxHourlyLoadingDaily.Count < 31) ndar1.Field3MaxHourlyLoadingDaily.Add(null);

        while (ndar1.Field4VolumeAppliedDaily.Count < 31) ndar1.Field4VolumeAppliedDaily.Add(null);
        while (ndar1.Field4TimeIrrigatedDaily.Count < 31) ndar1.Field4TimeIrrigatedDaily.Add(null);
        while (ndar1.Field4DailyLoadingDaily.Count < 31) ndar1.Field4DailyLoadingDaily.Add(null);
        while (ndar1.Field4MaxHourlyLoadingDaily.Count < 31) ndar1.Field4MaxHourlyLoadingDaily.Add(null);
    }

    /// <summary>
    /// Exports NDAR-1 report to Excel format using the template file.
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(Guid id)
    {
        var report = await GetByIdAsync(id);
        if (report == null)
            throw new EntityNotFoundException(nameof(NDAR1), id);

        var facility = report.Facility;
        if (facility == null)
            throw new BusinessRuleException("Facility not found for this report.");

        // Load template file
        var templatePath = Path.Combine(_environment.WebRootPath, "forms", "Non-Discharge Application Report (NDAR-1).xlsx");
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        using var workbook = new XLWorkbook(templatePath);
        var worksheet = workbook.Worksheet(1);

        // Populate header information
        // Row 1 value cells: D1 (Permit No.), I1 (Facility Name), P1 (County), S1 (Month), V1 (Year)
        worksheet.Cell("D1").Value = facility.PermitNumber;
        worksheet.Cell("I1").Value = facility.Name;
        worksheet.Cell("P1").Value = facility.County;
        worksheet.Cell("S1").Value = report.Month.ToString();
        worksheet.Cell("V1").Value = report.Year;

        // Did irrigation occur checkbox (Row 5, approximate location)
        // Note: Excel checkboxes are complex, this is a simplified approach
        // You may need to adjust based on actual template structure

        // Field information (Rows 2-7)
        // Field 1: Headers at G2-G7, Data at G-J (Volume, Time, Daily Loading, Max Hourly)
        // Field 2: Headers at I2-I7, Data at K-N
        // Field 3: Headers at K2-K7, Data at O-R
        // Field 4: Headers at M2-M7, Data at S-V
        var fields = new[] { report.Field1, report.Field2, report.Field3, report.Field4 };
        var fieldHeaderColumns = new[] { "G", "I", "K", "M" };

        for (int i = 0; i < fields.Length; i++)
        {
            if (fields[i] != null)
            {
                var col = fieldHeaderColumns[i];
                worksheet.Cell($"{col}2").Value = fields[i].FieldId; // Field Name
                worksheet.Cell($"{col}3").Value = fields[i].SizeAcres; // Area (acres)
                worksheet.Cell($"{col}4").Value = fields[i].Crop?.Name ?? ""; // Cover Crop
                worksheet.Cell($"{col}5").Value = fields[i].HourlyRateInches; // Hourly Rate (in)
                worksheet.Cell($"{col}6").Value = fields[i].AnnualRateInches; // Annual Rate (in)
            }
        }

        // Daily data (Rows 10-40, starting from row 10 for day 1)
        var daysInMonth = DateTime.DaysInMonth(report.Year, (int)report.Month);
        var startRow = 10;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var row = startRow + day - 1;
            var dayIndex = day - 1;

            // Day number
            worksheet.Cell($"A{row}").Value = day;

            // Weather data
            worksheet.Cell($"B{row}").Value = report.WeatherCodeDaily[dayIndex];
            worksheet.Cell($"C{row}").Value = report.TemperatureDaily[dayIndex];
            worksheet.Cell($"D{row}").Value = report.PrecipitationDaily[dayIndex];
            worksheet.Cell($"E{row}").Value = report.StorageDaily[dayIndex];
            worksheet.Cell($"F{row}").Value = report.FiveDayUpsetDaily[dayIndex];

            // Field 1 data (Columns G-J: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field1Id.HasValue)
            {
                worksheet.Cell($"G{row}").Value = report.Field1VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"H{row}").Value = report.Field1TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(G{row})," ",G{row}/($G$3*27152))
                worksheet.Cell($"I{row}").FormulaA1 = $"=IF(ISBLANK(G{row}),\" \",G{row}/($G$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(G{row}),ISBLANK(H{row}))," ",IF(H{row}<60,I{row},(I{row}/H{row})*60))
                worksheet.Cell($"J{row}").FormulaA1 = $"=IF(OR(ISBLANK(G{row}),ISBLANK(H{row})),\" \",IF(H{row}<60,I{row},(I{row}/H{row})*60))";
            }

            // Field 2 data (Columns K-N: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field2Id.HasValue)
            {
                worksheet.Cell($"K{row}").Value = report.Field2VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"L{row}").Value = report.Field2TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(K{row})," ",K{row}/($I$3*27152))
                worksheet.Cell($"M{row}").FormulaA1 = $"=IF(ISBLANK(K{row}),\" \",K{row}/($I$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(K{row}),ISBLANK(L{row}))," ",IF(L{row}<60,M{row},(M{row}/L{row})*60))
                worksheet.Cell($"N{row}").FormulaA1 = $"=IF(OR(ISBLANK(K{row}),ISBLANK(L{row})),\" \",IF(L{row}<60,M{row},(M{row}/L{row})*60))";
            }

            // Field 3 data (Columns O-R: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field3Id.HasValue)
            {
                worksheet.Cell($"O{row}").Value = report.Field3VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"P{row}").Value = report.Field3TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(O{row})," ",O{row}/($K$3*27152))
                worksheet.Cell($"Q{row}").FormulaA1 = $"=IF(ISBLANK(O{row}),\" \",O{row}/($K$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(O{row}),ISBLANK(P{row}))," ",IF(P{row}<60,Q{row},(Q{row}/P{row})*60))
                worksheet.Cell($"R{row}").FormulaA1 = $"=IF(OR(ISBLANK(O{row}),ISBLANK(P{row})),\" \",IF(P{row}<60,Q{row},(Q{row}/P{row})*60))";
            }

            // Field 4 data (Columns S-V: Volume, Time, Daily Loading, Max Hourly)
            if (report.Field4Id.HasValue)
            {
                worksheet.Cell($"S{row}").Value = report.Field4VolumeAppliedDaily[dayIndex];
                worksheet.Cell($"T{row}").Value = report.Field4TimeIrrigatedDaily[dayIndex];
                // Daily Loading formula: =IF(ISBLANK(S{row})," ",S{row}/($M$3*27152))
                worksheet.Cell($"U{row}").FormulaA1 = $"=IF(ISBLANK(S{row}),\" \",S{row}/($M$3*27152))";
                // Maximum Hourly Loading formula: =IF(OR(ISBLANK(S{row}),ISBLANK(T{row}))," ",IF(T{row}<60,U{row},(U{row}/T{row})*60))
                worksheet.Cell($"V{row}").FormulaA1 = $"=IF(OR(ISBLANK(S{row}),ISBLANK(T{row})),\" \",IF(T{row}<60,U{row},(U{row}/T{row})*60))";
            }
        }

        // Monthly totals (Row 41)
        var monthlyRow = 41;
        var dataStartRow = startRow; // Data starts at row 10 (day 1)
        var dataEndRow = startRow + daysInMonth - 1; // Data ends at the last day of the month
        
        if (report.Field1Id.HasValue)
        {
            // Monthly Loading: Sum of daily loading from row 10 to row 40
            worksheet.Cell($"I{monthlyRow}").FormulaA1 = $"=SUM(I{dataStartRow}:I{dataEndRow})";
            // Maximum Hourly Loading: Keep as calculated value (MAX of daily values)
            worksheet.Cell($"J{monthlyRow}").Value = report.Field1MaxHourlyLoading;
        }
        if (report.Field2Id.HasValue)
        {
            worksheet.Cell($"M{monthlyRow}").FormulaA1 = $"=SUM(M{dataStartRow}:M{dataEndRow})";
            worksheet.Cell($"N{monthlyRow}").Value = report.Field2MaxHourlyLoading;
        }
        if (report.Field3Id.HasValue)
        {
            worksheet.Cell($"Q{monthlyRow}").FormulaA1 = $"=SUM(Q{dataStartRow}:Q{dataEndRow})";
            worksheet.Cell($"R{monthlyRow}").Value = report.Field3MaxHourlyLoading;
        }
        if (report.Field4Id.HasValue)
        {
            worksheet.Cell($"U{monthlyRow}").FormulaA1 = $"=SUM(U{dataStartRow}:U{dataEndRow})";
            worksheet.Cell($"V{monthlyRow}").Value = report.Field4MaxHourlyLoading;
        }

        // 12-month floating totals (Row 42)
        var floatingRow = 42;
        if (report.Field1Id.HasValue)
        {
            worksheet.Cell($"I{floatingRow}").Value = report.Field1TwelveMonthFloatingTotal;
        }
        if (report.Field2Id.HasValue)
        {
            worksheet.Cell($"M{floatingRow}").Value = report.Field2TwelveMonthFloatingTotal;
        }
        if (report.Field3Id.HasValue)
        {
            worksheet.Cell($"Q{floatingRow}").Value = report.Field3TwelveMonthFloatingTotal;
        }
        if (report.Field4Id.HasValue)
        {
            worksheet.Cell($"U{floatingRow}").Value = report.Field4TwelveMonthFloatingTotal;
        }

        // Convert to byte array
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}


using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for generating Non-Discharge Monitoring Reports (NDMR)
/// in Excel format using the client-provided NDMR template.
/// </summary>
public class NDMRService : INDMRService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NDMRService> _logger;

    public NDMRService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<NDMRService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Exports an NDMR Excel file for the specified NDAR-1 report.
    /// The NDAR-1 report is used only as a convenient way to select
    /// the facility, month, and year for the monitoring period.
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(Guid ndar1Id)
    {
        var ndar1 = await _context.NDAR1s
            .Include(r => r.Facility)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == ndar1Id);

        if (ndar1 == null)
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);

        var facility = ndar1.Facility;
        if (facility == null)
            throw new BusinessRuleException("Facility not found for this NDAR-1 report.");

        var month = (int)ndar1.Month;
        var year = ndar1.Year;
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // Load NDMR template
        var templatePath = Path.Combine(
            _environment.WebRootPath,
            "forms",
            "Non-Discharge Monitoring Report (NDMR).xlsx");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        using var workbook = new XLWorkbook(templatePath);

        // PPI 001 worksheet (daily monitoring)
        var worksheet = workbook.Worksheet("PPI 001");

        // Header cells (provided by the user)
        worksheet.Cell("C1").Value = facility.PermitNumber;        // Permit number
        worksheet.Cell("G1").Value = facility.Name;                // Facility name
        worksheet.Cell("M1").Value = facility.County;              // County
        worksheet.Cell("P1").Value = ndar1.Month.ToString();       // Month label
        worksheet.Cell("S1").Value = year;                         // Year

        // Parameter code / identifiers (overall)
        // These are mostly static values in the template; we only set them if blank
        if (worksheet.Cell("A3").IsEmpty())
        {
            // Default parameter code for flow; caller can overwrite in Excel if needed
            worksheet.Cell("A3").Value = "50050";
        }

        if (worksheet.Cell("C2").IsEmpty())
        {
            worksheet.Cell("C2").Value = "PPI 001";
        }

        // Flow measuring point and parameter monitoring point are often labels like
        // "Flow, Maximum" plus the PCS code. We do not attempt to compute them
        // dynamically; instead, if the template leaves them blank we populate
        // simple defaults that can be edited later.
        if (worksheet.Cell("D2").IsEmpty())
        {
            worksheet.Cell("D2").Value = "Flow 50050";
        }

        if (worksheet.Cell("K2").IsEmpty())
        {
            worksheet.Cell("K2").Value = "Monitoring Point";
        }

        // Preload irrigation events and groundwater samples for the month
        var irrigations = await _context.Irrigates
            .Where(i => i.FacilityId == facility.Id &&
                        i.IrrigationDate >= startDate &&
                        i.IrrigationDate <= endDate)
            .ToListAsync();

        var gwMonits = await _context.GWMonits
            .Where(g => g.FacilityId == facility.Id &&
                        g.SampleDate >= startDate &&
                        g.SampleDate <= endDate)
            .ToListAsync();

        // Daily grid starts at row 6 (Day 1)
        const int startRow = 6;

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);
            var row = startRow + (day - 1);

            // Column A: Day number
            worksheet.Cell($"A{row}").Value = day;

            // Column D: Flow (GPD)
            var dayIrrigations = irrigations
                .Where(i => i.IrrigationDate.Date == currentDate.Date)
                .ToList();

            if (dayIrrigations.Any())
            {
                // Sum of total volume applied in gallons during the day.
                // Interpreted as daily total flow (GPD) for NDMR purposes.
                var totalGallons = dayIrrigations.Sum(i => i.TotalVolumeGallons);
                worksheet.Cell($"D{row}").Value = totalGallons;
            }
            else
            {
                // Leave blank if no flow recorded, but preserve template formatting.
                worksheet.Cell($"D{row}").Clear(XLClearOptions.Contents);
            }

            // Groundwater monitoring analytes for this date
            var daySamples = gwMonits
                .Where(g => g.SampleDate.Date == currentDate.Date)
                .ToList();

            if (daySamples.Any())
            {
                decimal? Avg(IEnumerable<decimal?> values) =>
                    values.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty().Average();

                var avgTkn = Avg(daySamples.Select(s => s.TKN));
                var avgNh3 = Avg(daySamples.Select(s => s.NH3N));
                var avgNo3 = Avg(daySamples.Select(s => s.NO3N));

                // Column I: TKN (mg/L)
                if (avgTkn.HasValue)
                    worksheet.Cell($"I{row}").Value = avgTkn.Value;

                // Column F: NH3 (mg/L)
                if (avgNh3.HasValue)
                    worksheet.Cell($"F{row}").Value = avgNh3.Value;

                // Column H: NO3 (mg/L)
                if (avgNo3.HasValue)
                    worksheet.Cell($"H{row}").Value = avgNo3.Value;

                // Column E: Total Nitrogen (as N) – approximate as TKN + NO3
                if (avgTkn.HasValue || avgNo3.HasValue)
                {
                    var totalN = (avgTkn ?? 0m) + (avgNo3 ?? 0m);
                    worksheet.Cell($"E{row}").Value = totalN;
                }

                // Column G: Nitrite (NO2) – not stored in the current data model.
                // We intentionally leave column G blank so the user can enter
                // values manually if needed.
            }
            else
            {
                // No groundwater sample for this date; leave nitrogen cells blank
                // but preserve template formatting (shading, borders, etc.).
                worksheet.Cell($"E{row}").Clear(XLClearOptions.Contents);
                worksheet.Cell($"F{row}").Clear(XLClearOptions.Contents);
                worksheet.Cell($"G{row}").Clear(XLClearOptions.Contents);
                worksheet.Cell($"H{row}").Clear(XLClearOptions.Contents);
                worksheet.Cell($"I{row}").Clear(XLClearOptions.Contents);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation(
            "Generated NDMR Excel report for facility {FacilityName} ({FacilityId}) for {Month}/{Year} based on NDAR-1 report {ReportId}.",
            facility.Name,
            facility.Id,
            month,
            year,
            ndar1Id);

        return stream.ToArray();
    }
}


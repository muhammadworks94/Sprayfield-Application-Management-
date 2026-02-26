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
    /// Writes the standard NDMR header block (permit, facility, county, month, year,
    /// PPI label and parameter code/label) into the given worksheet.
    /// This mirrors the layout used on the PPI 001 sheet.
    /// </summary>
    private static void WriteStandardHeader(
        IXLWorksheet worksheet,
        Facility facility,
        SAM.Domain.Enums.MonthEnum monthEnum,
        int year,
        string ppiLabel,
        string parameterCode,
        string parameterName,
        string measuringPointLabel)
    {
        // Core report identification
        worksheet.Cell("C1").Value = facility.PermitNumber;              // Permit number
        worksheet.Cell("G1").Value = facility.Name;                      // Facility name
        worksheet.Cell("M1").Value = facility.County;                    // County
        worksheet.Cell("P1").Value = monthEnum.ToString();               // Month label
        worksheet.Cell("S1").Value = year;                               // Year

        // PPI and parameter identification
        worksheet.Cell("C2").Value = ppiLabel;                           // PPI label
        worksheet.Cell("D2").Value = measuringPointLabel;                // Flow / parameter measuring point
        worksheet.Cell("K2").Value = "Parameter Monitoring Point";       // Generic text; can be edited in Excel

        // Parameter code: label in B3, value in D3 (match template layout; avoid duplicate code in F3)
        worksheet.Cell("B3").Value = "Parameter Code";                   // Label
        worksheet.Cell("D3").Value = parameterCode;                      // Code value (e.g. 50050, 00625)

        // Parameter name/units label above the value column (assumes result column D)
        worksheet.Cell("D4").Value = parameterName;
    }

    /// <summary>
    /// Populates a nitrogen PPI worksheet (one parameter per sheet) using
    /// GWMonit data for the specified reporting period.
    /// </summary>
    private void PopulateNitrogenPpi(
        IXLWorkbook workbook,
        string sheetName,
        Facility facility,
        SAM.Domain.Enums.MonthEnum monthEnum,
        int year,
        string ppiLabel,
        string parameterCode,
        string parameterName,
        string measuringPointLabel,
        IReadOnlyList<GWMonit> gwMonits,
        int daysInMonth,
        int startRow,
        Func<GWMonit, decimal?> selector)
    {
        var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);
        if (worksheet == null)
        {
            _logger.LogWarning(
                "NDMR nitrogen PPI worksheet '{SheetName}' not found in template. Skipping population for parameter {Parameter}.",
                sheetName,
                parameterName);
            return;
        }

        WriteStandardHeader(worksheet, facility, monthEnum, year, ppiLabel, parameterCode, parameterName, measuringPointLabel);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, (int)monthEnum, day);
            var row = startRow + (day - 1);

            // Day number in first column
            worksheet.Cell($"A{row}").Value = day;

            var daySamples = gwMonits
                .Where(g => g.SampleDate.Date == currentDate.Date)
                .ToList();

            if (daySamples.Any())
            {
                var values = daySamples
                    .Select(selector)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();

                if (values.Any())
                {
                    var average = values.Average();
                    worksheet.Cell($"D{row}").Value = average;
                }
                else
                {
                    worksheet.Cell($"D{row}").Clear(XLClearOptions.Contents);
                }
            }
            else
            {
                // No samples on this day – leave the value cell blank but preserve formatting.
                worksheet.Cell($"D{row}").Clear(XLClearOptions.Contents);
            }
        }
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

        // PPI 001 worksheet (daily flow monitoring)
        var flowWorksheet = workbook.Worksheet("PPI 001");

        // Always write a consistent header for the flow sheet.
        WriteStandardHeader(
            flowWorksheet,
            facility,
            ndar1.Month,
            year,
            "PPI 001",
            "50050",
            "Flow (GPD)",
            "Flow Measuring Point");

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

        // Populate flow values on PPI 001
        for (int day = 1; day <= daysInMonth; day++)
        {
            var currentDate = new DateTime(year, month, day);
            var row = startRow + (day - 1);

            // Column A: Day number
            flowWorksheet.Cell($"A{row}").Value = day;

            // Column D: Flow (GPD)
            var dayIrrigations = irrigations
                .Where(i => i.IrrigationDate.Date == currentDate.Date)
                .ToList();

            if (dayIrrigations.Any())
            {
                // Sum of total volume applied in gallons during the day.
                // Interpreted as daily total flow (GPD) for NDMR purposes.
                var totalGallons = dayIrrigations.Sum(i => i.TotalVolumeGallons);
                flowWorksheet.Cell($"D{row}").Value = totalGallons;
            }
            else
            {
                // Leave blank if no flow recorded, but preserve template formatting.
                flowWorksheet.Cell($"D{row}").Clear(XLClearOptions.Contents);
            }
        }

        // Populate nitrogen PPIs (one parameter per sheet) using GWMonit data.
        PopulateNitrogenPpi(
            workbook,
            sheetName: "PPI_TKN",
            facility: facility,
            monthEnum: ndar1.Month,
            year: year,
            ppiLabel: "PPI TKN",
            parameterCode: "00625",
            parameterName: "Total Kjeldahl Nitrogen (TKN) (mg/L)",
            measuringPointLabel: "TKN Sample Point",
            gwMonits: gwMonits,
            daysInMonth: daysInMonth,
            startRow: startRow,
            selector: g => g.TKN);

        PopulateNitrogenPpi(
            workbook,
            sheetName: "PPI_NH3N",
            facility: facility,
            monthEnum: ndar1.Month,
            year: year,
            ppiLabel: "PPI NH3-N",
            parameterCode: "00610",
            parameterName: "Ammonia Nitrogen (NH3-N) (mg/L)",
            measuringPointLabel: "NH3-N Sample Point",
            gwMonits: gwMonits,
            daysInMonth: daysInMonth,
            startRow: startRow,
            selector: g => g.NH3N);

        PopulateNitrogenPpi(
            workbook,
            sheetName: "PPI_NO3N",
            facility: facility,
            monthEnum: ndar1.Month,
            year: year,
            ppiLabel: "PPI NO3-N",
            parameterCode: "00620",
            parameterName: "Nitrate Nitrogen (NO3-N) (mg/L)",
            measuringPointLabel: "NO3-N Sample Point",
            gwMonits: gwMonits,
            daysInMonth: daysInMonth,
            startRow: startRow,
            selector: g => g.NO3N);

        // Total Nitrogen (as N) approximated as TKN + NO3-N
        PopulateNitrogenPpi(
            workbook,
            sheetName: "PPI_TN",
            facility: facility,
            monthEnum: ndar1.Month,
            year: year,
            ppiLabel: "PPI TN",
            parameterCode: "00600",
            parameterName: "Total Nitrogen (as N) (mg/L)",
            measuringPointLabel: "Total Nitrogen Sample Point",
            gwMonits: gwMonits,
            daysInMonth: daysInMonth,
            startRow: startRow,
            selector: g =>
            {
                if (!g.TKN.HasValue && !g.NO3N.HasValue)
                    return null;
                return (g.TKN ?? 0m) + (g.NO3N ?? 0m);
            });

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


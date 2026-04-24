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

        // Always write a consistent header for the master PPI sheet.
        WriteStandardHeader(
            flowWorksheet,
            facility,
            ndar1.Month,
            year,
            "002",
            "00310",
            "BOD5 (mg/L)",
            "Flow Measuring Point");

        // Match client one-page NDMR code layout on the master sheet.
        var masterParameterCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["D"] = "00310",
            ["E"] = "00916",
            ["F"] = "31616",
            ["G"] = "00927",
            ["H"] = "00620",
            ["I"] = "00610",
            ["J"] = "00625",
            ["K"] = "00400",
            ["L"] = "00665",
            ["M"] = "00931",
            ["N"] = "00929",
            ["O"] = "00530",
            ["P"] = "00940",
            ["Q"] = "50060",
            ["R"] = "00600",
            ["S"] = "70300"
        };

        foreach (var mapping in masterParameterCodes)
        {
            flowWorksheet.Cell($"{mapping.Key}3").Value = mapping.Value;
        }

        // Preload irrigation events and groundwater samples for the month
        var gwMonits = await _context.GWMonits
            .Where(g => g.FacilityId == facility.Id &&
                        g.SampleDate >= startDate &&
                        g.SampleDate <= endDate)
            .ToListAsync();

        var wwChar = await _context.WWChars
            .Where(w => w.FacilityId == facility.Id &&
                        (int)w.Month == month &&
                        w.Year == year)
            .FirstOrDefaultAsync();

        var operatorLogs = await _context.OperatorLogs
            .Where(o => o.FacilityId == facility.Id &&
                        o.LogDate >= startDate &&
                        o.LogDate <= endDate)
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

            // Column B/C: ORC arrival time and time-on-site (hours)
            var dayOperatorLogs = operatorLogs
                .Where(o => o.LogDate.Date == currentDate.Date)
                .ToList();

            if (dayOperatorLogs.Any())
            {
                var firstLog = dayOperatorLogs
                    .OrderBy(o => o.ArrivalTime)
                    .First();

                flowWorksheet.Cell($"B{row}").Value = firstLog.ArrivalTime;
                flowWorksheet.Cell($"B{row}").Style.NumberFormat.Format = "hh:mm";

                var avgHours = dayOperatorLogs.Average(o => o.TimeOnSiteHours);
                flowWorksheet.Cell($"C{row}").Value = avgHours;
                flowWorksheet.Cell($"C{row}").Style.NumberFormat.Format = "0.##";
            }
            else
            {
                flowWorksheet.Cell($"B{row}").Clear(XLClearOptions.Contents);
                flowWorksheet.Cell($"C{row}").Clear(XLClearOptions.Contents);
            }

            // Column D: BOD5 from WWChar daily array (00310)
            var bod5Value = (wwChar != null &&
                             wwChar.BOD5Daily != null &&
                             wwChar.BOD5Daily.Count >= day &&
                             wwChar.BOD5Daily[day - 1].HasValue)
                ? wwChar.BOD5Daily[day - 1]
                : null;
            if (bod5Value.HasValue)
            {
                flowWorksheet.Cell($"D{row}").Value = bod5Value.Value;
                flowWorksheet.Cell($"D{row}").Style.NumberFormat.Format = "0.00";
            }
            else
            {
                flowWorksheet.Cell($"D{row}").Clear(XLClearOptions.Contents);
            }

            // Populate supported master-sheet parameter columns from GWMonits.
            var daySamples = gwMonits
                .Where(g => g.SampleDate.Date == currentDate.Date)
                .ToList();

            decimal? AverageOf(Func<GWMonit, decimal?> selector)
            {
                var values = daySamples
                    .Select(selector)
                    .Where(v => v.HasValue)
                    .Select(v => v!.Value)
                    .ToList();
                return values.Any() ? values.Average() : null;
            }

            void SetDecimalCell(string col, decimal? value, string format)
            {
                var cell = flowWorksheet.Cell($"{col}{row}");
                if (value.HasValue)
                {
                    cell.Value = value.Value;
                    cell.Style.NumberFormat.Format = format;
                }
                else
                {
                    cell.Clear(XLClearOptions.Contents);
                }
            }

            SetDecimalCell("F", AverageOf(g => g.FecalColiform), "#,##0.00");
            SetDecimalCell("H", AverageOf(g => g.NO3N), "0.00");
            SetDecimalCell("I", AverageOf(g => g.NH3N), "0.00");
            SetDecimalCell("J", AverageOf(g => g.TKN), "0.00");
            SetDecimalCell("K", AverageOf(g => g.PH), "0.00");

            // Compatibility mapping: 00665 (Total Phosphorus) from TOC in current schema/data payload.
            SetDecimalCell("L", AverageOf(g => g.TOC), "0.00");
            SetDecimalCell("O", AverageOf(g => g.TSS), "0.00");

            // Compatibility mapping: 50060 rendered from Chloride field in current schema/data payload.
            SetDecimalCell("Q", AverageOf(g => g.Chloride), "0.00");

            var totalN = AverageOf(g =>
                (!g.TKN.HasValue && !g.NO3N.HasValue) ? null : (g.TKN ?? 0m) + (g.NO3N ?? 0m));
            SetDecimalCell("R", totalN, "0.00");
        }

        // Prevent ###### for high-count fecal values in summary cells.
        if (flowWorksheet.Column("F").Width < 11)
        {
            flowWorksheet.Column("F").Width = 11;
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

        WriteCertificationPage(workbook, facility);

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

    private static void WriteCertificationPage(IXLWorkbook workbook, Facility facility)
    {
        var certificationWorksheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, "Certification Page", StringComparison.OrdinalIgnoreCase));

        if (certificationWorksheet == null && workbook.Worksheets.Count >= 2)
        {
            certificationWorksheet = workbook.Worksheet(2);
        }

        if (certificationWorksheet == null)
        {
            return;
        }

        certificationWorksheet.Cell("C9").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("D10").Value = facility.OperatorNumber ?? string.Empty;
        certificationWorksheet.Cell("C11").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("G11").Value = facility.OperatorPhone ?? string.Empty;
        certificationWorksheet.Cell("A12").Value = $"Has the ORC changed since the previous NDMR? {(facility.ChangeInOrc == true ? "Yes" : "No")}";
        certificationWorksheet.Cell("I13").Value = DateTime.Today.ToString("MM/dd/yyyy");

        // Permittee certification section
        certificationWorksheet.Cell("M9").Value = facility.Permittee ?? string.Empty;
        certificationWorksheet.Cell("M10").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("N11").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("M12").Value = facility.PermitPhone ?? string.Empty;
        certificationWorksheet.Cell("R12").Value = facility.PermitExpirationDate?.ToString("MM/dd/yyyy") ?? string.Empty;
        certificationWorksheet.Cell("R13").Value = DateTime.Today.ToString("MM/dd/yyyy");

        // Sampling Person(s) and Certified Laboratories
        var samplerNames = (facility.PersonsCollectingSamples ?? string.Empty)
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        certificationWorksheet.Cell("C2").Value = samplerNames.Length > 0 ? samplerNames[0] : string.Empty;
        certificationWorksheet.Cell("C3").Value = samplerNames.Length > 1 ? samplerNames[1] : string.Empty;
        certificationWorksheet.Cell("L2").Value = facility.CertifiedLaboratory1Name ?? string.Empty;
        certificationWorksheet.Cell("L3").Value = facility.CertifiedLaboratory2Name ?? string.Empty;
    }
}

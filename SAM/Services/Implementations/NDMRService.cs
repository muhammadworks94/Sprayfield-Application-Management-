using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for generating Non-Discharge Monitoring Reports (NDMR)
/// in Excel format using the client-provided NDMR template.
/// </summary>
public class NDMRService : INDMRService
{
    private sealed class SamplingMetadata
    {
        public string SamplingType { get; init; } = "Grab";
        public string SampleFrequency { get; init; } = string.Empty;
    }

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NDMRService> _logger;
    private readonly IFacilityPermitResolver _facilityPermitResolver;

    public NDMRService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<NDMRService> logger,
        IFacilityPermitResolver facilityPermitResolver)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _facilityPermitResolver = facilityPermitResolver;
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
        FlowMeasuringPointEnum? flowMeasuringPoint,
        ParameterMonitoringPointEnum? parameterMonitoringPoint)
    {
        // Core report identification
        worksheet.Cell("C1").Value = facility.PermitNumber;              // Permit number
        worksheet.Cell("G1").Value = facility.Name;                      // Facility name
        worksheet.Cell("M1").Value = facility.County;                    // County
        worksheet.Cell("P1").Value = monthEnum.ToString();               // Month label
        worksheet.Cell("S1").Value = year;                               // Year

        // PPI and parameter identification
        worksheet.Cell("C2").Value = ppiLabel;                           // PPI label
        WriteMonitoringPointOptions(
            worksheet.Cell("D2"),
            "Flow Measuring Point",
            BuildFlowMonitoringPointOptions(flowMeasuringPoint));
        WriteMonitoringPointOptions(
            worksheet.Cell("K2"),
            "Parameter Monitoring Point",
            BuildParameterMonitoringPointOptions(parameterMonitoringPoint));

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
        FlowMeasuringPointEnum? flowMeasuringPoint,
        ParameterMonitoringPointEnum? parameterMonitoringPoint,
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

        WriteStandardHeader(
            worksheet,
            facility,
            monthEnum,
            year,
            ppiLabel,
            parameterCode,
            parameterName,
            flowMeasuringPoint,
            parameterMonitoringPoint);

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

    private static IReadOnlyList<(string Text, bool Selected)> BuildFlowMonitoringPointOptions(FlowMeasuringPointEnum? selected)
    {
        return new List<(string Text, bool Selected)>
        {
            ("Influent", selected == FlowMeasuringPointEnum.Influent),
            ("Effluent", selected == FlowMeasuringPointEnum.Effluent),
            ("No flow generated", selected == FlowMeasuringPointEnum.NoFlowGenerated)
        };
    }

    private static IReadOnlyList<(string Text, bool Selected)> BuildParameterMonitoringPointOptions(ParameterMonitoringPointEnum? selected)
    {
        return new List<(string Text, bool Selected)>
        {
            ("Influent", selected == ParameterMonitoringPointEnum.Influent),
            ("Effluent", selected == ParameterMonitoringPointEnum.Effluent),
            ("Groundwater Lowering", selected == ParameterMonitoringPointEnum.GroundwaterLowering),
            ("Surface Water", selected == ParameterMonitoringPointEnum.SurfaceWater)
        };
    }

    private static void WriteMonitoringPointOptions(
        IXLCell cell,
        string label,
        IReadOnlyList<(string Text, bool Selected)> options)
    {
        var richText = cell.GetRichText();
        richText.ClearText();

        richText.AddText($"{label}: ");

        for (var i = 0; i < options.Count; i++)
        {
            var option = options[i];
            var marker = option.Selected ? "\u2611" : "\u2610";
            richText.AddText($"{marker} {option.Text}")
                .SetFontSize(9)
                .SetBold(false);

            if (i < options.Count - 1)
            {
                richText.AddText("    ");
            }
        }
    }

    private static void PopulateSamplingFooterRows(IXLWorksheet worksheet, IReadOnlyDictionary<string, SamplingMetadata> samplingMetadataByParameterCode)
    {
        const int parameterCodeRow = 3;
        const int samplingTypeRow = 40;
        const int sampleFrequencyRow = 43;
        const double footerFontSize = 9;

        var startCol = XLHelper.GetColumnNumberFromLetter("D");
        var endCol = XLHelper.GetColumnNumberFromLetter("S");

        for (var colNum = startCol; colNum <= endCol; colNum++)
        {
            var col = XLHelper.GetColumnLetterFromNumber(colNum);
            var parameterCode = worksheet.Cell($"{col}{parameterCodeRow}").GetString().Trim();

            var samplingTypeCell = worksheet.Cell($"{col}{samplingTypeRow}");
            var sampleFrequencyCell = worksheet.Cell($"{col}{sampleFrequencyRow}");

            if (string.IsNullOrWhiteSpace(parameterCode) ||
                !samplingMetadataByParameterCode.TryGetValue(parameterCode, out var metadata))
            {
                samplingTypeCell.Clear(XLClearOptions.Contents);
                sampleFrequencyCell.Clear(XLClearOptions.Contents);
                continue;
            }

            samplingTypeCell.Value = metadata.SamplingType;
            samplingTypeCell.Style.Font.FontSize = footerFontSize;

            sampleFrequencyCell.Value = metadata.SampleFrequency;
            sampleFrequencyCell.Style.Font.FontSize = footerFontSize;
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

        var wwChar = await _context.WWChars
            .Where(w => w.FacilityId == facility.Id &&
                        (int)w.Month == month &&
                        w.Year == year)
            .FirstOrDefaultAsync();

        using var workbook = new XLWorkbook(templatePath);

        // PPI 001 worksheet (daily flow monitoring)
        var flowWorksheet = workbook.Worksheet("PPI 001");
        var reportDate = new DateTime(year, month, 1);
        var permit = await _facilityPermitResolver.ResolveForDateAsync(facility.Id, reportDate);

        // Always write a consistent header for the master PPI sheet.
        WriteStandardHeader(
            flowWorksheet,
            facility,
            ndar1.Month,
            year,
            "002",
            "00310",
            "BOD5 (mg/L)",
            wwChar?.FlowMeasuringPoint,
            wwChar?.ParameterMonitoringPoint);

        if (permit != null)
        {
            flowWorksheet.Cell("C1").Value = $"{permit.PermitNumber} v{permit.PermitVersion}";
        }

        // Match client one-page NDMR code layout on the master sheet.
        var permitTemplateRows = permit == null
            ? new List<FacilityPermitTemplateParameter>()
            : await _context.FacilityPermitTemplateParameters
                .Include(x => x.PcsParameterCatalog)
                .Where(x => x.FacilityPermitId == permit.Id && (x.ReportTypes & PermitTemplateReportTypeEnum.Ndmr) != 0)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

        if (permitTemplateRows.Any())
        {
            var hasFlow = permitTemplateRows.Any(x => string.Equals(x.PcsParameterCatalog!.PcsCode, "50050", StringComparison.OrdinalIgnoreCase) && x.IsRequired);
            if (!hasFlow)
            {
                throw new BusinessRuleException("Permit template must include required PCS code 50050 (Flow) for NDMR export.");
            }
        }

        var codeSlots = new[] { "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S" };
        var codesToRender = permitTemplateRows.Any()
            ? permitTemplateRows.Select(x => x.PcsParameterCatalog?.PcsCode ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).Take(codeSlots.Length).ToList()
            : new List<string> { "00310", "00916", "31616", "00927", "00620", "00610", "00625", "00400", "00665", "00931", "00929", "00530", "00940", "50060", "00600", "70300" };

        var samplingMetadataByParameterCode = new Dictionary<string, SamplingMetadata>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in permitTemplateRows)
        {
            var code = row.PcsParameterCatalog?.PcsCode;
            if (string.IsNullOrWhiteSpace(code)) continue;
            samplingMetadataByParameterCode[code] = new SamplingMetadata
            {
                SamplingType = row.SampleType.ToString(),
                SampleFrequency = row.MeasurementFrequency.ToString()
            };
        }

        for (var idx = 0; idx < codesToRender.Count && idx < codeSlots.Length; idx++)
        {
            flowWorksheet.Cell($"{codeSlots[idx]}3").Value = codesToRender[idx];
        }

        PopulateSamplingFooterRows(flowWorksheet, samplingMetadataByParameterCode);

        // Preload irrigation events and groundwater samples for the month
        var gwMonits = await _context.GWMonits
            .Where(g => g.FacilityId == facility.Id &&
                        g.SampleDate >= startDate &&
                        g.SampleDate <= endDate)
            .ToListAsync();

        var operatorLogs = await _context.OperatorLogs
            .Where(o => o.FacilityId == facility.Id &&
                        o.LogDate >= startDate &&
                        o.LogDate <= endDate)
            .ToListAsync();

        var irrigationReport = await _context.IrrRprts
            .Where(i => i.FacilityId == facility.Id &&
                        (int)i.Month == month &&
                        i.Year == year)
            .OrderByDescending(i => i.UpdatedDate)
            .FirstOrDefaultAsync();

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
            flowMeasuringPoint: wwChar?.FlowMeasuringPoint,
            parameterMonitoringPoint: wwChar?.ParameterMonitoringPoint,
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
            flowMeasuringPoint: wwChar?.FlowMeasuringPoint,
            parameterMonitoringPoint: wwChar?.ParameterMonitoringPoint,
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
            flowMeasuringPoint: wwChar?.FlowMeasuringPoint,
            parameterMonitoringPoint: wwChar?.ParameterMonitoringPoint,
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
            flowMeasuringPoint: wwChar?.FlowMeasuringPoint,
            parameterMonitoringPoint: wwChar?.ParameterMonitoringPoint,
            gwMonits: gwMonits,
            daysInMonth: daysInMonth,
            startRow: startRow,
            selector: g =>
            {
                if (!g.TKN.HasValue && !g.NO3N.HasValue)
                    return null;
                return (g.TKN ?? 0m) + (g.NO3N ?? 0m);
            });

        WriteCertificationPage(workbook, facility, irrigationReport?.ComplianceStatus);

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

    private static void WriteCertificationPage(IXLWorkbook workbook, Facility facility, ComplianceStatusEnum? complianceStatus)
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

        var complianceSelectionText = complianceStatus switch
        {
            ComplianceStatusEnum.Compliant => "☑ Compliant    ☐ Non-Compliant",
            ComplianceStatusEnum.NonCompliant => "☐ Compliant    ☑ Non-Compliant",
            _ => "☐ Compliant    ☐ Non-Compliant"
        };

        var complianceQuestionText = certificationWorksheet.Cell("A4").GetString().Trim();
        if (string.IsNullOrWhiteSpace(complianceQuestionText))
        {
            complianceQuestionText = "Does all monitoring data and sampling frequencies meet the requirements in Attachment A of your permit?";
        }

        var complianceCell = certificationWorksheet.Cell("A4");
        var complianceRichText = complianceCell.GetRichText();
        complianceRichText.ClearText();
        complianceRichText.AddText($"{complianceQuestionText}    ");
        complianceRichText.AddText(complianceSelectionText)
            .SetFontSize(11)
            .SetBold(false);

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

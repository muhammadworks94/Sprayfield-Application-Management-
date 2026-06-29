using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Extensions;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Utilities;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for generating Non-Discharge Monitoring Reports (NDMR)
/// in Excel format using the client-provided NDMR template.
/// </summary>
public class NDMRService : INDMRService
{
    private sealed class NdmrParameterRow
    {
        public required FacilityPermitTemplateParameter TemplateRow { get; init; }
        public required string PcsCode { get; init; }
        public required string DisplayName { get; init; }
        public required string Units { get; init; }
    }

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
        FacilityPermit? permit,
        SAM.Domain.Enums.MonthEnum monthEnum,
        int year,
        string ppiLabel,
        string parameterCode,
        string parameterName,
        FlowMeasuringPointEnum? flowMeasuringPoint,
        ParameterMonitoringPointEnum? parameterMonitoringPoint)
    {
        worksheet.Cell("C1").Value = Gw59FacilityFieldResolver.ResolvePermitNumberForReport(facility, permit);
        worksheet.Cell("G1").Value = facility.Name;
        worksheet.Cell("M1").Value = Gw59FacilityFieldResolver.ResolveCounty(facility, permit);
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
            null,
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
        => NdmrMonitoringPointOptions.BuildFlowOptions(selected);

    private static IReadOnlyList<(string Text, bool Selected)> BuildParameterMonitoringPointOptions(ParameterMonitoringPointEnum? selected)
        => NdmrMonitoringPointOptions.BuildParameterOptions(selected);

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

    private static bool IsTemplateRowApplicableForMonth(FacilityPermitTemplateParameter row, int month)
    {
        var alwaysInclude = row.MeasurementFrequency is MeasurementFrequencyEnum.Daily
            or MeasurementFrequencyEnum.Weekly
            or MeasurementFrequencyEnum.Monthly
            or MeasurementFrequencyEnum.Continuous;
        if (alwaysInclude)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(row.ScheduledMonthsCsv))
        {
            return false;
        }

        var months = row.ScheduledMonthsCsv
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value => int.TryParse(value, out var parsed) ? parsed : -1);
        return months.Any(m => m == month);
    }

    private static string BuildDailyLimitText(FacilityPermitTemplateParameter row, string pcsCode)
    {
        if (row.DailyMaximumLimit.HasValue)
        {
            return row.DailyMaximumLimit.Value.ToString(NdmrFlowFormatting.IsFlowPcs(pcsCode) ? NdmrFlowFormatting.FlowNumericFormat : "0.##");
        }

        if (row.DailyMinimumLimit.HasValue)
        {
            return row.DailyMinimumLimit.Value.ToString(NdmrFlowFormatting.IsFlowPcs(pcsCode) ? NdmrFlowFormatting.FlowNumericFormat : "0.##");
        }

        return string.Empty;
    }

    private static string BuildMonthlyLimitText(FacilityPermitTemplateParameter row, string pcsCode)
    {
        if (NdmrFlowFormatting.IsFlowPcs(pcsCode))
        {
            return NdmrFlowFormatting.BuildFlowMonthlyLimitText(row);
        }

        if (row.MonthlyAverageLimit.HasValue)
        {
            return row.MonthlyAverageLimit.Value.ToString("0.##");
        }

        if (row.MonthlyGeometricMeanLimit.HasValue)
        {
            return row.MonthlyGeometricMeanLimit.Value.ToString("0.##");
        }

        return string.Empty;
    }

    private static decimal? AverageSampleForDay(IReadOnlyList<GWMonit> gwMonits, DateTime currentDate, Func<GWMonit, decimal?> selector)
    {
        var values = gwMonits
            .Where(g => g.SampleDate.Date == currentDate.Date)
            .Select(selector)
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .ToList();

        return values.Count == 0 ? null : values.Average();
    }

    private static decimal? ResolveFallbackDailyValue(
        string pcsCode,
        DateTime currentDate,
        WWChar? wwChar,
        IReadOnlyList<GWMonit> gwMonits)
    {
        decimal? WwAt(IReadOnlyList<decimal?>? values)
        {
            if (values == null) return null;
            var dayIndex = currentDate.Day - 1;
            return dayIndex >= 0 && dayIndex < values.Count ? values[dayIndex] : null;
        }

        return pcsCode switch
        {
            "00310" => WwAt(wwChar?.BOD5Daily),
            "00620" => AverageSampleForDay(gwMonits, currentDate, g => g.NO3N),
            "00610" => AverageSampleForDay(gwMonits, currentDate, g => g.NH3N),
            "00625" => AverageSampleForDay(gwMonits, currentDate, g => g.TKN),
            "00400" => AverageSampleForDay(gwMonits, currentDate, g => g.PH),
            "31616" => AverageSampleForDay(gwMonits, currentDate, g => g.FecalColiform),
            "00940" => AverageSampleForDay(gwMonits, currentDate, g => g.Chloride),
            "00665" => AverageSampleForDay(gwMonits, currentDate, g => g.TOC),
            "00530" => AverageSampleForDay(gwMonits, currentDate, g => g.TSS),
            "00600" => AverageSampleForDay(gwMonits, currentDate, g =>
                (!g.TKN.HasValue && !g.NO3N.HasValue) ? null : (g.TKN ?? 0m) + (g.NO3N ?? 0m)),
            _ => null
        };
    }

    private static void SanitizeErrorCells(IXLWorksheet worksheet)
    {
        foreach (var cell in worksheet.RangeUsed()?.CellsUsed() ?? Enumerable.Empty<IXLCell>())
        {
            if (cell.DataType == XLDataType.Error)
            {
                cell.Clear(XLClearOptions.Contents);
                continue;
            }

            if (cell.HasFormula)
            {
                var formula = cell.FormulaA1?.Trim();
                if (string.IsNullOrWhiteSpace(formula))
                {
                    continue;
                }

                if (!formula.StartsWith("IFERROR(", StringComparison.OrdinalIgnoreCase))
                {
                    cell.FormulaA1 = $"IFERROR({formula},\"\")";
                }
            }
        }
    }

    private static void PopulateSummaryRowsForChunk(
        IXLWorksheet worksheet,
        IReadOnlyList<NdmrParameterRow> chunk,
        IReadOnlyList<string> codeSlots,
        int daysInMonth,
        int startRow)
    {
        const int averageRow = 37;
        const int dailyMaxRow = 38;
        const int dailyMinRow = 39;

        for (var slot = 0; slot < codeSlots.Count; slot++)
        {
            var col = codeSlots[slot];
            var avgCell = worksheet.Cell($"{col}{averageRow}");
            var maxCell = worksheet.Cell($"{col}{dailyMaxRow}");
            var minCell = worksheet.Cell($"{col}{dailyMinRow}");

            avgCell.Clear(XLClearOptions.Contents);
            maxCell.Clear(XLClearOptions.Contents);
            minCell.Clear(XLClearOptions.Contents);

            if (slot >= chunk.Count)
            {
                continue;
            }

            var values = new List<decimal>();
            for (var day = 0; day < daysInMonth; day++)
            {
                var cell = worksheet.Cell($"{col}{startRow + day}");
                if (!cell.TryGetValue<decimal>(out var numericValue))
                {
                    continue;
                }
                values.Add(numericValue);
            }

            if (values.Count == 0)
            {
                continue;
            }

            var parameter = chunk[slot];
            decimal averageValue;
            if (string.Equals(parameter.PcsCode, "31616", StringComparison.OrdinalIgnoreCase) && values.All(v => v > 0m))
            {
                // Fecal coliform average is represented as geometric mean when daily values are positive.
                var logAverage = values.Select(v => Math.Log((double)v)).Average();
                averageValue = (decimal)Math.Exp(logAverage);
            }
            else
            {
                averageValue = values.Average();
            }

            avgCell.Value = averageValue;
            maxCell.Value = values.Max();
            minCell.Value = values.Min();
            var summaryFormat = NdmrFlowFormatting.IsFlowPcs(parameter.PcsCode)
                ? NdmrFlowFormatting.FlowNumericFormat
                : "0.00";
            avgCell.Style.NumberFormat.Format = summaryFormat;
            maxCell.Style.NumberFormat.Format = summaryFormat;
            minCell.Style.NumberFormat.Format = summaryFormat;
        }
    }

    /// <summary>
    /// Exports an NDMR Excel file for the specified NDMR report.
    /// </summary>
    public async Task<byte[]> ExportToExcelAsync(Guid ndmrId)
    {
        var ndmrReport = await _context.IrrRprts
            .Include(r => r.Facility)
            .Include(r => r.Company)
            .FirstOrDefaultAsync(r => r.Id == ndmrId);

        if (ndmrReport == null)
            throw new EntityNotFoundException(nameof(IrrRprt), ndmrId);

        var facility = ndmrReport.Facility;
        if (facility == null)
            throw new BusinessRuleException("Facility not found for this NDMR report.");

        var month = (int)ndmrReport.Month;
        var year = ndmrReport.Year;
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
            permit,
            ndmrReport.Month,
            year,
            "002",
            "00310",
            "BOD5 (mg/L)",
            wwChar?.FlowMeasuringPoint,
            wwChar?.ParameterMonitoringPoint);

        var permitTemplateRows = permit == null
            ? new List<FacilityPermitTemplateParameter>()
            : await _context.FacilityPermitTemplateParameters
                .Include(x => x.PcsParameterCatalog)
                .Where(x => x.FacilityPermitId == permit.Id && (x.ReportTypes & PermitTemplateReportTypeEnum.Ndmr) != 0)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.CreatedDate)
                .ThenBy(x => x.Id)
                .ToListAsync();
        permitTemplateRows = permitTemplateRows
            .Where(row => IsTemplateRowApplicableForMonth(row, month))
            .ToList();
        permitTemplateRows = permitTemplateRows
            .Where(row => IsTemplateRowApplicableForMonth(row, month))
            .ToList();

        if (permitTemplateRows.Any())
        {
            var hasFlow = permitTemplateRows.Any(x =>
                string.Equals(x.PcsParameterCatalog?.PcsCode, "50050", StringComparison.OrdinalIgnoreCase));
            if (!hasFlow)
            {
                throw new BusinessRuleException("Permit template must include PCS code 50050 (Flow) for NDMR export.");
            }
        }

        var parameterRows = permitTemplateRows
            .Select(row =>
            {
                var code = row.PcsParameterCatalog?.PcsCode ?? string.Empty;
                var name = row.ParameterDisplayOverride
                    ?? row.PcsParameterCatalog?.UserFriendlyName
                    ?? row.PcsParameterCatalog?.OfficialParameterName
                    ?? string.Empty;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = string.IsNullOrWhiteSpace(code) ? string.Empty : $"PCS {code}";
                }

                var units = NdmrFlowFormatting.IsFlowPcs(code)
                    ? NdmrFlowFormatting.FlowUnitsLabel
                    : row.UnitsOverride
                        ?? row.PcsParameterCatalog?.AcceptedUnits
                        ?? string.Empty;

                return new NdmrParameterRow
                {
                    TemplateRow = row,
                    PcsCode = code,
                    DisplayName = name,
                    Units = units
                };
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.PcsCode))
            .ToList();

        var defaultCodes = new List<string> { "50050", "00680", "78732", "82546", "00400", "00310", "00940", "50060", "31616", "00610", "00625", "00620", "00600", "00665", "70300", "00530" };
        if (parameterRows.Count == 0)
        {
            parameterRows = defaultCodes.Select(code => new NdmrParameterRow
            {
                TemplateRow = new FacilityPermitTemplateParameter
                {
                    SampleType = SampleTypeEnum.Grab,
                    MeasurementFrequency = MeasurementFrequencyEnum.Monthly
                },
                PcsCode = code,
                DisplayName = $"PCS {code}",
                Units = string.Empty
            }).ToList();
        }

        var wwCharTemplateValues = wwChar == null
            ? new List<WWCharTemplateValue>()
            : await _context.WWCharTemplateValues
                .Where(x => x.WWCharId == wwChar.Id && x.DayNo >= 1 && x.DayNo <= 31)
                .ToListAsync();
        var wwDailyValueByKey = wwCharTemplateValues
            .GroupBy(x => (x.FacilityPermitTemplateParameterId, x.DayNo))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First().NumericValue);

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
            .OrderByDescending(o => o.UpdatedDate ?? o.CreatedDate)
            .ThenByDescending(o => o.CreatedDate)
            .ToListAsync();

        var irrigationReport = ndmrReport;

        var parameterChunks = parameterRows
            .Select((row, idx) => new { row, idx })
            .GroupBy(x => x.idx / 16)
            .Select(g => g.Select(x => x.row).ToList())
            .ToList();
        if (parameterChunks.Count == 0)
        {
            parameterChunks.Add(new List<NdmrParameterRow>());
        }

        var allPpiSheets = new List<IXLWorksheet>();
        var firstSheet = flowWorksheet;
        firstSheet.Name = "PPI 001";
        allPpiSheets.Add(firstSheet);
        for (var chunkIndex = 1; chunkIndex < parameterChunks.Count; chunkIndex++)
        {
            var nextSheet = firstSheet.CopyTo($"PPI 001 ({chunkIndex + 1})");
            allPpiSheets.Add(nextSheet);
        }

        var codeSlots = new[] { "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S" };
        const int startRow = 6;
        const int codeRow = 3;
        const int nameRow = 4;
        const int unitsRow = 5;
        const int samplingTypeRow = 40;
        const int monthlyLimitRow = 41;
        const int dailyLimitRow = 42;
        const int sampleFrequencyRow = 43;

        for (var chunkIndex = 0; chunkIndex < parameterChunks.Count; chunkIndex++)
        {
            var worksheet = allPpiSheets[chunkIndex];
            var chunk = parameterChunks[chunkIndex];

            WriteStandardHeader(
                worksheet,
                facility,
                permit,
                ndmrReport.Month,
                year,
                "002",
                chunk.FirstOrDefault()?.PcsCode ?? "50050",
                chunk.FirstOrDefault()?.DisplayName ?? "Flow",
                wwChar?.FlowMeasuringPoint,
                wwChar?.ParameterMonitoringPoint);

            for (var slot = 0; slot < codeSlots.Length; slot++)
            {
                var col = codeSlots[slot];
                var codeCell = worksheet.Cell($"{col}{codeRow}");
                var nameCell = worksheet.Cell($"{col}{nameRow}");
                var unitCell = worksheet.Cell($"{col}{unitsRow}");
                var samplingCell = worksheet.Cell($"{col}{samplingTypeRow}");
                var monthlyLimitCell = worksheet.Cell($"{col}{monthlyLimitRow}");
                var dailyLimitCell = worksheet.Cell($"{col}{dailyLimitRow}");
                var frequencyCell = worksheet.Cell($"{col}{sampleFrequencyRow}");

                codeCell.Clear(XLClearOptions.Contents);
                nameCell.Clear(XLClearOptions.Contents);
                unitCell.Clear(XLClearOptions.Contents);
                samplingCell.Clear(XLClearOptions.Contents);
                monthlyLimitCell.Clear(XLClearOptions.Contents);
                dailyLimitCell.Clear(XLClearOptions.Contents);
                frequencyCell.Clear(XLClearOptions.Contents);

                if (slot >= chunk.Count)
                {
                    continue;
                }

                var parameter = chunk[slot];
                codeCell.Value = parameter.PcsCode;
                nameCell.Value = parameter.DisplayName;
                unitCell.Value = NdmrFlowFormatting.IsFlowPcs(parameter.PcsCode)
                    ? NdmrFlowFormatting.FlowUnitsLabel
                    : parameter.Units;
                samplingCell.Value = parameter.TemplateRow.SampleType.ToString();
                frequencyCell.Value = parameter.TemplateRow.MeasurementFrequency.ToDisplayLabel();
                monthlyLimitCell.Value = BuildMonthlyLimitText(parameter.TemplateRow, parameter.PcsCode);
                dailyLimitCell.Value = BuildDailyLimitText(parameter.TemplateRow, parameter.PcsCode);
            }

            for (int day = 1; day <= daysInMonth; day++)
            {
                var currentDate = new DateTime(year, month, day);
                var row = startRow + (day - 1);
                worksheet.Cell($"A{row}").Value = day;

                var dayOperatorLogs = operatorLogs
                    .Where(o => o.LogDate.Date == currentDate.Date)
                    .ToList();

                if (dayOperatorLogs.Any())
                {
                    var firstLog = dayOperatorLogs
                        .OrderBy(o => o.ArrivalTime)
                        .First();
                    var canonicalLog = dayOperatorLogs
                        .OrderByDescending(o => o.UpdatedDate ?? o.CreatedDate)
                        .ThenByDescending(o => o.CreatedDate)
                        .First();

                    worksheet.Cell($"B{row}").Value = firstLog.ArrivalTime;
                    worksheet.Cell($"B{row}").Style.NumberFormat.Format = "hh:mm";

                    worksheet.Cell($"C{row}").Value = canonicalLog.TimeOnSiteHours;
                    worksheet.Cell($"C{row}").Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    worksheet.Cell($"B{row}").Clear(XLClearOptions.Contents);
                    worksheet.Cell($"C{row}").Clear(XLClearOptions.Contents);
                }

                for (var slot = 0; slot < codeSlots.Length; slot++)
                {
                    var col = codeSlots[slot];
                    var valueCell = worksheet.Cell($"{col}{row}");
                    valueCell.Clear(XLClearOptions.Contents);

                    if (slot >= chunk.Count)
                    {
                        continue;
                    }

                    var parameter = chunk[slot];
                    decimal? value = null;
                    if (parameter.TemplateRow.Id != Guid.Empty &&
                        wwDailyValueByKey.TryGetValue((parameter.TemplateRow.Id, day), out var wwValue))
                    {
                        value = wwValue;
                    }

                    value ??= ResolveFallbackDailyValue(parameter.PcsCode, currentDate, wwChar, gwMonits);
                    if (value.HasValue)
                    {
                        if (NdmrFlowFormatting.IsFlowPcs(parameter.PcsCode))
                        {
                            value = NdmrFlowFormatting.NormalizeFlowValueToMgd(value);
                        }

                        valueCell.Value = value.Value;
                        valueCell.Style.NumberFormat.Format = NdmrFlowFormatting.IsFlowPcs(parameter.PcsCode)
                            ? NdmrFlowFormatting.FlowNumericFormat
                            : "0.00";
                    }
                }
            }

            PopulateSummaryRowsForChunk(worksheet, chunk, codeSlots, daysInMonth, startRow);
            SanitizeErrorCells(worksheet);
        }

        // Prevent ###### for high-count fecal values in summary cells.
        if (flowWorksheet.Column("F").Width < 11)
        {
            flowWorksheet.Column("F").Width = 11;
        }

        var labOption = await Gw59FacilityFieldResolver.ResolveLabOptionAsync(_context, wwChar?.LabOptionId, facility.CompanyId);
        var secondaryLabOption = await Gw59FacilityFieldResolver.ResolveLabOptionAsync(
            _context,
            wwChar?.SecondaryLabOptionId,
            facility.CompanyId);
        WriteCertificationPage(
            workbook,
            facility,
            permit,
            labOption,
            secondaryLabOption,
            wwChar,
            irrigationReport?.ComplianceStatus);

        // Keep NDMR output focused on PPI 001 chunk pages + required supporting sheets.
        // Remove legacy nitrogen PPI worksheets that are not part of the consolidated layout.
        foreach (var sheetName in new[] { "PPI_TKN", "PPI_NH3N", "PPI_NO3N", "PPI_TN" })
        {
            var legacySheet = workbook.Worksheets.FirstOrDefault(ws =>
                string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase));
            if (legacySheet != null)
            {
                workbook.Worksheets.Delete(legacySheet.Name);
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation(
            "Generated NDMR Excel report for facility {FacilityName} ({FacilityId}) for {Month}/{Year} based on NDMR report {ReportId}.",
            facility.Name,
            facility.Id,
            month,
            year,
            ndmrId);

        return stream.ToArray();
    }

    private static void WriteCertificationPage(
        IXLWorkbook workbook,
        Facility facility,
        FacilityPermit? permit,
        CompanyLabOption? labOption,
        CompanyLabOption? secondaryLabOption,
        WWChar? wwChar,
        ComplianceStatusEnum? complianceStatus)
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

        // Permittee certification section
        certificationWorksheet.Cell("M9").Value = facility.Permittee ?? string.Empty;
        certificationWorksheet.Cell("M10").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("N11").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("M12").Value = facility.PermitPhone ?? string.Empty;
        certificationWorksheet.Cell("R12").Value = permit?.EffectiveEndDate?.ToString("MM/dd/yyyy") ?? string.Empty;

        certificationWorksheet.Cell("C2").Value = wwChar?.SamplingPerson1 ?? string.Empty;
        certificationWorksheet.Cell("C3").Value = wwChar?.SamplingPerson2 ?? string.Empty;
        var labInfo = Gw59FacilityFieldResolver.ResolveLabInfo(facility, labOption);
        var secondaryLabInfo = Gw59FacilityFieldResolver.ResolveLabInfo(facility, secondaryLabOption);
        certificationWorksheet.Cell("L2").Value = labInfo.LabName;
        certificationWorksheet.Cell("L3").Value = secondaryLabInfo.LabName;
    }
}

using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.Services.Models;
using SAM.Utilities;
using System.Text.RegularExpressions;

namespace SAM.Services.Implementations;

/// <summary>
/// Service implementation for generating Non-Discharge Mass Loading Reports (NDMLR)
/// in Excel format using the client-provided NDMLR template.
/// Uses NDAR-1 report data plus GWMonit for average concentration; applies the
/// client formula: Monthly Load (lbs/ac) = (Volume gal × Avg Conc mg/L × 8.34e-6) / Area acres.
/// </summary>
public class NDMLRService : INDMLRService
{
    private const decimal MonthlyLoadConversionFactor = 8.34e-6m; // lbs / (gal * mg/L)

    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NDMLRService> _logger;
    private readonly IApplicationComplianceService _applicationComplianceService;

    public NDMLRService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<NDMLRService> logger,
        IApplicationComplianceService applicationComplianceService)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
        _applicationComplianceService = applicationComplianceService;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync(Guid ndmlrId)
    {
        var ndmlr = await _context.NDMLRs
            .Include(x => x.Facility)
            .FirstOrDefaultAsync(x => x.Id == ndmlrId);
        if (ndmlr == null)
            throw new EntityNotFoundException(nameof(NDMLR), ndmlrId);

        var facility = ndmlr.Facility;
        if (facility == null)
            throw new BusinessRuleException("Facility not found for this NDMLR report.");

        var year = ndmlr.Year;
        var (windowStart, windowEnd) = GetNdmlrWindow(year, ndmlr.Month);
        var monthKeys = BuildDescendingNdmlrWindowMonthKeys(year, ndmlr.Month);
        var startYear = windowStart.Year;
        var startMonth = windowStart.Month;
        var endYear = windowEnd.Year;
        var endMonth = windowEnd.Month;

        var templatePath = Path.Combine(
            _environment.WebRootPath,
            "forms",
            "Non-Discharge Mass Loading Report (NDMLR).xlsx");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        var ndarReports = await _context.NDAR1s
            .Where(r => r.CompanyId == ndmlr.CompanyId &&
                        r.FacilityId == ndmlr.FacilityId &&
                        (r.Year > startYear || (r.Year == startYear && (int)r.Month >= startMonth)) &&
                        (r.Year < endYear || (r.Year == endYear && (int)r.Month <= endMonth)))
            .Include(r => r.Field1).ThenInclude(f => f!.Crop)
            .Include(r => r.Field2).ThenInclude(f => f!.Crop)
            .Include(r => r.Field3).ThenInclude(f => f!.Crop)
            .Include(r => r.Field4).ThenInclude(f => f!.Crop)
            .Include(r => r.Fields)
                .ThenInclude(f => f.Sprayfield)
                    .ThenInclude(s => s!.Crop)
            .Include(r => r.Fields)
                .ThenInclude(f => f.DailyValues)
            .ToListAsync();

        var reportsByMonth = ndarReports
            .GroupBy(r => BuildMonthKey(r.Year, (int)r.Month))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedDate).First());

        var gwMonits = await _context.GWMonits
            .Where(g => g.FacilityId == facility.Id &&
                        g.SampleDate >= windowStart &&
                        g.SampleDate <= windowEnd)
            .ToListAsync();

        var fieldMetaById = new Dictionary<Guid, NdmlrFieldMeta>();
        foreach (var report in ndarReports)
        {
            await ResolveFieldsIfNeededAsync(report);

            if (report.Fields.Any())
            {
                foreach (var field in report.Fields.OrderBy(f => f.FieldOrder))
                {
                    if (field.Sprayfield == null)
                    {
                        continue;
                    }

                    var sprayfieldId = field.Sprayfield.Id;
                    if (!fieldMetaById.TryGetValue(sprayfieldId, out var existing))
                    {
                        fieldMetaById[sprayfieldId] = new NdmlrFieldMeta
                        {
                            Sprayfield = field.Sprayfield,
                            PreferredOrder = field.FieldOrder
                        };
                        continue;
                    }

                    if (!existing.PreferredOrder.HasValue || field.FieldOrder < existing.PreferredOrder.Value)
                    {
                        existing.PreferredOrder = field.FieldOrder;
                    }
                }
                continue;
            }

            foreach (var field in new[] { report.Field1, report.Field2, report.Field3, report.Field4 })
            {
                if (field == null)
                {
                    continue;
                }

                if (!fieldMetaById.ContainsKey(field.Id))
                {
                    fieldMetaById[field.Id] = new NdmlrFieldMeta
                    {
                        Sprayfield = field
                    };
                }
            }
        }

        var selectedFields = fieldMetaById.Values
            .OrderBy(x => x.PreferredOrder.HasValue ? 0 : 1)
            .ThenBy(x => x.PreferredOrder ?? int.MaxValue)
            .ThenBy(x => x.Sprayfield.FieldId)
            .Select(x => x.Sprayfield)
            .ToList();

        var monthlyVolumesByFieldByMonth = BuildMonthlyFieldVolumesByMonth(reportsByMonth);

        using var workbook = new XLWorkbook(templatePath);
        var reportTemplateSheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Page 1") ?? workbook.Worksheet(1);
        var certificationTemplate = GetCertificationTemplateSheet(workbook);
        RemovePreExistingDynamicNdmlrSheets(workbook);
        RemovePreExistingDynamicCertificationSheets(workbook);

        var chunks = selectedFields
            .Select((field, idx) => new { field, idx })
            .GroupBy(x => x.idx / 5)
            .Select(g => g.Select(x => x.field).ToList())
            .ToList();

        if (chunks.Count == 0)
        {
            chunks.Add(new List<Sprayfield>());
        }

        for (var chunkIndex = 0; chunkIndex < chunks.Count; chunkIndex++)
        {
            IXLWorksheet reportSheet;
            var reportSheetName = $"NDMLR ({chunkIndex + 1})";
            if (chunkIndex == 0)
            {
                reportSheet = reportTemplateSheet;
                reportSheet.Name = reportSheetName;
            }
            else
            {
                reportSheet = reportTemplateSheet.CopyTo(reportSheetName);
            }

            var chunk = chunks[chunkIndex];
            WriteHeader(reportSheet, facility, year, ndmlr.Month);
            WriteFieldBlocks(reportSheet, chunk, monthlyVolumesByFieldByMonth);
            WriteDataRows(reportSheet, chunk, monthlyVolumesByFieldByMonth, gwMonits, monthKeys);
            await WriteFooterAsync(reportSheet, ndmlr, chunk, windowEnd);

            if (certificationTemplate != null)
            {
                var certSheetName = $"Certification ({chunkIndex + 1})";
                var certSheet = certificationTemplate.CopyTo(certSheetName);
                WriteCertificationPage(certSheet, facility);
                certSheet.Position = reportSheet.Position + 1;
            }
        }

        DeleteSheetByName(workbook, "Certification Page");
        DeleteSheetByName(workbook, "Formulas");

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation(
            "Generated annual NDMLR Excel report for facility {FacilityName} ({FacilityId}) for year {Year} based on NDMLR record {ReportId}.",
            facility.Name,
            facility.Id,
            year,
            ndmlrId);

        return stream.ToArray();
    }

    /// <summary>
    /// Loads Sprayfield (with zone crops) for any FieldNId that is set but FieldN is null,
    /// so field blocks and area can be populated even when the initial Include didn't load them.
    /// </summary>
    private async Task ResolveFieldsIfNeededAsync(NDAR1 report)
    {
        var ids = new[] { report.Field1Id, report.Field2Id, report.Field3Id, report.Field4Id };
        var fields = new[] { report.Field1, report.Field2, report.Field3, report.Field4 };
        for (int i = 0; i < 4; i++)
        {
            if (ids[i].HasValue && fields[i] == null)
            {
                var sprayfield = await _context.Sprayfields
                    .Include(s => s.Crop)
                    .FirstOrDefaultAsync(s => s.Id == ids[i]!.Value);
                switch (i)
                {
                    case 0: report.Field1 = sprayfield; break;
                    case 1: report.Field2 = sprayfield; break;
                    case 2: report.Field3 = sprayfield; break;
                    case 3: report.Field4 = sprayfield; break;
                }
            }
        }
    }

    private static decimal? ComputeAverageTotalNMgl(List<GWMonit> gwMonits)
    {
        var values = gwMonits
            .Select(g =>
            {
                if (!g.TKN.HasValue && !g.NO3N.HasValue)
                    return (decimal?)null;
                return (g.TKN ?? 0m) + (g.NO3N ?? 0m);
            })
            .Where(v => v.HasValue)
            .ToList();
        if (values.Count == 0)
            return null;
        return values.Average();
    }

    private static void WriteHeader(IXLWorksheet worksheet, Facility facility, int year, MonthEnum month)
    {
        worksheet.Cell("C1").Value = facility.PermitNumber ?? "";
        worksheet.Cell("G1").Value = facility.Name ?? "";
        worksheet.Cell("O1").Value = facility.County ?? "";
        worksheet.Cell("S1").Value = month.ToString();
        worksheet.Cell("V1").Value = year;
    }

    private static void WriteFieldBlocks(IXLWorksheet worksheet, List<Sprayfield> selectedFields, Dictionary<int, Dictionary<Guid, decimal>> monthlyVolumesByFieldByMonth)
    {
        var yearlyVolumeByFieldId = new Dictionary<Guid, decimal>();
        foreach (var monthly in monthlyVolumesByFieldByMonth.Values)
        {
            foreach (var kvp in monthly)
            {
                if (!yearlyVolumeByFieldId.ContainsKey(kvp.Key))
                {
                    yearlyVolumeByFieldId[kvp.Key] = 0m;
                }
                yearlyVolumeByFieldId[kvp.Key] += kvp.Value;
            }
        }

        var valueCols = new[] { "E", "I", "M", "Q", "U" };

        for (int i = 0; i < valueCols.Length; i++)
        {
            var col = valueCols[i];
            var field = i < selectedFields.Count ? selectedFields[i] : null;
            if (field != null)
            {
                worksheet.Cell($"{col}2").Value = field.FieldId;
                worksheet.Cell($"{col}3").Value = SprayfieldReportHelper.GetReportAcres(field);
                worksheet.Cell($"{col}4").Value = SprayfieldZoneSummaryHelper.GetCropSummary(field) ?? "";
                var isFieldLoaded = yearlyVolumeByFieldId.TryGetValue(field.Id, out var fieldVol) && fieldVol > 0;
                if (i == 0)
                {
                    // Block 1 has separate YES/NO cells per template.
                    var yesCell = worksheet.Cell("E6");
                    var noCell = worksheet.Cell("F6");
                    yesCell.Value = isFieldLoaded ? "☑ YES" : "☐ YES";
                    noCell.Value = isFieldLoaded ? "☐ NO" : "☑ NO";
                    yesCell.Style.Font.Bold = false;
                    noCell.Style.Font.Bold = false;
                }
                else
                {
                    var fieldLoadedCell = worksheet.Cell($"{col}6");
                    fieldLoadedCell.Value = isFieldLoaded ? "☑ YES    ☐ NO" : "☐ YES    ☑ NO";
                    fieldLoadedCell.Style.Font.Bold = false;
                }
            }
            else
            {
                foreach (var row in new[] { 2, 3, 4, 6 })
                {
                    worksheet.Cell($"{col}{row}").Clear(XLClearOptions.Contents);
                }
            }
        }
    }

    private static void WriteDataRows(IXLWorksheet worksheet, List<Sprayfield> selectedFields, Dictionary<int, Dictionary<Guid, decimal>> monthlyVolumesByFieldByMonth, List<GWMonit> gwMonits, List<int> monthKeys)
    {
        var runningTotals = new decimal[5];
        var colSets = new[] { ("C", "D", "E", "F"), ("G", "H", "I", "J"), ("K", "L", "M", "N"), ("O", "P", "Q", "R"), ("S", "T", "U", "V") };

        for (int index = 0; index < monthKeys.Count; index++)
        {
            var monthKey = monthKeys[index];
            var (monthYear, monthNo) = ParseMonthKey(monthKey);
            var row = 9 + index;
            var monthDate = new DateTime(monthYear, monthNo, 1);
            worksheet.Cell($"A{row}").Value = monthDate;
            worksheet.Cell($"B{row}").Value = monthDate.ToString("MMMM");
            var monthAvgConc = ComputeAverageTotalNMgl(gwMonits.Where(g => g.SampleDate.Year == monthYear && g.SampleDate.Month == monthNo).ToList());
            monthlyVolumesByFieldByMonth.TryGetValue(monthKey, out var monthVolumes);

            for (int i = 0; i < 5; i++)
            {
                var (volCol, concCol, monthlyCol, cumulCol) = colSets[i];
                var field = i < selectedFields.Count ? selectedFields[i] : null;
                var volume = 0m;
                if (field != null && monthVolumes != null && monthVolumes.TryGetValue(field.Id, out var mappedVolume))
                {
                    volume = mappedVolume;
                }
                var area = field != null ? SprayfieldReportHelper.GetReportAcres(field) : 0m;

                if (volume > 0)
                    worksheet.Cell($"{volCol}{row}").Value = volume;
                else
                    worksheet.Cell($"{volCol}{row}").Clear(XLClearOptions.Contents);

                if (monthAvgConc.HasValue && volume > 0)
                {
                    worksheet.Cell($"{concCol}{row}").Value = monthAvgConc.Value;
                    worksheet.Cell($"{concCol}{row}").Style.NumberFormat.Format = "0.00";
                }
                else
                    worksheet.Cell($"{concCol}{row}").Clear(XLClearOptions.Contents);

                if (field != null && area > 0 && monthAvgConc.HasValue && volume > 0)
                {
                    var monthlyLoad = (volume * monthAvgConc.Value * MonthlyLoadConversionFactor) / area;
                    runningTotals[i] += monthlyLoad;
                    worksheet.Cell($"{monthlyCol}{row}").Value = monthlyLoad;
                    worksheet.Cell($"{cumulCol}{row}").Value = runningTotals[i];
                    worksheet.Cell($"{monthlyCol}{row}").Style.NumberFormat.Format = "0.00";
                    worksheet.Cell($"{cumulCol}{row}").Style.NumberFormat.Format = "0.00";
                }
                else
                {
                    worksheet.Cell($"{monthlyCol}{row}").Clear(XLClearOptions.Contents);
                    worksheet.Cell($"{cumulCol}{row}").Clear(XLClearOptions.Contents);
                }
            }
        }
    }

    private async Task WriteFooterAsync(IXLWorksheet worksheet, NDMLR report, List<Sprayfield> selectedFields, DateTime asOfDate)
    {
        var valueCols = new[] { "E", "I", "M", "Q", "U" };

        for (int i = 0; i < 5; i++)
        {
            var fieldId = i < selectedFields.Count ? selectedFields[i].Id : Guid.Empty;
            if (fieldId == Guid.Empty)
            {
                // Keep template styling, but clear stale values for unused field slots.
                worksheet.Cell($"{valueCols[i]}21").Clear(XLClearOptions.Contents);
                worksheet.Cell($"{valueCols[i]}22").Clear(XLClearOptions.Contents);
                continue;
            }

            FieldRollingMetricsResult metrics;
            try
            {
                metrics = await _applicationComplianceService.GetFieldRollingMetricsAsync(report.FacilityId, fieldId, asOfDate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not compute rolling metrics for sprayfield {SprayfieldId} in NDMLR export {ReportId}.", fieldId, report.Id);
                continue;
            }

            // Row 21: 12-month floating PAN load (lbs/ac/yr).
            worksheet.Cell($"{valueCols[i]}21").Value = metrics.RollingPanLbsPerAcre;
            worksheet.Cell($"{valueCols[i]}21").Style.NumberFormat.Format = "0.00";

            // Row 22: Annual PAN load limit (lbs/ac/yr).
            if (metrics.PanLimitLbsPerAcre.HasValue)
            {
                worksheet.Cell($"{valueCols[i]}22").Value = metrics.PanLimitLbsPerAcre.Value;
                worksheet.Cell($"{valueCols[i]}22").Style.NumberFormat.Format = "0.00";
            }
            else
            {
                worksheet.Cell($"{valueCols[i]}22").Clear(XLClearOptions.Contents);
            }
        }
    }

    private static Dictionary<int, Dictionary<Guid, decimal>> BuildMonthlyFieldVolumesByMonth(Dictionary<int, NDAR1> reportsByMonth)
    {
        var result = new Dictionary<int, Dictionary<Guid, decimal>>();
        foreach (var (month, report) in reportsByMonth)
        {
            var monthly = new Dictionary<Guid, decimal>();

            if (report.Fields.Any())
            {
                foreach (var field in report.Fields.OrderBy(f => f.FieldOrder))
                {
                    if (field.SprayfieldId == Guid.Empty)
                    {
                        continue;
                    }

                    var sum = field.DailyValues?.Sum(v => v.VolumeApplied ?? 0m) ?? 0m;
                    if (!monthly.ContainsKey(field.SprayfieldId))
                    {
                        monthly[field.SprayfieldId] = 0m;
                    }
                    monthly[field.SprayfieldId] += sum;
                }
            }
            else
            {
                AccumulateLegacyFieldVolume(monthly, report.Field1Id, report.Field1VolumeAppliedDaily);
                AccumulateLegacyFieldVolume(monthly, report.Field2Id, report.Field2VolumeAppliedDaily);
                AccumulateLegacyFieldVolume(monthly, report.Field3Id, report.Field3VolumeAppliedDaily);
                AccumulateLegacyFieldVolume(monthly, report.Field4Id, report.Field4VolumeAppliedDaily);
            }

            result[month] = monthly;
        }

        return result;
    }

    private static void AccumulateLegacyFieldVolume(Dictionary<Guid, decimal> monthly, Guid? sprayfieldId, List<decimal?>? dailyVolumes)
    {
        if (!sprayfieldId.HasValue)
        {
            return;
        }

        var sum = dailyVolumes?.Sum(v => v ?? 0m) ?? 0m;
        if (!monthly.ContainsKey(sprayfieldId.Value))
        {
            monthly[sprayfieldId.Value] = 0m;
        }
        monthly[sprayfieldId.Value] += sum;
    }

    private static int BuildMonthKey(int year, int month) => (year * 100) + month;

    private static (int Year, int Month) ParseMonthKey(int monthKey) => (monthKey / 100, monthKey % 100);

    private static List<int> BuildDescendingNdmlrWindowMonthKeys(int year, MonthEnum month)
    {
        var result = new List<int>(12);
        var cursor = new DateTime(year, (int)month, 1);
        for (var i = 0; i < 12; i++)
        {
            result.Add(BuildMonthKey(cursor.Year, cursor.Month));
            cursor = cursor.AddMonths(-1);
        }

        return result;
    }

    private static (DateTime WindowStart, DateTime WindowEnd) GetNdmlrWindow(int year, MonthEnum month)
    {
        var endMonthStart = new DateTime(year, (int)month, 1);
        var windowStart = endMonthStart.AddMonths(-11);
        var windowEnd = endMonthStart.AddMonths(1).AddDays(-1);
        return (windowStart, windowEnd);
    }

    private static IXLWorksheet? GetCertificationTemplateSheet(IXLWorkbook workbook)
    {
        var byName = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, "Certification Page", StringComparison.OrdinalIgnoreCase));
        if (byName != null)
        {
            return byName;
        }

        return workbook.Worksheets.Count >= 2 ? workbook.Worksheet(2) : null;
    }

    private static void RemovePreExistingDynamicNdmlrSheets(IXLWorkbook workbook)
    {
        var staleSheets = workbook.Worksheets
            .Where(ws => Regex.IsMatch(ws.Name, @"^NDMLR \(\d+\)$", RegexOptions.IgnoreCase))
            .ToList();

        foreach (var sheet in staleSheets)
        {
            sheet.Delete();
        }
    }

    private static void RemovePreExistingDynamicCertificationSheets(IXLWorkbook workbook)
    {
        var staleSheets = workbook.Worksheets
            .Where(ws => Regex.IsMatch(ws.Name, @"^Certification \(\d+\)$", RegexOptions.IgnoreCase))
            .ToList();

        foreach (var sheet in staleSheets)
        {
            sheet.Delete();
        }
    }

    private static void DeleteSheetByName(IXLWorkbook workbook, string sheetName)
    {
        var sheet = workbook.Worksheets
            .FirstOrDefault(ws => string.Equals(ws.Name, sheetName, StringComparison.OrdinalIgnoreCase));
        sheet?.Delete();
    }

    private static void WriteCertificationPage(IXLWorksheet certificationWorksheet, Facility facility)
    {
        certificationWorksheet.Cell("C6").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("E7").Value = facility.OperatorNumber ?? string.Empty;
        certificationWorksheet.Cell("C8").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("I8").Value = facility.OperatorPhone ?? string.Empty;
        certificationWorksheet.Cell("B9").Value = $"Has the ORC changed since the previous NDMLR? {(facility.ChangeInOrc == true ? "Yes" : "No")}";
        certificationWorksheet.Cell("K10").Value = DateTime.Today.ToString("MM/dd/yyyy");

        // Permittee certification section
        certificationWorksheet.Cell("O6").Value = facility.Permittee ?? string.Empty;
        certificationWorksheet.Cell("P7").Value = facility.OrcName ?? string.Empty;
        certificationWorksheet.Cell("P8").Value = facility.OperatorGrade ?? string.Empty;
        certificationWorksheet.Cell("O9").Value = facility.PermitPhone ?? string.Empty;
        certificationWorksheet.Cell("T9").Value = facility.PermitExpirationDate?.ToString("MM/dd/yyyy") ?? string.Empty;
        certificationWorksheet.Cell("U10").Value = DateTime.Today.ToString("MM/dd/yyyy");
    }

    private sealed class NdmlrFieldMeta
    {
        public Sprayfield Sprayfield { get; set; } = null!;
        public int? PreferredOrder { get; set; }
    }
}

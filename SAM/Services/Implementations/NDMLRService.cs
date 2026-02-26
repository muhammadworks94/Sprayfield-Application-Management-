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

    public NDMLRService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        ILogger<NDMLRService> logger)
    {
        _context = context;
        _environment = environment;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> ExportToExcelAsync(Guid ndar1Id)
    {
        var report = await _context.NDAR1s
            .Include(r => r.Facility)
            .Include(r => r.Field1).ThenInclude(f => f!.Crop)
            .Include(r => r.Field2).ThenInclude(f => f!.Crop)
            .Include(r => r.Field3).ThenInclude(f => f!.Crop)
            .Include(r => r.Field4).ThenInclude(f => f!.Crop)
            .FirstOrDefaultAsync(r => r.Id == ndar1Id);

        if (report == null)
            throw new EntityNotFoundException(nameof(NDAR1), ndar1Id);

        var facility = report.Facility;
        if (facility == null)
            throw new BusinessRuleException("Facility not found for this NDAR-1 report.");

        var month = (int)report.Month;
        var year = report.Year;
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var templatePath = Path.Combine(
            _environment.WebRootPath,
            "forms",
            "Non-Discharge Mass Loading Report (NDMLR).xlsx");

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template file not found: {templatePath}");

        var gwMonits = await _context.GWMonits
            .Where(g => g.FacilityId == facility.Id &&
                        g.SampleDate >= startDate &&
                        g.SampleDate <= endDate)
            .ToListAsync();

        decimal? avgConcMgL = ComputeAverageTotalNMgl(gwMonits);

        // Resolve Sprayfields when FieldNId is set but navigation is null (e.g. Include didn't load)
        await ResolveFieldsIfNeededAsync(report);

        using var workbook = new XLWorkbook(templatePath);
        var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == "Page 1") ?? workbook.Worksheet(1);

        WriteHeader(worksheet, facility, report.Month, year);
        WriteFieldBlocks(worksheet, report);
        WriteDataRow(worksheet, report, avgConcMgL, 9);
        WriteFooter(worksheet, report);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        _logger.LogInformation(
            "Generated NDMLR Excel report for facility {FacilityName} ({FacilityId}) for {Month}/{Year} based on NDAR-1 report {ReportId}.",
            facility.Name,
            facility.Id,
            month,
            year,
            ndar1Id);

        return stream.ToArray();
    }

    /// <summary>
    /// Loads Sprayfield (with Crop) for any FieldNId that is set but FieldN is null,
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

    private static void WriteHeader(IXLWorksheet worksheet, Facility facility, MonthEnum monthEnum, int year)
    {
        worksheet.Cell("C1").Value = facility.PermitNumber ?? "";
        worksheet.Cell("G1").Value = facility.Name ?? "";
        worksheet.Cell("O1").Value = facility.County ?? "";
        worksheet.Cell("S1").Value = monthEnum.ToString();
        worksheet.Cell("V1").Value = year;
    }

    private static void WriteFieldBlocks(IXLWorksheet worksheet, NDAR1 report)
    {
        var fields = new[] { report.Field1, report.Field2, report.Field3, report.Field4 };
        var fieldVolumeSums = new[]
        {
            report.Field1VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0,
            report.Field2VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0,
            report.Field3VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0,
            report.Field4VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0,
        };
        var fieldIds = new[] { report.Field1Id, report.Field2Id, report.Field3Id, report.Field4Id };

        // Block 1: value col E, Field Loaded? in F6; Block 2-4: value cols I, M, Q (rows 2-6)
        var valueCols = new[] { "E", "I", "M", "Q" };

        for (int i = 0; i < fields.Length; i++)
        {
            var col = valueCols[i];
            if (fields[i] != null)
            {
                worksheet.Cell($"{col}2").Value = fields[i]!.FieldId;
                worksheet.Cell($"{col}3").Value = fields[i]!.SizeAcres;
                worksheet.Cell($"{col}4").Value = fields[i]!.Crop?.Name ?? "";
                worksheet.Cell($"{col}5").Value = "Wastewater"; // Load Type constant per plan
                var fieldLoaded = fieldIds[i].HasValue && fieldVolumeSums[i] > 0 ? "Yes" : "No";
                if (i == 0)
                    worksheet.Cell("F6").Value = fieldLoaded; // Block 1: E6 and F6 separate per template
                else
                    worksheet.Cell($"{col}6").Value = fieldLoaded;
            }
        }
    }

    private static void WriteDataRow(IXLWorksheet worksheet, NDAR1 report, decimal? avgConcMgL, int row)
    {
        var startDate = new DateTime(report.Year, (int)report.Month, 1);
        worksheet.Cell($"A{row}").Value = startDate;
        worksheet.Cell($"B{row}").Value = report.Month.ToString();

        var fields = new[] { report.Field1, report.Field2, report.Field3, report.Field4 };
        var volumeSums = new[]
        {
            report.Field1VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0m,
            report.Field2VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0m,
            report.Field3VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0m,
            report.Field4VolumeAppliedDaily?.Sum(x => x ?? 0) ?? 0m,
        };
        // Per-field columns: Block 1 C,D,E,F; Block 2 G,H,I,J; Block 3 K,L,M,N; Block 4 O,P,Q,R
        var colSets = new[] { ("C", "D", "E", "F"), ("G", "H", "I", "J"), ("K", "L", "M", "N"), ("O", "P", "Q", "R") };

        for (int i = 0; i < 4; i++)
        {
            var (volCol, concCol, monthlyCol, cumulCol) = colSets[i];
            var vol = volumeSums[i];
            var area = fields[i]?.SizeAcres ?? 0;

            if (vol > 0)
                worksheet.Cell($"{volCol}{row}").Value = vol;
            else
                worksheet.Cell($"{volCol}{row}").Clear(XLClearOptions.Contents);
            if (avgConcMgL.HasValue)
                worksheet.Cell($"{concCol}{row}").Value = avgConcMgL.Value;
            else
                worksheet.Cell($"{concCol}{row}").Clear(XLClearOptions.Contents);

            if (area > 0 && avgConcMgL.HasValue && vol > 0)
            {
                var monthlyLoad = (vol * avgConcMgL.Value * MonthlyLoadConversionFactor) / area;
                worksheet.Cell($"{monthlyCol}{row}").Value = monthlyLoad;
                worksheet.Cell($"{cumulCol}{row}").Value = monthlyLoad;
            }
            else
            {
                // Clear cells when we don't have complete data so template formulas don't show #DIV/0!
                worksheet.Cell($"{monthlyCol}{row}").Clear(XLClearOptions.Contents);
                worksheet.Cell($"{cumulCol}{row}").Clear(XLClearOptions.Contents);
            }
        }
    }

    private static void WriteFooter(IXLWorksheet worksheet, NDAR1 report)
    {
        // Row 21: 12 Month Floating Load (lbs/ac/yr) - NDAR1 stores inches; template wants lbs/ac/yr. Write inch value for now.
        // Row 22: Annual Load Limit - not in entities; leave empty (E22, I22, M22, Q22).
        var fields = new[] { report.Field1, report.Field2, report.Field3, report.Field4 };
        var twelveMonthInches = new[]
        {
            report.Field1TwelveMonthFloatingTotal,
            report.Field2TwelveMonthFloatingTotal,
            report.Field3TwelveMonthFloatingTotal,
            report.Field4TwelveMonthFloatingTotal,
        };
        var valueCols = new[] { "E", "I", "M", "Q" };
        for (int i = 0; i < 4; i++)
        {
            if (fields[i] != null)
                worksheet.Cell($"{valueCols[i]}21").Value = twelveMonthInches[i]; // inches; conversion to lbs/ac/yr can be added later
        }
    }
}

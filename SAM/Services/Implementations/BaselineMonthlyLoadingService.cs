using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Utilities;
using SAM.ViewModels.CompanyManagement;

namespace SAM.Services.Implementations;

public class BaselineMonthlyLoadingService : IBaselineMonthlyLoadingService
{
    private const string WorksheetName = "Baseline Loading";
    private const string FacilityLabel = "Facility";
    private const string MonthHeaderLabel = "Month";

    private readonly ApplicationDbContext _context;
    private readonly IMonthlyLoadingResolutionService _monthlyLoadingResolution;
    private readonly ISprayfieldService _sprayfieldService;

    public BaselineMonthlyLoadingService(
        ApplicationDbContext context,
        IMonthlyLoadingResolutionService monthlyLoadingResolution,
        ISprayfieldService sprayfieldService)
    {
        _context = context;
        _monthlyLoadingResolution = monthlyLoadingResolution;
        _sprayfieldService = sprayfieldService;
    }

    public async Task<NewClientSetupBaselineViewModel> GetWizardGridAsync(
        Guid companyId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company == null)
        {
            throw new EntityNotFoundException(nameof(Company), companyId);
        }

        var facilities = await _context.Facilities
            .AsNoTracking()
            .Where(f => f.CompanyId == companyId)
            .OrderBy(f => f.Name)
            .ToListAsync(cancellationToken);

        var historicalMonths = BaselineWindowHelper
            .GetHistoricalMonths(throughYear, throughMonth)
            .ToList();

        var sprayfields = await _context.Sprayfields
            .AsNoTracking()
            .Where(s => s.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var sprayfieldsByFacility = sprayfields
            .Where(s => s.FacilityId.HasValue)
            .GroupBy(s => s.FacilityId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderBy(s => int.TryParse(s.FieldId, out var order) ? order : int.MaxValue)
                    .ThenBy(s => s.FieldId)
                    .ToList());

        var monthKeys = historicalMonths
            .Select(m => m.Year * 100 + m.Month)
            .ToHashSet();

        var baselineLookup = await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .Where(b => monthKeys.Contains(b.Year * 100 + b.Month))
            .Select(b => new { b.FacilityId, b.SprayfieldId, b.Year, b.Month, b.LoadingInches })
            .ToListAsync(cancellationToken);

        var baselinesByKey = baselineLookup.ToDictionary(
            b => (b.FacilityId, b.SprayfieldId, b.Year, b.Month),
            b => (decimal?)b.LoadingInches);

        var oldest = historicalMonths[^1];
        var newest = historicalMonths[0];
        var rangeStart = new DateTime(oldest.Year, oldest.Month, 1);
        var rangeEndExclusive = new DateTime(newest.Year, newest.Month, 1).AddMonths(1);

        var facilityIds = facilities.Select(f => f.Id).ToList();
        var gallonsLookup = await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a => facilityIds.Contains(a.FacilityId)
                        && a.ApplicationDate >= rangeStart
                        && a.ApplicationDate < rangeEndExclusive)
            .GroupBy(a => new
            {
                a.FacilityId,
                a.SprayfieldId,
                Year = a.ApplicationDate.Year,
                Month = a.ApplicationDate.Month
            })
            .Select(g => new
            {
                g.Key.FacilityId,
                g.Key.SprayfieldId,
                g.Key.Year,
                g.Key.Month,
                Gallons = g.Sum(x => x.VolumeGallons),
                HasRealVolume = g.Any(x => x.VolumeGallons > 0m)
            })
            .ToListAsync(cancellationToken);

        var gallonsByKey = gallonsLookup.ToDictionary(
            g => (g.FacilityId, g.SprayfieldId, g.Year, g.Month),
            g => (g.Gallons, g.HasRealVolume));

        var facilityGroups = new List<ClientSetupFacilityGroupViewModel>();

        foreach (var facility in facilities)
        {
            var facilitySprayfields = sprayfieldsByFacility.TryGetValue(facility.Id, out var list)
                ? list
                : new List<Sprayfield>();

            var monthRows = BuildWizardMonthRowsFromLookups(
                facility.Id,
                facilitySprayfields,
                historicalMonths,
                baselinesByKey,
                gallonsByKey);

            facilityGroups.Add(new ClientSetupFacilityGroupViewModel
            {
                FacilityId = facility.Id,
                FacilityName = facility.Name,
                SprayfieldColumns = facilitySprayfields.Select(s => new ClientSetupSprayfieldColumnViewModel
                {
                    SprayfieldId = s.Id,
                    FieldCode = s.FieldId,
                    Acres = SprayfieldReportHelper.GetReportAcres(s)
                }).ToList(),
                MonthRows = monthRows
            });
        }

        var savedCellCount = await CountSavedBaselineCellsAsync(companyId, cancellationToken);

        return new NewClientSetupBaselineViewModel
        {
            CompanyId = companyId,
            CompanyName = company.Name,
            ThroughYear = throughYear,
            ThroughMonth = throughMonth,
            FacilityGroups = facilityGroups,
            SavedCellCount = savedCellCount
        };
    }

    public async Task SaveSetupCellAsync(
        Guid facilityId,
        Guid sprayfieldId,
        int throughYear,
        int throughMonth,
        int year,
        int month,
        decimal? loadingInches,
        string userId,
        CancellationToken cancellationToken = default)
    {
        await SaveCellsAsync(
            facilityId,
            throughYear,
            throughMonth,
            new[]
            {
                new ClientSetupCellSaveRequest
                {
                    SprayfieldId = sprayfieldId,
                    Year = year,
                    Month = month,
                    LoadingInches = loadingInches
                }
            },
            userId,
            cancellationToken);
    }

    public async Task<int> CountSavedBaselineCellsAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && x.LoadingInches > 0m)
            .CountAsync(cancellationToken);
    }

    public async Task<byte[]> BuildTemplateAsync(
        Guid companyId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var grid = await GetWizardGridAsync(companyId, throughYear, throughMonth, cancellationToken);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(WorksheetName);
        var row = 1;

        foreach (var group in grid.FacilityGroups)
        {
            sheet.Cell(row, 1).Value = FacilityLabel;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            sheet.Cell(row, 2).Value = group.FacilityName;
            sheet.Cell(row, 2).Style.Font.Bold = true;
            row++;

            sheet.Cell(row, 1).Value = MonthHeaderLabel;
            sheet.Cell(row, 1).Style.Font.Bold = true;
            for (var col = 0; col < group.SprayfieldColumns.Count; col++)
            {
                sheet.Cell(row, col + 2).Value = group.SprayfieldColumns[col].FieldCode;
                sheet.Cell(row, col + 2).Style.Font.Bold = true;
            }

            row++;

            foreach (var monthRow in group.MonthRows)
            {
                sheet.Cell(row, 1).Value = monthRow.Label;
                for (var col = 0; col < monthRow.Cells.Count; col++)
                {
                    var inches = monthRow.Cells[col].LoadingInches;
                    if (inches.HasValue && inches.Value > 0m)
                    {
                        sheet.Cell(row, col + 2).Value = Math.Round(inches.Value, 2, MidpointRounding.AwayFromZero);
                    }
                }

                row++;
            }

            row++; // blank row between facility blocks
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<BaselineImportResult> ImportFromExcelAsync(
        Guid companyId,
        int throughYear,
        int throughMonth,
        Stream excel,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = new BaselineImportResult();

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null)
        {
            throw new EntityNotFoundException(nameof(Company), companyId);
        }

        var facilities = await _context.Facilities
            .Where(f => f.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var facilityByName = facilities
            .GroupBy(f => f.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sprayfieldsByFacility = new Dictionary<Guid, Dictionary<string, Sprayfield>>();
        foreach (var facility in facilities)
        {
            var sprayfields = await _sprayfieldService.GetByFacilityIdAsync(facility.Id);
            sprayfieldsByFacility[facility.Id] = sprayfields
                .GroupBy(s => s.FieldId.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }

        var allowedMonths = BaselineWindowHelper
            .GetHistoricalMonths(throughYear, throughMonth)
            .ToHashSet();

        var monthLabelLookup = allowedMonths.ToDictionary(
            m => new DateTime(m.Year, m.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            m => m,
            StringComparer.OrdinalIgnoreCase);

        using var workbook = new XLWorkbook(excel);
        var sheet = workbook.Worksheets.FirstOrDefault(w =>
                       string.Equals(w.Name, WorksheetName, StringComparison.OrdinalIgnoreCase))
                   ?? workbook.Worksheets.First();

        var usedRange = sheet.RangeUsed();
        if (usedRange == null)
        {
            result.Errors.Add("The Excel file has no data.");
            return result;
        }

        var lastRow = usedRange.LastRow().RowNumber();
        var row = 1;

        while (row <= lastRow)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var col1 = GetCellText(sheet.Cell(row, 1));
            if (string.IsNullOrWhiteSpace(col1))
            {
                row++;
                continue;
            }

            if (!string.Equals(col1, FacilityLabel, StringComparison.OrdinalIgnoreCase))
            {
                row++;
                continue;
            }

            var facilityName = GetCellText(sheet.Cell(row, 2));
            if (string.IsNullOrWhiteSpace(facilityName))
            {
                result.Errors.Add($"Row {row}: Facility name is missing.");
                row++;
                continue;
            }

            if (!facilityByName.TryGetValue(facilityName.Trim(), out var facility))
            {
                result.Errors.Add($"Row {row}: Unknown facility '{facilityName}'.");
                // Skip this block: advance past header + up to 11 month rows or until next Facility
                row++;
                while (row <= lastRow)
                {
                    var peek = GetCellText(sheet.Cell(row, 1));
                    if (string.Equals(peek, FacilityLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    row++;
                }

                continue;
            }

            row++; // move to Month header
            if (row > lastRow || !string.Equals(GetCellText(sheet.Cell(row, 1)), MonthHeaderLabel, StringComparison.OrdinalIgnoreCase))
            {
                result.Errors.Add($"Facility '{facilityName}': expected a '{MonthHeaderLabel}' header row after the facility name.");
                continue;
            }

            var fieldCodes = new List<string>();
            var lastCol = Math.Max(usedRange.LastColumn().ColumnNumber(), 2);
            for (var col = 2; col <= lastCol; col++)
            {
                var fieldCode = GetCellText(sheet.Cell(row, col));
                if (string.IsNullOrWhiteSpace(fieldCode))
                {
                    break;
                }

                fieldCodes.Add(fieldCode.Trim());
            }

            if (fieldCodes.Count == 0)
            {
                result.Errors.Add($"Facility '{facilityName}': no field columns found on the Month header row.");
                row++;
                continue;
            }

            var fieldLookup = sprayfieldsByFacility[facility.Id];
            row++; // first month data row

            for (var monthIndex = 0; monthIndex < BaselineWindowHelper.HistoricalMonthCount && row <= lastRow; monthIndex++, row++)
            {
                var monthLabel = GetCellText(sheet.Cell(row, 1));
                if (string.IsNullOrWhiteSpace(monthLabel)
                    || string.Equals(monthLabel, FacilityLabel, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (!monthLabelLookup.TryGetValue(monthLabel.Trim(), out var monthKey))
                {
                    // Try parse "MMMM yyyy" even if not in window, then count as out-of-window
                    if (DateTime.TryParseExact(
                            monthLabel.Trim(),
                            "MMMM yyyy",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out var parsedMonth))
                    {
                        result.SkippedOutOfWindowCount += fieldCodes.Count;
                    }
                    else
                    {
                        result.Errors.Add($"Facility '{facilityName}', row {row}: unrecognized month label '{monthLabel}'.");
                    }

                    continue;
                }

                if (!allowedMonths.Contains(monthKey))
                {
                    result.SkippedOutOfWindowCount += fieldCodes.Count;
                    continue;
                }

                for (var col = 0; col < fieldCodes.Count; col++)
                {
                    var fieldCode = fieldCodes[col];
                    if (!fieldLookup.TryGetValue(fieldCode, out var sprayfield))
                    {
                        result.Errors.Add($"Facility '{facilityName}': unknown field '{fieldCode}'.");
                        continue;
                    }

                    var cellValue = sheet.Cell(row, col + 2);
                    var loadingInches = ParseLoadingInches(cellValue);

                    if (await _monthlyLoadingResolution.HasRealOperationalMonthAsync(
                            facility.Id,
                            sprayfield.Id,
                            monthKey.Year,
                            monthKey.Month,
                            cancellationToken))
                    {
                        result.SkippedOperationalMonthCount++;
                        continue;
                    }

                    var existing = await _context.SprayfieldBaselineMonthlyLoadings
                        .FirstOrDefaultAsync(
                            x => x.FacilityId == facility.Id
                                 && x.SprayfieldId == sprayfield.Id
                                 && x.Year == monthKey.Year
                                 && x.Month == monthKey.Month,
                            cancellationToken);

                    if (!loadingInches.HasValue || loadingInches.Value <= 0m)
                    {
                        if (existing != null)
                        {
                            _context.SprayfieldBaselineMonthlyLoadings.Remove(existing);
                            result.ClearedCellCount++;
                        }

                        continue;
                    }

                    var rounded = Math.Round(loadingInches.Value, 2, MidpointRounding.AwayFromZero);
                    if (existing == null)
                    {
                        _context.SprayfieldBaselineMonthlyLoadings.Add(new SprayfieldBaselineMonthlyLoading
                        {
                            CompanyId = companyId,
                            FacilityId = facility.Id,
                            SprayfieldId = sprayfield.Id,
                            Year = monthKey.Year,
                            Month = monthKey.Month,
                            LoadingInches = rounded,
                            CreatedBy = userId
                        });
                    }
                    else
                    {
                        existing.LoadingInches = rounded;
                        existing.UpdatedDate = DateTime.UtcNow;
                    }

                    result.ImportedCellCount++;
                }
            }
        }

        if (result.ImportedCellCount > 0 || result.ClearedCellCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Deduplicate repeated unknown-field errors
        result.Errors = result.Errors.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return result;
    }

    private const string OperationalMonthTooltip =
        "SAM irrigation data exists for this month — baseline cannot be edited.";

    private static List<ClientSetupMonthRowViewModel> BuildWizardMonthRowsFromLookups(
        Guid facilityId,
        IReadOnlyList<Sprayfield> sprayfields,
        IReadOnlyList<(int Year, int Month)> historicalMonths,
        IReadOnlyDictionary<(Guid FacilityId, Guid SprayfieldId, int Year, int Month), decimal?> baselinesByKey,
        IReadOnlyDictionary<(Guid FacilityId, Guid SprayfieldId, int Year, int Month), (decimal Gallons, bool HasRealVolume)> gallonsByKey)
    {
        var monthRows = new List<ClientSetupMonthRowViewModel>(historicalMonths.Count);

        foreach (var (year, month) in historicalMonths)
        {
            var monthDate = new DateTime(year, month, 1);
            var cells = new List<ClientSetupCellViewModel>(sprayfields.Count);

            foreach (var sprayfield in sprayfields)
            {
                var cellKey = (facilityId, sprayfield.Id, year, month);

                if (gallonsByKey.TryGetValue(cellKey, out var app) && app.HasRealVolume)
                {
                    var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
                    decimal? realInches = null;
                    if (acres > 0m && app.Gallons > 0m)
                    {
                        realInches = app.Gallons
                            / (acres * MonthlyApplicationCalculationHelper.GallonsPerAcreInch);
                    }

                    cells.Add(new ClientSetupCellViewModel
                    {
                        SprayfieldId = sprayfield.Id,
                        LoadingInches = realInches,
                        IsReadOnly = true,
                        Source = "Operational",
                        Tooltip = OperationalMonthTooltip
                    });
                    continue;
                }

                baselinesByKey.TryGetValue(cellKey, out var baseline);

                cells.Add(new ClientSetupCellViewModel
                {
                    SprayfieldId = sprayfield.Id,
                    LoadingInches = baseline,
                    IsReadOnly = false,
                    Source = baseline.HasValue ? "Baseline" : "Empty"
                });
            }

            monthRows.Add(new ClientSetupMonthRowViewModel
            {
                Year = year,
                Month = month,
                Label = monthDate.ToString("MMMM yyyy"),
                Cells = cells
            });
        }

        return monthRows;
    }

    private async Task SaveCellsAsync(
        Guid facilityId,
        int throughYear,
        int throughMonth,
        IReadOnlyList<ClientSetupCellSaveRequest> cells,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var facility = await _context.Facilities
            .FirstOrDefaultAsync(f => f.Id == facilityId, cancellationToken);

        if (facility == null)
        {
            throw new EntityNotFoundException(nameof(Facility), facilityId);
        }

        var allowedMonths = BaselineWindowHelper
            .GetHistoricalMonths(throughYear, throughMonth)
            .ToHashSet();

        foreach (var cell in cells)
        {
            if (!allowedMonths.Contains((cell.Year, cell.Month)))
            {
                throw new BusinessRuleException(
                    $"Baseline loading can only be set for the {BaselineWindowHelper.HistoricalMonthCount} months before the first reporting month.");
            }

            if (await _monthlyLoadingResolution.HasRealOperationalMonthAsync(
                    facilityId,
                    cell.SprayfieldId,
                    cell.Year,
                    cell.Month,
                    cancellationToken))
            {
                throw new BusinessRuleException(OperationalMonthTooltip);
            }

            var existing = await _context.SprayfieldBaselineMonthlyLoadings
                .FirstOrDefaultAsync(
                    x => x.FacilityId == facilityId
                         && x.SprayfieldId == cell.SprayfieldId
                         && x.Year == cell.Year
                         && x.Month == cell.Month,
                    cancellationToken);

            if (!cell.LoadingInches.HasValue || cell.LoadingInches.Value <= 0m)
            {
                if (existing != null)
                {
                    _context.SprayfieldBaselineMonthlyLoadings.Remove(existing);
                }

                continue;
            }

            var loadingInches = Math.Round(cell.LoadingInches.Value, 2, MidpointRounding.AwayFromZero);

            if (existing == null)
            {
                _context.SprayfieldBaselineMonthlyLoadings.Add(new SprayfieldBaselineMonthlyLoading
                {
                    CompanyId = facility.CompanyId,
                    FacilityId = facilityId,
                    SprayfieldId = cell.SprayfieldId,
                    Year = cell.Year,
                    Month = cell.Month,
                    LoadingInches = loadingInches,
                    CreatedBy = userId
                });
            }
            else
            {
                existing.LoadingInches = loadingInches;
                existing.UpdatedDate = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static string GetCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return string.Empty;
        }

        return cell.GetFormattedString()?.Trim()
               ?? cell.GetString()?.Trim()
               ?? string.Empty;
    }

    private static decimal? ParseLoadingInches(IXLCell cell)
    {
        if (cell.IsEmpty())
        {
            return null;
        }

        if (cell.DataType == XLDataType.Number)
        {
            var number = (decimal)cell.GetDouble();
            return number > 0m ? number : null;
        }

        var text = GetCellText(cell);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var invariant)
            && invariant > 0m)
        {
            return invariant;
        }

        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var current)
            && current > 0m)
        {
            return current;
        }

        return null;
    }
}

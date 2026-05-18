using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Infrastructure.Authorization;

namespace SAM.Controllers;

public partial class SystemAdminController
{
    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityPermits(Guid facilityId, int? month = null, int? year = null, string? source = null, string? returnUrl = null)
    {
        var facility = await _context.Facilities.Include(f => f.Company).FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null) return NotFound();
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var permits = await _context.FacilityPermits
            .Include(p => p.TemplateParameters)
                .ThenInclude(t => t.PcsParameterCatalog)
            .Where(x => x.FacilityId == facilityId)
            .OrderByDescending(x => x.EffectiveStartDate)
            .ToListAsync();

        var pcsCatalog = await _context.PcsParameterCatalogs
            .Where(x => x.IsActive)
            .OrderBy(x => x.PcsCode)
            .ToListAsync();

        ViewBag.Facility = facility;
        ViewBag.Permits = permits;
        ViewBag.PcsCatalog = pcsCatalog;
        ViewBag.Frequencies = Enum.GetValues<MeasurementFrequencyEnum>();
        ViewBag.SampleTypes = Enum.GetValues<SampleTypeEnum>();
        ViewBag.ReportTypes = Enum.GetValues<PermitTemplateReportTypeEnum>()
            .Where(x => x != PermitTemplateReportTypeEnum.None)
            .ToList();
        ViewBag.SourceContext = source;
        ViewBag.ContextMonth = month;
        ViewBag.ContextYear = year;
        ViewBag.ReturnUrl = returnUrl;

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityPermitCreate(
        Guid facilityId,
        string permitNumber,
        string permitVersion,
        DateTime effectiveStartDate,
        DateTime? effectiveEndDate,
        string? notes,
        IFormFile? permitPdf)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null) return NotFound();
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var normalizedPermitNumber = permitNumber.Trim();
        var normalizedPermitVersion = permitVersion.Trim();
        var startDate = effectiveStartDate.Date;
        var endDate = effectiveEndDate?.Date;

        string? storedPath = null;
        string? originalName = null;
        if (permitPdf is { Length: > 0 })
        {
            var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads", "permits", facilityId.ToString("N"));
            Directory.CreateDirectory(uploadsDir);
            originalName = Path.GetFileName(permitPdf.FileName);
            var fileName = $"{Guid.NewGuid():N}_{originalName}";
            var absolutePath = Path.Combine(uploadsDir, fileName);
            await using var stream = System.IO.File.Create(absolutePath);
            await permitPdf.CopyToAsync(stream);
            storedPath = Path.Combine("uploads", "permits", facilityId.ToString("N"), fileName).Replace("\\", "/");
        }

        var existingSameVersion = await _context.FacilityPermits
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId
                                      && p.PermitNumber == normalizedPermitNumber
                                      && p.PermitVersion == normalizedPermitVersion);

        var existingPermitId = existingSameVersion?.Id;
        var overlaps = await _context.FacilityPermits
            .Where(p => p.FacilityId == facilityId && p.IsActive && p.Id != existingPermitId)
            .AnyAsync(p => p.EffectiveStartDate <= (endDate ?? DateTime.MaxValue)
                           && (p.EffectiveEndDate ?? DateTime.MaxValue) >= startDate);

        if (overlaps)
        {
            TempData["ErrorMessage"] = "Permit date range overlaps an existing active permit version.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId });
        }

        if (existingSameVersion != null)
        {
            if (!existingSameVersion.IsDeleted)
            {
                TempData["ErrorMessage"] = "This permit number and version already exists. Edit or archive the existing record.";
                return RedirectToAction(nameof(FacilityPermits), new { facilityId });
            }

            existingSameVersion.IsDeleted = false;
            existingSameVersion.IsActive = true;
            existingSameVersion.EffectiveStartDate = startDate;
            existingSameVersion.EffectiveEndDate = endDate;
            existingSameVersion.Notes = notes;
            existingSameVersion.PermitNumber = normalizedPermitNumber;
            existingSameVersion.PermitVersion = normalizedPermitVersion;
            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                existingSameVersion.PermitPdfStoragePath = storedPath;
                existingSameVersion.PermitPdfFileName = originalName;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Archived permit version restored and updated.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId });
        }

        _context.FacilityPermits.Add(new FacilityPermit
        {
            CompanyId = facility.CompanyId,
            FacilityId = facilityId,
            PermitNumber = normalizedPermitNumber,
            PermitVersion = normalizedPermitVersion,
            EffectiveStartDate = startDate,
            EffectiveEndDate = endDate,
            IsActive = true,
            Notes = notes,
            PermitPdfFileName = originalName,
            PermitPdfStoragePath = storedPath
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            TempData["ErrorMessage"] = "A permit with the same number and version already exists. If it was archived, restore it from Archived Permit Versions.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId });
        }

        TempData["SuccessMessage"] = "Permit version added.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityPermitArchive(Guid permitId)
    {
        var permit = await _context.FacilityPermits.FirstOrDefaultAsync(x => x.Id == permitId);
        if (permit == null) return NotFound();
        await EnsureCompanyAccessAsync(permit.CompanyId);
        permit.IsActive = false;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permit version archived.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityPermitUnarchive(Guid permitId)
    {
        var permit = await _context.FacilityPermits.FirstOrDefaultAsync(x => x.Id == permitId);
        if (permit == null) return NotFound();
        await EnsureCompanyAccessAsync(permit.CompanyId);

        var overlaps = await _context.FacilityPermits
            .Where(p => p.FacilityId == permit.FacilityId && p.Id != permit.Id && p.IsActive)
            .AnyAsync(p => p.EffectiveStartDate <= (permit.EffectiveEndDate ?? DateTime.MaxValue)
                           && (p.EffectiveEndDate ?? DateTime.MaxValue) >= permit.EffectiveStartDate);

        if (overlaps)
        {
            TempData["ErrorMessage"] = "Cannot unarchive permit version because its date range overlaps an active permit version.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
        }

        permit.IsActive = true;
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permit version unarchived.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityPermitDelete(Guid permitId)
    {
        var permit = await _context.FacilityPermits.FirstOrDefaultAsync(x => x.Id == permitId);
        if (permit == null) return NotFound();
        await EnsureCompanyAccessAsync(permit.CompanyId);

        permit.IsActive = false;
        permit.IsDeleted = true;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Permit version deleted.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> ImportPcsCatalogFromTsv(Guid facilityId)
    {
        var facility = await _context.Facilities.FirstOrDefaultAsync(f => f.Id == facilityId);
        if (facility == null) return NotFound();
        await EnsureCompanyAccessAsync(facility.CompanyId);

        var tsvPath = Path.Combine(_environment.WebRootPath, "table.tsv");
        var changed = await _pcsCatalogService.ImportFromTsvAsync(tsvPath);
        TempData["SuccessMessage"] = $"PCS catalog import complete. {changed} rows processed.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> AddPermitTemplateParameter(
        Guid facilityPermitId,
        Guid pcsParameterCatalogId,
        decimal? monthlyAverageLimit,
        decimal? monthlyGeometricMeanLimit,
        decimal? dailyMinimumLimit,
        decimal? dailyMaximumLimit,
        SampleTypeEnum sampleType,
        MeasurementFrequencyEnum measurementFrequency,
        string? scheduledMonthsCsv,
        string? notes,
        bool isRequired,
        PermitTemplateReportTypeEnum reportType = PermitTemplateReportTypeEnum.Ndmr)
    {
        var permit = await _context.FacilityPermits.Include(p => p.Facility).FirstOrDefaultAsync(p => p.Id == facilityPermitId);
        if (permit == null) return NotFound();
        await EnsureCompanyAccessAsync(permit.CompanyId);

        var selectedReportType = reportType == PermitTemplateReportTypeEnum.None
            ? PermitTemplateReportTypeEnum.Ndmr
            : reportType;

        var exists = await _context.FacilityPermitTemplateParameters
            .AnyAsync(x =>
                x.FacilityPermitId == facilityPermitId &&
                x.PcsParameterCatalogId == pcsParameterCatalogId &&
                x.ReportTypes == selectedReportType);
        if (exists)
        {
            TempData["ErrorMessage"] = "PCS code already exists in this permit template section.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
        }

        var nextSortOrder = (await _context.FacilityPermitTemplateParameters
            .Where(x => x.FacilityPermitId == facilityPermitId &&
                        (x.ReportTypes & selectedReportType) != 0)
            .Select(x => (int?)x.SortOrder)
            .MaxAsync() ?? 0) + 1;

        _context.FacilityPermitTemplateParameters.Add(new FacilityPermitTemplateParameter
        {
            CompanyId = permit.CompanyId,
            FacilityPermitId = facilityPermitId,
            PcsParameterCatalogId = pcsParameterCatalogId,
            MonthlyAverageLimit = monthlyAverageLimit,
            MonthlyGeometricMeanLimit = monthlyGeometricMeanLimit,
            DailyMinimumLimit = dailyMinimumLimit,
            DailyMaximumLimit = dailyMaximumLimit,
            SampleType = sampleType,
            MeasurementFrequency = measurementFrequency,
            ScheduledMonthsCsv = string.IsNullOrWhiteSpace(scheduledMonthsCsv) ? null : scheduledMonthsCsv,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            SortOrder = nextSortOrder,
            IsRequired = isRequired,
            ReportTypes = selectedReportType
        });
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permit template parameter added.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> UpdatePermitTemplateParameter(
        Guid id,
        decimal? monthlyAverageLimit,
        decimal? monthlyGeometricMeanLimit,
        decimal? dailyMinimumLimit,
        decimal? dailyMaximumLimit,
        SampleTypeEnum sampleType,
        MeasurementFrequencyEnum measurementFrequency,
        string? scheduledMonthsCsv,
        string? notes,
        bool isRequired,
        PermitTemplateReportTypeEnum reportType = PermitTemplateReportTypeEnum.Ndmr)
    {
        var row = await _context.FacilityPermitTemplateParameters
            .Include(x => x.FacilityPermit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (row == null) return NotFound();
        await EnsureCompanyAccessAsync(row.CompanyId);

        var selectedReportType = reportType == PermitTemplateReportTypeEnum.None
            ? PermitTemplateReportTypeEnum.Ndmr
            : reportType;

        var duplicate = await _context.FacilityPermitTemplateParameters
            .AnyAsync(x =>
                x.Id != row.Id &&
                x.FacilityPermitId == row.FacilityPermitId &&
                x.PcsParameterCatalogId == row.PcsParameterCatalogId &&
                x.ReportTypes == selectedReportType);
        if (duplicate)
        {
            TempData["ErrorMessage"] = "A template row with the same PCS code already exists in the selected section.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = row.FacilityPermit!.FacilityId });
        }

        row.MonthlyAverageLimit = monthlyAverageLimit;
        row.MonthlyGeometricMeanLimit = monthlyGeometricMeanLimit;
        row.DailyMinimumLimit = dailyMinimumLimit;
        row.DailyMaximumLimit = dailyMaximumLimit;
        row.SampleType = sampleType;
        row.MeasurementFrequency = measurementFrequency;
        row.ScheduledMonthsCsv = string.IsNullOrWhiteSpace(scheduledMonthsCsv) ? null : scheduledMonthsCsv;
        row.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        row.IsRequired = isRequired;
        row.ReportTypes = selectedReportType;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permit template parameter updated.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = row.FacilityPermit!.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> ReorderPermitTemplateParameters(
        Guid facilityPermitId,
        PermitTemplateReportTypeEnum reportType,
        string orderedIdsCsv)
    {
        var permit = await _context.FacilityPermits.FirstOrDefaultAsync(p => p.Id == facilityPermitId);
        if (permit == null) return NotFound();
        await EnsureCompanyAccessAsync(permit.CompanyId);

        var selectedReportType = reportType == PermitTemplateReportTypeEnum.None
            ? PermitTemplateReportTypeEnum.Ndmr
            : reportType;

        var orderedIds = (orderedIdsCsv ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => Guid.TryParse(x, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();

        if (!orderedIds.Any())
        {
            TempData["ErrorMessage"] = "No rows were provided for reorder.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
        }

        var rows = await _context.FacilityPermitTemplateParameters
            .Where(x => x.FacilityPermitId == facilityPermitId &&
                        (x.ReportTypes & selectedReportType) != 0)
            .ToListAsync();

        if (!rows.Any())
        {
            TempData["ErrorMessage"] = "No matching template rows found for reorder.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
        }

        var rowLookup = rows.ToDictionary(x => x.Id, x => x);
        var validOrderedIds = orderedIds.Where(id => rowLookup.ContainsKey(id)).Distinct().ToList();
        var remainingIds = rows
            .Where(x => !validOrderedIds.Contains(x.Id))
            .OrderBy(x => x.SortOrder)
            .Select(x => x.Id)
            .ToList();
        var finalOrder = validOrderedIds.Concat(remainingIds).ToList();

        for (var i = 0; i < finalOrder.Count; i++)
        {
            rowLookup[finalOrder[i]].SortOrder = i + 1;
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Template parameter order updated.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> DeletePermitTemplateParameter(Guid id)
    {
        var row = await _context.FacilityPermitTemplateParameters
            .Include(x => x.FacilityPermit)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (row == null) return NotFound();
        await EnsureCompanyAccessAsync(row.CompanyId);
        _context.FacilityPermitTemplateParameters.Remove(row);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permit template parameter removed.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = row.FacilityPermit!.FacilityId });
    }
}

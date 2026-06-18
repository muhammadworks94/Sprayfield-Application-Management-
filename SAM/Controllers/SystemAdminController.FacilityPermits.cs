using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
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
        ViewBag.LatestPermit = permits.Where(p => p.IsActive).OrderByDescending(p => p.EffectiveStartDate).FirstOrDefault();
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
        bool gwOperationLagoon,
        bool gwOperationSprayField,
        string? address,
        string? city,
        string? state,
        string? zipCode,
        string? county,
        int? totalNumberOfSprayfields,
        decimal? permittedMinimumFreeboardFeet,
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
            originalName = Path.GetFileName(permitPdf.FileName);
            var fileName = $"{Guid.NewGuid():N}_{originalName}";
            storedPath = $"SAM/facility-permits/{facilityId:N}/{fileName}";
            await UploadBlobAsync(storedPath, permitPdf.OpenReadStream(), "application/pdf");
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
            existingSameVersion.GwOperationLagoon = gwOperationLagoon;
            existingSameVersion.GwOperationSprayField = gwOperationSprayField;
            existingSameVersion.Notes = notes;
            existingSameVersion.PermitNumber = normalizedPermitNumber;
            existingSameVersion.PermitVersion = normalizedPermitVersion;
            if (!string.IsNullOrWhiteSpace(storedPath))
            {
                if (!string.IsNullOrWhiteSpace(existingSameVersion.PermitPdfStoragePath))
                {
                    await DeleteBlobIfExistsAsync(existingSameVersion.PermitPdfStoragePath);
                }
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
            GwOperationLagoon = gwOperationLagoon,
            GwOperationSprayField = gwOperationSprayField,
            Notes = notes,
            PermitPdfFileName = originalName,
            PermitPdfStoragePath = storedPath,
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            State = string.IsNullOrWhiteSpace(state) ? null : state.Trim(),
            ZipCode = string.IsNullOrWhiteSpace(zipCode) ? null : zipCode.Trim(),
            County = string.IsNullOrWhiteSpace(county) ? null : county.Trim(),
            TotalNumberOfSprayfields = totalNumberOfSprayfields,
            PermittedMinimumFreeboardFeet = permittedMinimumFreeboardFeet
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
    public async Task<IActionResult> FacilityPermitUpdate(
        Guid permitId,
        DateTime effectiveStartDate,
        DateTime? effectiveEndDate,
        bool gwOperationLagoon,
        bool gwOperationSprayField,
        string? address,
        string? city,
        string? state,
        string? zipCode,
        string? county,
        int? totalNumberOfSprayfields,
        decimal? permittedMinimumFreeboardFeet,
        string? notes,
        IFormFile? permitPdf)
    {
        var permit = await _context.FacilityPermits.FirstOrDefaultAsync(x => x.Id == permitId);
        if (permit == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(permit.CompanyId);
        if (!permit.IsActive)
        {
            TempData["ErrorMessage"] = "Only active permit versions can be edited.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
        }

        var startDate = effectiveStartDate.Date;
        var endDate = effectiveEndDate?.Date;
        var overlaps = await _context.FacilityPermits
            .Where(p => p.FacilityId == permit.FacilityId && p.IsActive && p.Id != permitId)
            .AnyAsync(p => p.EffectiveStartDate <= (endDate ?? DateTime.MaxValue)
                           && (p.EffectiveEndDate ?? DateTime.MaxValue) >= startDate);

        if (overlaps)
        {
            TempData["ErrorMessage"] = "Permit date range overlaps an existing active permit version.";
            return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
        }

        string? newBlobPath = null;
        string? newOriginalName = null;
        if (permitPdf is { Length: > 0 })
        {
            if (!IsPdfUpload(permitPdf))
            {
                TempData["ErrorMessage"] = "Only PDF files are allowed for permit upload.";
                return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
            }

            newOriginalName = Path.GetFileName(permitPdf.FileName);
            var storedName = $"{Guid.NewGuid():N}_{newOriginalName}";
            newBlobPath = $"SAM/facility-permits/{permit.FacilityId:N}/{storedName}";

            await using var uploadStream = permitPdf.OpenReadStream();
            await UploadBlobAsync(newBlobPath, uploadStream, "application/pdf");
        }

        permit.EffectiveStartDate = startDate;
        permit.EffectiveEndDate = endDate;
        permit.GwOperationLagoon = gwOperationLagoon;
        permit.GwOperationSprayField = gwOperationSprayField;
        permit.Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        permit.City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        permit.State = string.IsNullOrWhiteSpace(state) ? null : state.Trim();
        permit.ZipCode = string.IsNullOrWhiteSpace(zipCode) ? null : zipCode.Trim();
        permit.County = string.IsNullOrWhiteSpace(county) ? null : county.Trim();
        permit.TotalNumberOfSprayfields = totalNumberOfSprayfields;
        permit.PermittedMinimumFreeboardFeet = permittedMinimumFreeboardFeet;
        permit.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        if (!string.IsNullOrWhiteSpace(newBlobPath))
        {
            var oldBlobPath = permit.PermitPdfStoragePath;
            permit.PermitPdfStoragePath = newBlobPath;
            permit.PermitPdfFileName = newOriginalName;

            if (!string.IsNullOrWhiteSpace(oldBlobPath))
            {
                await DeleteBlobIfExistsAsync(oldBlobPath);
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Permit version updated.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
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

        if (!string.IsNullOrWhiteSpace(permit.PermitPdfStoragePath))
        {
            await DeleteBlobIfExistsAsync(permit.PermitPdfStoragePath);
            permit.PermitPdfStoragePath = null;
            permit.PermitPdfFileName = null;
        }

        permit.IsActive = false;
        permit.IsDeleted = true;

        var facilitiesUsingDefault = await _context.Facilities
            .Where(f => f.DefaultFacilityPermitId == permitId)
            .ToListAsync();
        foreach (var facility in facilitiesUsingDefault)
        {
            facility.DefaultFacilityPermitId = null;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Permit version deleted.";
        return RedirectToAction(nameof(FacilityPermits), new { facilityId = permit.FacilityId });
    }

    [HttpGet]
    [Authorize(Policy = Policies.RequireCompanyAdmin)]
    public async Task<IActionResult> FacilityPermitPdf(Guid permitId, bool download = false)
    {
        var permit = await _context.FacilityPermits.FirstOrDefaultAsync(x => x.Id == permitId);
        if (permit == null)
        {
            return NotFound();
        }

        await EnsureCompanyAccessAsync(permit.CompanyId);
        if (string.IsNullOrWhiteSpace(permit.PermitPdfStoragePath))
        {
            return NotFound();
        }

        var blobClient = (await GetSamBlobContainerClientAsync()).GetBlobClient(permit.PermitPdfStoragePath);
        if (!await blobClient.ExistsAsync())
        {
            return NotFound("Permit PDF blob is missing from storage.");
        }

        var blob = await blobClient.DownloadStreamingAsync();
        var contentType = string.IsNullOrWhiteSpace(blob.Value.Details.ContentType)
            ? "application/pdf"
            : blob.Value.Details.ContentType;

        if (download)
        {
            var fileName = string.IsNullOrWhiteSpace(permit.PermitPdfFileName)
                ? $"{permit.PermitNumber}_{permit.PermitVersion}.pdf"
                : permit.PermitPdfFileName;
            return File(blob.Value.Content, contentType, fileName);
        }

        return File(blob.Value.Content, contentType);
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

    private async Task UploadBlobAsync(string blobName, Stream fileStream, string contentType)
    {
        var container = await GetSamBlobContainerClientAsync();
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.UploadAsync(fileStream, overwrite: false);
        await blobClient.SetHttpHeadersAsync(new Azure.Storage.Blobs.Models.BlobHttpHeaders
        {
            ContentType = contentType
        });
    }

    private async Task DeleteBlobIfExistsAsync(string blobName)
    {
        var container = await GetSamBlobContainerClientAsync();
        var blobClient = container.GetBlobClient(blobName);
        await blobClient.DeleteIfExistsAsync();
    }

    private async Task<BlobContainerClient> GetSamBlobContainerClientAsync()
    {
        var connectionString = _configuration.GetConnectionString("StorageConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("StorageConnectionString is not configured.");
        }

        var blobServiceClient = new BlobServiceClient(connectionString);
        var container = blobServiceClient.GetBlobContainerClient("sam-files");
        await container.CreateIfNotExistsAsync();
        return container;
    }

    private static bool IsPdfUpload(IFormFile file)
    {
        var fileName = file.FileName ?? string.Empty;
        return file.ContentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }
}

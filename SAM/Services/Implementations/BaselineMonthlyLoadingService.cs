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
    private readonly ApplicationDbContext _context;
    private readonly IMonthlyLoadingResolutionService _monthlyLoadingResolution;
    private readonly INDAR1Service _ndar1Service;
    private readonly ISprayfieldService _sprayfieldService;

    public BaselineMonthlyLoadingService(
        ApplicationDbContext context,
        IMonthlyLoadingResolutionService monthlyLoadingResolution,
        INDAR1Service ndar1Service,
        ISprayfieldService sprayfieldService)
    {
        _context = context;
        _monthlyLoadingResolution = monthlyLoadingResolution;
        _ndar1Service = ndar1Service;
        _sprayfieldService = sprayfieldService;
    }

    public async Task<ClientSetupViewModel> GetSetupGridAsync(
        Guid facilityId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var facility = await _context.Facilities
            .AsNoTracking()
            .Include(f => f.Company)
            .FirstOrDefaultAsync(f => f.Id == facilityId, cancellationToken);

        if (facility == null)
        {
            throw new EntityNotFoundException(nameof(Facility), facilityId);
        }

        var sprayfields = (await _sprayfieldService.GetByFacilityIdAsync(facilityId))
            .OrderBy(s => int.TryParse(s.FieldId, out var order) ? order : int.MaxValue)
            .ThenBy(s => s.FieldId)
            .ToList();

        var endMonth = new DateTime(throughYear, throughMonth, 1);
        var monthRows = new List<ClientSetupMonthRowViewModel>();

        for (var i = 0; i < 12; i++)
        {
            var monthDate = endMonth.AddMonths(-i);
            var cells = new List<ClientSetupCellViewModel>();

            foreach (var sprayfield in sprayfields)
            {
                var hasReal = await _monthlyLoadingResolution.HasRealOperationalMonthAsync(
                    facilityId,
                    sprayfield.Id,
                    monthDate.Year,
                    monthDate.Month,
                    cancellationToken);

                if (hasReal)
                {
                    var inches = await _monthlyLoadingResolution.GetRealMonthlyLoadingInchesAsync(
                        facilityId,
                        sprayfield.Id,
                        monthDate.Year,
                        monthDate.Month,
                        cancellationToken);

                    cells.Add(new ClientSetupCellViewModel
                    {
                        SprayfieldId = sprayfield.Id,
                        LoadingInches = inches,
                        IsReadOnly = true,
                        Source = "Real"
                    });
                }
                else
                {
                    var baseline = await _monthlyLoadingResolution.GetBaselineMonthlyLoadingInchesAsync(
                        facilityId,
                        sprayfield.Id,
                        monthDate.Year,
                        monthDate.Month,
                        cancellationToken);

                    cells.Add(new ClientSetupCellViewModel
                    {
                        SprayfieldId = sprayfield.Id,
                        LoadingInches = baseline,
                        IsReadOnly = false,
                        Source = baseline.HasValue ? "Baseline" : "Empty"
                    });
                }
            }

            monthRows.Add(new ClientSetupMonthRowViewModel
            {
                Year = monthDate.Year,
                Month = monthDate.Month,
                Label = monthDate.ToString("MMMM yyyy"),
                Cells = cells
            });
        }

        return new ClientSetupViewModel
        {
            CompanyId = facility.CompanyId,
            FacilityId = facilityId,
            ThroughYear = throughYear,
            ThroughMonth = throughMonth,
            CompanyName = facility.Company?.Name,
            FacilityName = facility.Name,
            SprayfieldColumns = sprayfields.Select(s => new ClientSetupSprayfieldColumnViewModel
            {
                SprayfieldId = s.Id,
                FieldCode = s.FieldId,
                Acres = SprayfieldReportHelper.GetReportAcres(s)
            }).ToList(),
            MonthRows = monthRows
        };
    }

    public async Task SaveSetupGridAsync(
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

        var endMonth = new DateTime(throughYear, throughMonth, 1);
        var allowedMonths = Enumerable.Range(0, 12)
            .Select(offset => endMonth.AddMonths(-offset))
            .Select(d => (d.Year, d.Month))
            .ToHashSet();

        foreach (var cell in cells)
        {
            if (!allowedMonths.Contains((cell.Year, cell.Month)))
            {
                continue;
            }

            if (await _monthlyLoadingResolution.HasRealOperationalMonthAsync(
                    facilityId,
                    cell.SprayfieldId,
                    cell.Year,
                    cell.Month,
                    cancellationToken))
            {
                continue;
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

    public async Task RefreshNdarReportsForWindowAsync(
        Guid facilityId,
        int throughYear,
        int throughMonth,
        CancellationToken cancellationToken = default)
    {
        var endMonth = new DateTime(throughYear, throughMonth, 1);

        for (var i = 0; i < 12; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var monthDate = endMonth.AddMonths(-i);
            await _ndar1Service.EnsureAndRefreshForMonthAsync(facilityId, monthDate.Month, monthDate.Year);
        }
    }
}

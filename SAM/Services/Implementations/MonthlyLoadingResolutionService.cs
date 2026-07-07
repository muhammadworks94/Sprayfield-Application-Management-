using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Utilities;

namespace SAM.Services.Implementations;

public class MonthlyLoadingResolutionService : IMonthlyLoadingResolutionService
{
    private readonly ApplicationDbContext _context;

    public MonthlyLoadingResolutionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> HasRealOperationalMonthAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default)
    {
        var (monthStart, monthEndExclusive) = GetMonthBounds(year, month);
        return await _context.MonthlyApplications
            .AsNoTracking()
            .AnyAsync(
                a => a.FacilityId == facilityId
                     && a.SprayfieldId == sprayfieldId
                     && a.ApplicationDate >= monthStart
                     && a.ApplicationDate < monthEndExclusive
                     && a.VolumeGallons > 0m,
                cancellationToken);
    }

    public async Task<decimal> GetRealMonthlyLoadingInchesAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default)
    {
        var sprayfield = await _context.Sprayfields
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sprayfieldId, cancellationToken);
        if (sprayfield == null)
        {
            return 0m;
        }

        var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
        if (acres <= 0m)
        {
            return 0m;
        }

        var (monthStart, monthEndExclusive) = GetMonthBounds(year, month);
        var totalGallons = await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a => a.FacilityId == facilityId
                        && a.SprayfieldId == sprayfieldId
                        && a.ApplicationDate >= monthStart
                        && a.ApplicationDate < monthEndExclusive)
            .SumAsync(a => a.VolumeGallons, cancellationToken);

        return totalGallons / (acres * MonthlyApplicationCalculationHelper.GallonsPerAcreInch);
    }

    public async Task<decimal?> GetBaselineMonthlyLoadingInchesAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default)
    {
        return await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId
                        && x.SprayfieldId == sprayfieldId
                        && x.Year == year
                        && x.Month == month)
            .Select(x => (decimal?)x.LoadingInches)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<decimal> GetEffectiveMonthlyLoadingInchesAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default)
    {
        if (await HasRealOperationalMonthAsync(facilityId, sprayfieldId, year, month, cancellationToken))
        {
            return await GetRealMonthlyLoadingInchesAsync(facilityId, sprayfieldId, year, month, cancellationToken);
        }

        var baseline = await GetBaselineMonthlyLoadingInchesAsync(facilityId, sprayfieldId, year, month, cancellationToken);
        return baseline ?? 0m;
    }

    public async Task<decimal> GetCalendar12MonthRollingInchesAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        var endMonthStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        decimal total = 0m;
        for (var i = 0; i < 12; i++)
        {
            var monthDate = endMonthStart.AddMonths(-i);
            total += await GetEffectiveMonthlyLoadingInchesAsync(
                facilityId,
                sprayfieldId,
                monthDate.Year,
                monthDate.Month,
                cancellationToken);
        }

        return total;
    }

    public async Task<decimal> Get365DayRollingInchesAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, Guid? excludeApplicationId = null, CancellationToken cancellationToken = default)
    {
        var sprayfield = await _context.Sprayfields
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sprayfieldId, cancellationToken);
        if (sprayfield == null)
        {
            return 0m;
        }

        var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
        if (acres <= 0m)
        {
            return 0m;
        }

        var endDate = asOfDate.Date;
        var startDate = endDate.AddDays(-364);
        decimal total = 0m;

        var cursor = new DateTime(startDate.Year, startDate.Month, 1);
        var lastMonthStart = new DateTime(endDate.Year, endDate.Month, 1);
        while (cursor <= lastMonthStart)
        {
            var year = cursor.Year;
            var month = cursor.Month;
            var monthStart = cursor;
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var overlapStart = monthStart > startDate ? monthStart : startDate;
            var overlapEnd = monthEnd < endDate ? monthEnd : endDate;
            if (overlapStart <= overlapEnd)
            {
                if (await HasRealOperationalMonthAsync(facilityId, sprayfieldId, year, month, cancellationToken))
                {
                    var monthEndExclusive = monthStart.AddMonths(1);
                    var gallonsQuery = _context.MonthlyApplications
                        .AsNoTracking()
                        .Where(a => a.FacilityId == facilityId
                                    && a.SprayfieldId == sprayfieldId
                                    && a.ApplicationDate >= overlapStart
                                    && a.ApplicationDate < monthEndExclusive
                                    && a.ApplicationDate <= endDate);

                    if (excludeApplicationId.HasValue)
                    {
                        gallonsQuery = gallonsQuery.Where(a => a.Id != excludeApplicationId.Value);
                    }

                    var gallons = await gallonsQuery.SumAsync(a => a.VolumeGallons, cancellationToken);
                    total += gallons / (acres * MonthlyApplicationCalculationHelper.GallonsPerAcreInch);
                }
                else
                {
                    var baseline = await GetBaselineMonthlyLoadingInchesAsync(facilityId, sprayfieldId, year, month, cancellationToken) ?? 0m;
                    if (baseline > 0m)
                    {
                        var daysInMonth = DateTime.DaysInMonth(year, month);
                        var daysInOverlap = (overlapEnd - overlapStart).Days + 1;
                        total += baseline * daysInOverlap / daysInMonth;
                    }
                }
            }

            cursor = cursor.AddMonths(1);
        }

        return total;
    }

    public async Task<bool> HasEffectiveIrrigationForMonthAsync(Guid facilityId, int year, int month, CancellationToken cancellationToken = default)
    {
        var hasRealIrrigation = await _context.MonthlyApplications
            .AsNoTracking()
            .AnyAsync(
                a => a.FacilityId == facilityId
                     && a.ApplicationDate.Year == year
                     && a.ApplicationDate.Month == month
                     && a.TimeIrrigatedMinutes.HasValue
                     && a.TimeIrrigatedMinutes.Value > 0m,
                cancellationToken);
        if (hasRealIrrigation)
        {
            return true;
        }

        return await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .AnyAsync(
                x => x.FacilityId == facilityId
                     && x.Year == year
                     && x.Month == month
                     && x.LoadingInches > 0m,
                cancellationToken);
    }

    private static (DateTime MonthStart, DateTime MonthEndExclusive) GetMonthBounds(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        return (monthStart, monthStart.AddMonths(1));
    }
}

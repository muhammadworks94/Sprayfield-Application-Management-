using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Services.Helpers;
using SAM.Services.Interfaces;
using SAM.Services.Models;
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

    public async Task<IReadOnlyDictionary<Guid, SprayfieldLoadingMetrics>> GetBatchFieldLoadingMetricsAsync(
        IReadOnlyList<Sprayfield> sprayfields,
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        if (sprayfields.Count == 0)
        {
            return new Dictionary<Guid, SprayfieldLoadingMetrics>();
        }

        var endMonthStart = new DateTime(asOfDate.Year, asOfDate.Month, 1);
        var windowStart = endMonthStart.AddMonths(-11);
        var windowEndExclusive = endMonthStart.AddMonths(1);
        var windowStartKey = windowStart.Year * 12 + windowStart.Month;
        var windowEndKey = endMonthStart.Year * 12 + endMonthStart.Month;

        var sprayfieldIds = sprayfields.Select(s => s.Id).ToList();

        var applicationAggs = await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a => sprayfieldIds.Contains(a.SprayfieldId)
                        && a.ApplicationDate >= windowStart
                        && a.ApplicationDate < windowEndExclusive)
            .GroupBy(a => new { a.SprayfieldId, a.ApplicationDate.Year, a.ApplicationDate.Month })
            .Select(g => new
            {
                g.Key.SprayfieldId,
                g.Key.Year,
                g.Key.Month,
                TotalGallons = g.Sum(a => a.VolumeGallons),
                HasRealVolume = g.Any(a => a.VolumeGallons > 0m)
            })
            .ToListAsync(cancellationToken);

        var applicationLookup = applicationAggs.ToDictionary(
            x => (x.SprayfieldId, x.Year, x.Month),
            x => (x.TotalGallons, x.HasRealVolume));

        var baselines = await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(x => sprayfieldIds.Contains(x.SprayfieldId)
                        && x.Year * 12 + x.Month >= windowStartKey
                        && x.Year * 12 + x.Month <= windowEndKey)
            .Select(x => new { x.SprayfieldId, x.Year, x.Month, x.LoadingInches })
            .ToListAsync(cancellationToken);

        var baselineLookup = baselines.ToDictionary(
            x => (x.SprayfieldId, x.Year, x.Month),
            x => x.LoadingInches);

        var results = new Dictionary<Guid, SprayfieldLoadingMetrics>(sprayfields.Count);
        foreach (var sprayfield in sprayfields)
        {
            var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
            decimal rollingTotal = 0m;
            decimal currentMonthInches = 0m;

            for (var i = 0; i < 12; i++)
            {
                var monthDate = endMonthStart.AddMonths(-i);
                var key = (sprayfield.Id, monthDate.Year, monthDate.Month);
                var monthInches = ResolveEffectiveMonthInches(key, acres, applicationLookup, baselineLookup);
                rollingTotal += monthInches;

                if (monthDate.Year == asOfDate.Year && monthDate.Month == asOfDate.Month)
                {
                    currentMonthInches = monthInches;
                }
            }

            results[sprayfield.Id] = new SprayfieldLoadingMetrics
            {
                CurrentMonthInches = currentMonthInches,
                Rolling12MonthInches = rollingTotal
            };
        }

        return results;
    }

    private static decimal ResolveEffectiveMonthInches(
        (Guid SprayfieldId, int Year, int Month) key,
        decimal acres,
        IReadOnlyDictionary<(Guid SprayfieldId, int Year, int Month), (decimal TotalGallons, bool HasRealVolume)> applicationLookup,
        IReadOnlyDictionary<(Guid SprayfieldId, int Year, int Month), decimal> baselineLookup)
    {
        if (applicationLookup.TryGetValue(key, out var application) && application.HasRealVolume)
        {
            return acres > 0m
                ? application.TotalGallons / (acres * MonthlyApplicationCalculationHelper.GallonsPerAcreInch)
                : 0m;
        }

        return baselineLookup.TryGetValue(key, out var baseline) ? baseline : 0m;
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetBatch365DayRollingInchesAsync(
        IReadOnlyList<Sprayfield> sprayfields,
        DateTime asOfDate,
        Guid? excludeApplicationId = null,
        CancellationToken cancellationToken = default)
    {
        if (sprayfields.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var endDate = asOfDate.Date;
        var startDate = endDate.AddDays(-364);
        var firstMonthStart = new DateTime(startDate.Year, startDate.Month, 1);
        var sprayfieldIds = sprayfields.Select(s => s.Id).ToList();

        var applicationsQuery = _context.MonthlyApplications
            .AsNoTracking()
            .Where(a => sprayfieldIds.Contains(a.SprayfieldId)
                        && a.ApplicationDate >= startDate
                        && a.ApplicationDate <= endDate);

        if (excludeApplicationId.HasValue)
        {
            applicationsQuery = applicationsQuery.Where(a => a.Id != excludeApplicationId.Value);
        }

        var applications = await applicationsQuery
            .Select(a => new { a.SprayfieldId, a.ApplicationDate, a.VolumeGallons })
            .ToListAsync(cancellationToken);

        var monthlyAggs = await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a => sprayfieldIds.Contains(a.SprayfieldId)
                        && a.ApplicationDate >= firstMonthStart
                        && a.ApplicationDate <= endDate)
            .GroupBy(a => new { a.SprayfieldId, a.ApplicationDate.Year, a.ApplicationDate.Month })
            .Select(g => new
            {
                g.Key.SprayfieldId,
                g.Key.Year,
                g.Key.Month,
                HasRealVolume = g.Any(a => a.VolumeGallons > 0m)
            })
            .ToListAsync(cancellationToken);

        var hasRealVolumeLookup = monthlyAggs
            .Where(x => x.HasRealVolume)
            .Select(x => (x.SprayfieldId, x.Year, x.Month))
            .ToHashSet();

        var windowStartKey = firstMonthStart.Year * 12 + firstMonthStart.Month;
        var windowEndKey = endDate.Year * 12 + endDate.Month;
        var baselines = await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(x => sprayfieldIds.Contains(x.SprayfieldId)
                        && x.Year * 12 + x.Month >= windowStartKey
                        && x.Year * 12 + x.Month <= windowEndKey)
            .Select(x => new { x.SprayfieldId, x.Year, x.Month, x.LoadingInches })
            .ToListAsync(cancellationToken);

        var baselineLookup = baselines.ToDictionary(
            x => (x.SprayfieldId, x.Year, x.Month),
            x => x.LoadingInches);

        var results = new Dictionary<Guid, decimal>(sprayfields.Count);
        foreach (var sprayfield in sprayfields)
        {
            var acres = SprayfieldReportHelper.GetReportAcres(sprayfield);
            if (acres <= 0m)
            {
                results[sprayfield.Id] = 0m;
                continue;
            }

            decimal total = 0m;
            var cursor = firstMonthStart;
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
                    if (hasRealVolumeLookup.Contains((sprayfield.Id, year, month)))
                    {
                        var gallons = applications
                            .Where(a => a.SprayfieldId == sprayfield.Id
                                        && a.ApplicationDate >= overlapStart
                                        && a.ApplicationDate <= overlapEnd)
                            .Sum(a => a.VolumeGallons);
                        total += gallons / (acres * MonthlyApplicationCalculationHelper.GallonsPerAcreInch);
                    }
                    else if (baselineLookup.TryGetValue((sprayfield.Id, year, month), out var baseline) && baseline > 0m)
                    {
                        var daysInMonth = DateTime.DaysInMonth(year, month);
                        var daysInOverlap = (overlapEnd - overlapStart).Days + 1;
                        total += baseline * daysInOverlap / daysInMonth;
                    }
                }

                cursor = cursor.AddMonths(1);
            }

            results[sprayfield.Id] = total;
        }

        return results;
    }

    public async Task<(IReadOnlySet<Guid> RealOperationalSprayfieldIds, IReadOnlyDictionary<Guid, decimal> BaselineInchesBySprayfieldId)> GetBatchMonthBaselineContextAsync(
        Guid facilityId,
        IReadOnlyList<Guid> sprayfieldIds,
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        if (sprayfieldIds.Count == 0)
        {
            return (new HashSet<Guid>(), new Dictionary<Guid, decimal>());
        }

        var (monthStart, monthEndExclusive) = GetMonthBounds(year, month);
        var realOperationalSprayfieldIds = await _context.MonthlyApplications
            .AsNoTracking()
            .Where(a => a.FacilityId == facilityId
                        && sprayfieldIds.Contains(a.SprayfieldId)
                        && a.ApplicationDate >= monthStart
                        && a.ApplicationDate < monthEndExclusive
                        && a.VolumeGallons > 0m)
            .Select(a => a.SprayfieldId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var baselines = await _context.SprayfieldBaselineMonthlyLoadings
            .AsNoTracking()
            .Where(x => x.FacilityId == facilityId
                        && sprayfieldIds.Contains(x.SprayfieldId)
                        && x.Year == year
                        && x.Month == month)
            .Select(x => new { x.SprayfieldId, x.LoadingInches })
            .ToListAsync(cancellationToken);

        return (
            realOperationalSprayfieldIds.ToHashSet(),
            baselines.ToDictionary(x => x.SprayfieldId, x => x.LoadingInches));
    }

    private static (DateTime MonthStart, DateTime MonthEndExclusive) GetMonthBounds(int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        return (monthStart, monthStart.AddMonths(1));
    }
}

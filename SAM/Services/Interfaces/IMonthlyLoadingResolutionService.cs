using SAM.Domain.Entities;
using SAM.Services.Models;

namespace SAM.Services.Interfaces;

public interface IMonthlyLoadingResolutionService
{
    Task<bool> HasRealOperationalMonthAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default);

    Task<decimal> GetRealMonthlyLoadingInchesAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default);

    Task<decimal?> GetBaselineMonthlyLoadingInchesAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default);

    Task<decimal> GetEffectiveMonthlyLoadingInchesAsync(Guid facilityId, Guid sprayfieldId, int year, int month, CancellationToken cancellationToken = default);

    Task<decimal> GetCalendar12MonthRollingInchesAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, CancellationToken cancellationToken = default);

    Task<decimal> Get365DayRollingInchesAsync(Guid facilityId, Guid sprayfieldId, DateTime asOfDate, Guid? excludeApplicationId = null, CancellationToken cancellationToken = default);

    Task<bool> HasEffectiveIrrigationForMonthAsync(Guid facilityId, int year, int month, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, SprayfieldLoadingMetrics>> GetBatchFieldLoadingMetricsAsync(
        IReadOnlyList<Sprayfield> sprayfields,
        DateTime asOfDate,
        CancellationToken cancellationToken = default);
}

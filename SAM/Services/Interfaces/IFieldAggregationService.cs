namespace SAM.Services.Interfaces;

public interface IFieldAggregationService
{
    Task<decimal> GetFieldMonthVolumeGallonsAsync(Guid sprayfieldId, int month, int year);
    Task<decimal> GetFieldMonthLbsAppliedAsync(Guid sprayfieldId, int month, int year);
}

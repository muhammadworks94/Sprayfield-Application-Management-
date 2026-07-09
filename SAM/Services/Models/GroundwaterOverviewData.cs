namespace SAM.Services.Models;

public class GroundwaterOverviewData
{
    public decimal? AvgPH { get; set; }

    public decimal? AvgConductivity { get; set; }

    public IReadOnlyDictionary<Guid, decimal?> LatestPhByWellId { get; set; } =
        new Dictionary<Guid, decimal?>();
}

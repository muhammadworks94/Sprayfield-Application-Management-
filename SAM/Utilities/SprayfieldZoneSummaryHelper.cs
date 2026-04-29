using SAM.Domain.Entities;

namespace SAM.Utilities;

public static class SprayfieldZoneSummaryHelper
{
    public static string? GetSoilSummary(Sprayfield? sprayfield) => sprayfield?.Soil?.TypeName;
    public static string? GetCropSummary(Sprayfield? sprayfield) => sprayfield?.Crop?.Name;
    public static string? GetNozzleSummary(Sprayfield? sprayfield) =>
        sprayfield?.Nozzle is null ? null : $"{sprayfield.Nozzle.Manufacturer} {sprayfield.Nozzle.Model}".Trim();
}

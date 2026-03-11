using SAM.Domain.Entities;

namespace SAM.Utilities;

public static class SprayfieldZoneSummaryHelper
{
    public const string MixedSummary = "Mixed / Zone-specific";

    public static string? GetSoilSummary(Sprayfield? sprayfield) =>
        Summarize(GetActiveZones(sprayfield)
            .Select(z => z.Soil?.TypeName));

    public static string? GetCropSummary(Sprayfield? sprayfield) =>
        Summarize(GetActiveZones(sprayfield)
            .Select(z => z.Crop?.Name));

    public static string? GetNozzleSummary(Sprayfield? sprayfield) =>
        Summarize(GetActiveZones(sprayfield)
            .Select(z => z.Nozzle == null ? null : $"{z.Nozzle.Manufacturer} {z.Nozzle.Model}".Trim()));

    private static IEnumerable<ApplicationZone> GetActiveZones(Sprayfield? sprayfield) =>
        sprayfield?.ApplicationZones?.Where(z => z.Active) ?? Enumerable.Empty<ApplicationZone>();

    private static string? Summarize(IEnumerable<string?> values)
    {
        var distinctValues = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctValues.Count switch
        {
            0 => null,
            1 => distinctValues[0],
            _ => MixedSummary
        };
    }
}

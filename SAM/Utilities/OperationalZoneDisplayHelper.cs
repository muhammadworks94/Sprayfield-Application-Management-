using SAM.Domain.Entities;

namespace SAM.Utilities;

public static class OperationalZoneDisplayHelper
{
    private const decimal PercentTolerance = 0.01m;

    public static IReadOnlyDictionary<Guid, OperationalZoneDisplayInfo> BuildZoneDisplayMap(IEnumerable<Sprayfield> sprayfields)
    {
        var result = new Dictionary<Guid, OperationalZoneDisplayInfo>();
        var orderedSprayfields = SprayfieldReportHelper.OrderByFieldIdNatural(sprayfields).ToList();

        for (var sprayfieldIndex = 0; sprayfieldIndex < orderedSprayfields.Count; sprayfieldIndex++)
        {
            var sprayfield = orderedSprayfields[sprayfieldIndex];
            var sprayfieldOrdinal = (sprayfieldIndex + 1).ToString();
            var orderedZones = GetOrderedZones(sprayfield).ToList();
            var useSingleZoneLabel = orderedZones.Count == 1 &&
                orderedZones[0].Active &&
                Math.Abs(orderedZones[0].PercentOfField - 100m) <= PercentTolerance;
            var totalAcres = SprayfieldReportHelper.GetReportAcres(sprayfield);

            for (var zoneIndex = 0; zoneIndex < orderedZones.Count; zoneIndex++)
            {
                var zone = orderedZones[zoneIndex];
                var zoneDisplayLabel = useSingleZoneLabel
                    ? sprayfieldOrdinal
                    : $"{sprayfieldOrdinal}{GetZoneSuffix(zoneIndex)}";

                result[zone.Id] = new OperationalZoneDisplayInfo(
                    sprayfield.Id,
                    zone.Id,
                    sprayfieldOrdinal,
                    zoneDisplayLabel,
                    totalAcres * (zone.PercentOfField / 100m),
                    zone.ZoneName);
            }
        }

        return result;
    }

    private static IEnumerable<ApplicationZone> GetOrderedZones(Sprayfield sprayfield) =>
        sprayfield.ApplicationZones
            .OrderByDescending(z => z.Active)
            .ThenBy(z => z.ZoneName, StringComparer.OrdinalIgnoreCase);

    private static string GetZoneSuffix(int index)
    {
        var suffix = string.Empty;
        var current = index;

        do
        {
            suffix = (char)('A' + (current % 26)) + suffix;
            current = (current / 26) - 1;
        }
        while (current >= 0);

        return suffix;
    }
}

public sealed record OperationalZoneDisplayInfo(
    Guid SprayfieldId,
    Guid ZoneId,
    string SprayfieldOrdinalLabel,
    string ZoneDisplayLabel,
    decimal ZoneAcres,
    string ZoneName);

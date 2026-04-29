using SAM.Domain.Entities;

namespace SAM.Utilities;

public static class OperationalZoneDisplayHelper
{
    public static IReadOnlyDictionary<Guid, OperationalZoneDisplayInfo> BuildZoneDisplayMap(IEnumerable<Sprayfield> sprayfields)
    {
        var result = new Dictionary<Guid, OperationalZoneDisplayInfo>();
        var orderedSprayfields = SprayfieldReportHelper.OrderByFieldIdNatural(sprayfields).ToList();

        for (var i = 0; i < orderedSprayfields.Count; i++)
        {
            var sprayfield = orderedSprayfields[i];
            var ordinal = (i + 1).ToString();
            result[sprayfield.Id] = new OperationalZoneDisplayInfo(
                sprayfield.Id,
                sprayfield.Id,
                ordinal,
                ordinal,
                SprayfieldReportHelper.GetReportAcres(sprayfield),
                sprayfield.FieldId);
        }

        return result;
    }
}

public sealed record OperationalZoneDisplayInfo(
    Guid SprayfieldId,
    Guid ZoneId,
    string SprayfieldOrdinalLabel,
    string ZoneDisplayLabel,
    decimal ZoneAcres,
    string ZoneName);

using System.Text.RegularExpressions;
using SAM.Domain.Entities;

namespace SAM.Utilities;

public static partial class SprayfieldReportHelper
{
    public static decimal GetReportAcres(Sprayfield? sprayfield) =>
        sprayfield?.AcresTotal ?? sprayfield?.SizeAcres ?? 0m;

    public static IEnumerable<Sprayfield> OrderByFieldIdNatural(IEnumerable<Sprayfield> sprayfields) =>
        sprayfields
            .OrderBy(s => BuildNaturalSortKey(s.FieldId))
            .ThenBy(s => s.FieldId, StringComparer.OrdinalIgnoreCase);

    private static string BuildNaturalSortKey(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : NumericChunkRegex().Replace(value, match => match.Value.PadLeft(10, '0'));

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumericChunkRegex();
}

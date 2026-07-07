using SAM.Domain.Entities;
using SAM.Utilities;

namespace SAM.Services.Helpers;

public static class WWCharChemistryResolver
{
    public const string TknPcsCode = "00625";
    public const string No3PcsCode = "00620";

    public static decimal? AverageNonNull(IEnumerable<decimal?> values)
    {
        var list = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return list.Count == 0 ? null : list.Average();
    }

    public static decimal? AverageNonNull(
        IReadOnlyList<decimal?> values,
        IReadOnlyList<bool>? rdlFlags,
        string pcsCode)
    {
        return ReportingDetectionLimitHelper.AverageForReporting(values, rdlFlags, pcsCode);
    }

    public static decimal? AverageTemplateValueForPcs(
        Guid wwCharId,
        string pcsCode,
        IEnumerable<WWCharTemplateValue> templateValues,
        IReadOnlyDictionary<Guid, string> pcsByTemplateParameterId)
    {
        var matching = templateValues
            .Where(v => v.WWCharId == wwCharId
                        && v.NumericValue.HasValue
                        && pcsByTemplateParameterId.TryGetValue(v.FacilityPermitTemplateParameterId, out var mappedPcs)
                        && string.Equals(mappedPcs, pcsCode, StringComparison.OrdinalIgnoreCase))
            .OrderBy(v => v.DayNo)
            .ToList();

        if (matching.Count == 0)
        {
            return null;
        }

        var values = matching.Select(v => v.NumericValue).ToList();
        var rdlFlags = matching.Select(v => v.IsReportingDetectionLimit).ToList();
        return AverageNonNull(values, rdlFlags, pcsCode);
    }
}

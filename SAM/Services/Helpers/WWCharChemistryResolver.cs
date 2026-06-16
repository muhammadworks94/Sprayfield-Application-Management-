using SAM.Domain.Entities;

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

    public static decimal? AverageTemplateValueForPcs(
        Guid wwCharId,
        string pcsCode,
        IEnumerable<WWCharTemplateValue> templateValues,
        IReadOnlyDictionary<Guid, string> pcsByTemplateParameterId)
    {
        var values = templateValues
            .Where(v => v.WWCharId == wwCharId
                        && v.NumericValue.HasValue
                        && pcsByTemplateParameterId.TryGetValue(v.FacilityPermitTemplateParameterId, out var mappedPcs)
                        && string.Equals(mappedPcs, pcsCode, StringComparison.OrdinalIgnoreCase))
            .Select(v => v.NumericValue);

        return AverageNonNull(values);
    }
}

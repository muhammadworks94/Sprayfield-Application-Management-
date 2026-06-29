using SAM.Domain.Entities;

namespace SAM.Utilities;

public static class NdmrFlowFormatting
{
    public const string FlowPcsCode = "50050";
    public const string FlowUnitsLabel = "MGD";
    public const string FlowNumericFormat = "0.000";
    private const decimal GpdPerMgd = 1_000_000m;

    /// <summary>
    /// Values at or above this threshold are treated as GPD and converted to MGD.
    /// Smaller values are assumed to already be in MGD.
    /// </summary>
    private const decimal GpdMagnitudeThreshold = 100m;

    public static bool IsFlowPcs(string? pcsCode) =>
        string.Equals(pcsCode, FlowPcsCode, StringComparison.OrdinalIgnoreCase);

    public static decimal? NormalizeFlowValueToMgd(decimal? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var magnitude = Math.Abs(value.Value);
        return magnitude >= GpdMagnitudeThreshold
            ? value.Value / GpdPerMgd
            : value.Value;
    }

    public static string FormatFlowValue(decimal? value)
    {
        var mgd = NormalizeFlowValueToMgd(value);
        return mgd.HasValue ? mgd.Value.ToString(FlowNumericFormat) : string.Empty;
    }

    public static string FormatFlowMonthlyLimitFromGpd(decimal? gpdLimit)
    {
        var mgd = NormalizeFlowValueToMgd(gpdLimit);
        return mgd.HasValue ? mgd.Value.ToString(FlowNumericFormat) : string.Empty;
    }

    public static string FormatFlowLimit(decimal? limit) => FormatFlowMonthlyLimitFromGpd(limit);

    public static string BuildFlowMonthlyLimitText(FacilityPermitTemplateParameter row)
    {
        if (row.MonthlyAverageLimit.HasValue)
        {
            return FormatFlowMonthlyLimitFromGpd(row.MonthlyAverageLimit);
        }

        if (row.MonthlyGeometricMeanLimit.HasValue)
        {
            return FormatFlowMonthlyLimitFromGpd(row.MonthlyGeometricMeanLimit);
        }

        return string.Empty;
    }
}

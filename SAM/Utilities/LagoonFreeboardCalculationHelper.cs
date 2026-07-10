namespace SAM.Utilities;

/// <summary>
/// Calculates storage lagoon freeboard from facility berm height and daily water depth.
/// </summary>
public static class LagoonFreeboardCalculationHelper
{
    public const string MissingBermHeightMessage =
        "Lagoon Berm Height must be configured on the facility before entering water depth.";

    public static decimal? CalculateFreeboardFeet(decimal? lagoonBermHeightFeet, decimal? waterDepthFt)
    {
        if (!lagoonBermHeightFeet.HasValue || !waterDepthFt.HasValue)
        {
            return null;
        }

        return lagoonBermHeightFeet.Value - waterDepthFt.Value;
    }

    /// <summary>
    /// Applies water depth to an operator log and computes storage freeboard when water depth is provided.
    /// When water depth is null, existing StorageFt is preserved (legacy rows).
    /// </summary>
    public static void ApplyWaterDepth(
        decimal? waterDepthFt,
        decimal? lagoonBermHeightFeet,
        Action<decimal?> setWaterDepth,
        Action<decimal?> setStorageFt,
        decimal? existingStorageFt)
    {
        setWaterDepth(waterDepthFt);

        if (!waterDepthFt.HasValue)
        {
            setStorageFt(existingStorageFt);
            return;
        }

        if (!lagoonBermHeightFeet.HasValue)
        {
            setStorageFt(existingStorageFt);
            return;
        }

        setStorageFt(CalculateFreeboardFeet(lagoonBermHeightFeet, waterDepthFt));
    }

    public static bool IsBelowPermittedMinimum(decimal? freeboardFt, decimal? permittedMinimumFreeboardFeet)
    {
        return freeboardFt.HasValue
            && permittedMinimumFreeboardFeet.HasValue
            && freeboardFt.Value < permittedMinimumFreeboardFeet.Value;
    }
}

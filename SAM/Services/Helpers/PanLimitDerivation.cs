namespace SAM.Services.Helpers;

/// <summary>
/// Centralized annual PAN limit derivation from crop N uptake.
/// </summary>
public static class PanLimitDerivation
{
    /// <summary>
    /// Derives annual PAN limit for compliance thresholding.
    /// PAN loading itself is calculated separately from wastewater chemistry inputs.
    /// </summary>
    public static decimal DeriveFromNUptake(decimal nUptake)
    {
        return nUptake < 0m ? 0m : nUptake;
    }
}




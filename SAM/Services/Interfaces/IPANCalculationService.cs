namespace SAM.Services.Interfaces;

/// <summary>
/// Result of PAN (Plant Available Nitrogen) calculation.
/// </summary>
public record PANCalculationResult(decimal PanLbs, decimal PanLoadingLbsPerAcre);

/// <summary>
/// Service for calculating PAN (Plant Available Nitrogen) and PAN Loading from wastewater chemistry and application data.
/// Formula: PAN (lbs) = { [MR×(TKN−NH₃)] + (1-VR)×NH₃ + NO₂ + NO₃ } × 8.34 × (volumeGallons/1_000_000);
/// PAN Loading (lbs/acre) = PAN (lbs) / acres.
/// </summary>
public interface IPANCalculationService
{
    /// <summary>
    /// Calculates PAN (lbs) and PAN Loading (lbs/acre) from chemistry and application parameters.
    /// </summary>
    /// <param name="tknMgL">Total Kjeldahl Nitrogen as N (mg/L). Treated as 0 if null.</param>
    /// <param name="nh3MgL">Ammonia as N (mg/L). Treated as 0 if null.</param>
    /// <param name="no2MgL">Nitrite as N (mg/L). Treated as 0 if null.</param>
    /// <param name="no3MgL">Nitrate as N (mg/L). Treated as 0 if null.</param>
    /// <param name="mineralizationRate">Mineralization rate as decimal 0-1 (e.g. 0.40 for 40%). Default 0.40.</param>
    /// <param name="volatilizationRate">Volatilization rate as decimal 0-1 (e.g. 0.50 for 50%). Default 0.50.</param>
    /// <param name="volumeGallons">Volume of wastewater applied (gallons).</param>
    /// <param name="acres">Sprayfield acreage. Must be &gt; 0 for PAN Loading; returns 0 if 0.</param>
    /// <returns>PAN (lbs) and PAN Loading (lbs/acre).</returns>
    PANCalculationResult Calculate(
        decimal? tknMgL,
        decimal? nh3MgL,
        decimal? no2MgL,
        decimal? no3MgL,
        decimal mineralizationRate = 0.40m,
        decimal volatilizationRate = 0.50m,
        decimal volumeGallons = 0m,
        decimal acres = 0m);
}

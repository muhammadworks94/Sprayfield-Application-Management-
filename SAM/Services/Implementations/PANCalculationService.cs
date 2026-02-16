using SAM.Services.Interfaces;

namespace SAM.Services.Implementations;

/// <summary>
/// Calculates PAN (Plant Available Nitrogen) and PAN Loading using the client formula.
/// PAN (lbs) = { [MR×(TKN−NH₃)] + (1-VR)×NH₃ + NO₂ + NO₃ } × 8.34 × (volumeGallons/1_000_000).
/// PAN Loading (lbs/acre) = PAN (lbs) / acres.
/// </summary>
public class PANCalculationService : IPANCalculationService
{
    private const decimal LbsPerMgalMgL = 8.34m;
    private const decimal GallonsPerMgal = 1_000_000m;

    /// <inheritdoc />
    public PANCalculationResult Calculate(
        decimal? tknMgL,
        decimal? nh3MgL,
        decimal? no2MgL,
        decimal? no3MgL,
        decimal mineralizationRate = 0.40m,
        decimal volatilizationRate = 0.50m,
        decimal volumeGallons = 0m,
        decimal acres = 0m)
    {
        var tkn = tknMgL ?? 0m;
        var nh3 = nh3MgL ?? 0m;
        var no2 = no2MgL ?? 0m;
        var no3 = no3MgL ?? 0m;

        // PAN (lbs) = { [MR×(TKN−NH₃)] + (1-VR)×NH₃ + NO₂ + NO₃ } × 8.34 × (volumeGallons/1_000_000)
        var organicN = (tkn - nh3) > 0 ? (tkn - nh3) : 0m;
        var mineralized = mineralizationRate * organicN;
        var nh3AfterVolatilization = (1m - volatilizationRate) * nh3;
        var concentrationTerm = mineralized + nh3AfterVolatilization + no2 + no3;
        var volumeMgal = volumeGallons / GallonsPerMgal;
        var panLbs = concentrationTerm * LbsPerMgalMgL * volumeMgal;

        var panLoadingLbsPerAcre = acres > 0 ? panLbs / acres : 0m;

        return new PANCalculationResult(panLbs, panLoadingLbsPerAcre);
    }
}

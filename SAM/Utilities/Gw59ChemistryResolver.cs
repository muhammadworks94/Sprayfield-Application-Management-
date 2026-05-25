using SAM.ViewModels.Reports;

namespace SAM.Utilities;

public sealed class Gw59ResolvedChemistry
{
    public decimal? TDS { get; init; }
    public decimal? Turbidity { get; init; }
    public decimal? TOC { get; init; }
    public decimal? Chloride { get; init; }
    public decimal? NH3N { get; init; }
    public decimal? NO3N { get; init; }
    public decimal? TKN { get; init; }
    public decimal? Calcium { get; init; }
    public decimal? Magnesium { get; init; }
    public decimal? FecalColiform { get; init; }
    public decimal? TotalColiform { get; init; }
}

public static class Gw59ChemistryResolver
{
    private static readonly string[] TdsPcsCodes = ["70300", "70295"];
    private static readonly string[] TurbidityPcsCodes = ["00076"];
    private static readonly string[] TocPcsCodes = ["00680", "00665"];
    private static readonly string[] ChloridePcsCodes = ["00940"];
    private static readonly string[] Nh3nPcsCodes = ["00610", "CO610"];
    private static readonly string[] No3nPcsCodes = ["00620"];
    private static readonly string[] TknPcsCodes = ["00625"];
    private static readonly string[] CalciumPcsCodes = ["00916"];
    private static readonly string[] MagnesiumPcsCodes = ["00927"];
    private static readonly string[] FecalColiformPcsCodes = ["31613", "31616"];
    private static readonly string[] TotalColiformPcsCodes = ["31504", "31505"];

    public static Gw59ResolvedChemistry Resolve(IEnumerable<Gw59ParameterSnapshot>? snapshots)
    {
        var rows = snapshots?.Where(x => x.IsGw59).ToList() ?? [];

        return new Gw59ResolvedChemistry
        {
            TDS = ResolveValue(rows, TdsPcsCodes),
            Turbidity = ResolveValue(rows, TurbidityPcsCodes),
            TOC = ResolveValue(rows, TocPcsCodes),
            Chloride = ResolveValue(rows, ChloridePcsCodes),
            NH3N = ResolveValue(rows, Nh3nPcsCodes),
            NO3N = ResolveValue(rows, No3nPcsCodes),
            TKN = ResolveValue(rows, TknPcsCodes),
            Calcium = ResolveValue(rows, CalciumPcsCodes),
            Magnesium = ResolveValue(rows, MagnesiumPcsCodes),
            FecalColiform = ResolveValue(rows, FecalColiformPcsCodes),
            TotalColiform = ResolveValue(rows, TotalColiformPcsCodes)
        };
    }

    private static decimal? ResolveValue(IReadOnlyCollection<Gw59ParameterSnapshot> rows, IEnumerable<string> pcsCodes)
    {
        foreach (var pcsCode in pcsCodes)
        {
            var match = rows.FirstOrDefault(x =>
                string.Equals(x.PcsCode, pcsCode, StringComparison.OrdinalIgnoreCase) &&
                x.Value.HasValue);
            if (match?.Value.HasValue == true)
            {
                return match.Value.Value;
            }
        }

        return null;
    }
}

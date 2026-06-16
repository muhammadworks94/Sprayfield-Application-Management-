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
    public decimal? PHLab { get; init; }
    public decimal? PhosphorusTotal { get; init; }
}

public static class Gw59ChemistryResolver
{
    private const string VocPcsCode = "78732";
    private const int Gw59OtherLineLimit = Gw59PdfCalibration.OtherLineLimit;
    private static readonly string[] TdsPcsCodes = ["70300", "70295"];
    private static readonly string[] TurbidityPcsCodes = ["00076"];
    private static readonly string[] CodPcsCodes = ["00340"];
    private static readonly string[] PhLabPcsCodes = ["00403", "00400"];
    private static readonly string[] WaterLevelPcsCodes = ["82546"];
    private static readonly string[] TocPcsCodes = ["00680"];
    private static readonly string[] ChloridePcsCodes = ["00940"];
    private static readonly string[] ArsenicPcsCodes = ["01002"];
    private static readonly string[] GreaseAndOilsPcsCodes = ["00552"];
    private static readonly string[] PhenolPcsCodes = ["32730"];
    private static readonly string[] SulfatePcsCodes = ["00945"];
    private static readonly string[] Nh3nPcsCodes = ["00610", "CO610"];
    private static readonly string[] No3nPcsCodes = ["00620"];
    private static readonly string[] TknPcsCodes = ["00625"];
    private static readonly string[] OrthophosphatePcsCodes = ["70507", "00660"];
    private static readonly string[] PhosphorusTotalPcsCodes = ["00665"];
    private static readonly string[] CalciumPcsCodes = ["00916"];
    private static readonly string[] MagnesiumPcsCodes = ["00927"];
    private static readonly string[] BariumPcsCodes = ["01007"];
    private static readonly string[] CadmiumPcsCodes = ["01027"];
    private static readonly string[] ChromiumPcsCodes = ["01034"];
    private static readonly string[] CopperPcsCodes = ["01042"];
    private static readonly string[] IronPcsCodes = ["01045"];
    private static readonly string[] MercuryPcsCodes = ["71900"];
    private static readonly string[] PotassiumPcsCodes = ["00937"];
    private static readonly string[] ManganesePcsCodes = ["01055"];
    private static readonly string[] NickelPcsCodes = ["01067"];
    private static readonly string[] LeadPcsCodes = ["01051"];
    private static readonly string[] ZincPcsCodes = ["01092"];
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
            TotalColiform = ResolveValue(rows, TotalColiformPcsCodes),
            PHLab = ResolveValue(rows, PhLabPcsCodes),
            PhosphorusTotal = ResolveValue(rows, PhosphorusTotalPcsCodes)
        };
    }

    public static IReadOnlyList<Gw59OtherParameterLine> ResolveOtherLines(IEnumerable<Gw59ParameterSnapshot>? snapshots)
    {
        var rows = snapshots?.Where(x => x.IsGw59).ToList() ?? [];
        return rows
            .Where(x => x.Value.HasValue)
            .Where(x => !string.Equals(x.PcsCode, VocPcsCode, StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.Equals(x.PcsCode, WaterLevelPcsCodes[0], StringComparison.OrdinalIgnoreCase))
            .Where(x => !Gw59PdfCalibration.HasNamedSlot(x.PcsCode))
            .Where(x => !x.ParameterName.Contains("recoverable", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PcsCode, StringComparer.OrdinalIgnoreCase)
            .Take(Gw59OtherLineLimit)
            .Select(x => new Gw59OtherParameterLine
            {
                PcsCode = x.PcsCode,
                ParameterName = x.ParameterName,
                Units = x.Units,
                Value = x.Value!.Value
            })
            .ToList();
    }

    public static decimal? ResolveWaterLevel(decimal? gwMonitWaterLevel, IEnumerable<Gw59ParameterSnapshot>? snapshots)
    {
        if (gwMonitWaterLevel.HasValue)
        {
            return gwMonitWaterLevel.Value;
        }

        var rows = snapshots?.Where(x => x.IsGw59).ToList() ?? [];
        return ResolveValue(rows, WaterLevelPcsCodes);
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

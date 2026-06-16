using SAM.ViewModels.Reports;

namespace SAM.Utilities;

public sealed class Gw59LabPdfSlot
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 55;
    public string ValueFormat { get; init; } = "F2";
}

public static class Gw59LabPdfCalibration
{
    public const int OtherLineLimit = 10;

    /// <summary>Y values in the slot map are form underline positions; render uses baseline alignment.</summary>
    public const double BaselineAboveUnderline = 1.5;

    private const double OtherLineStep = 14.5;
    private const double OtherStartY = 409;
    private const double OtherLeftX = 582;
    private const double OtherRightX = 685;
    private const double OtherLeftWidth = 98;
    private const double OtherRightWidth = 95;

    private static readonly Dictionary<string, Gw59LabPdfSlot> NamedSlots = BuildNamedSlots();

    public static bool HasNamedSlot(string? pcsCode) =>
        !string.IsNullOrWhiteSpace(pcsCode) && NamedSlots.ContainsKey(pcsCode);

    public static bool TryGetNamedSlot(string? pcsCode, out Gw59LabPdfSlot slot)
    {
        if (!string.IsNullOrWhiteSpace(pcsCode) && NamedSlots.TryGetValue(pcsCode, out var match))
        {
            slot = match;
            return true;
        }

        slot = null!;
        return false;
    }

    public static Gw59LabPdfSlot GetOtherSlot(int index)
    {
        if (index < 0 || index >= OtherLineLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var row = index % 5;
        var isRightColumn = index >= 5;
        return new Gw59LabPdfSlot
        {
            X = isRightColumn ? OtherRightX : OtherLeftX,
            Y = OtherStartY + (row * OtherLineStep),
            Width = isRightColumn ? OtherRightWidth : OtherLeftWidth
        };
    }

    public static string GetSlotPositionKey(Gw59LabPdfSlot slot) =>
        $"{slot.X:0.##}:{slot.Y:0.##}";

    public static string FormatSlotValue(decimal value, Gw59LabPdfSlot slot) =>
        value.ToString(slot.ValueFormat);

    public static double GetBaselineY(double underlineY) => underlineY - BaselineAboveUnderline;

    public static string FormatOtherLine(Gw59OtherParameterLine line) =>
        FormatOtherLine(line.ParameterName, line.Value, line.Units);

    public static string FormatOtherLine(string parameterName, decimal value, string? units)
    {
        var unitsSuffix = string.IsNullOrWhiteSpace(units) ? string.Empty : $" {units.Trim()}";
        var name = AbbreviateOtherParameterName(parameterName);
        return $"{name}, {value:0.######}{unitsSuffix}";
    }

    private static string AbbreviateOtherParameterName(string parameterName)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
        {
            return string.Empty;
        }

        return parameterName.Trim() switch
        {
            "Total Suspended Solids" => "TSS",
            "Total Nitrogen" => "Total N",
            "Nitrite + Nitrate" => "Nitrite+Nitrate",
            "Dissolved Organic Carbon" => "DOC",
            "Organic Phosphorus" => "Org P",
            "Volatile Compounds" => "VOC",
            _ when parameterName.Length > 22 => parameterName[..21].TrimEnd() + "…",
            _ => parameterName.Trim()
        };
    }

    public static IReadOnlyList<Gw59ParameterSnapshot> SelectNamedSnapshots(IEnumerable<Gw59ParameterSnapshot>? snapshots)
    {
        var drawnPositions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<Gw59ParameterSnapshot>();

        var rows = snapshots?
            .Where(x => x.IsGw59 && x.Value.HasValue && HasNamedSlot(x.PcsCode))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.PcsCode, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];

        foreach (var snapshot in rows)
        {
            if (!TryGetNamedSlot(snapshot.PcsCode, out var slot))
            {
                continue;
            }

            var positionKey = GetSlotPositionKey(slot);
            if (!drawnPositions.Add(positionKey))
            {
                continue;
            }

            results.Add(snapshot);
        }

        return results;
    }

    private static Dictionary<string, Gw59LabPdfSlot> BuildNamedSlots()
    {
        var slots = new Dictionary<string, Gw59LabPdfSlot>(StringComparer.OrdinalIgnoreCase);

        void Add(string pcsCode, double x, double y, string valueFormat = "F2", double width = 55)
        {
            slots[pcsCode] = new Gw59LabPdfSlot
            {
                X = x,
                Y = y,
                Width = width,
                ValueFormat = valueFormat
            };
        }

        // Left column — Y = underline position on form
        Add("00340", 165, 344);
        Add("31616", 165, 360, "F0");
        Add("31613", 165, 360, "F0");
        Add("31504", 165, 377, "F0");
        Add("31505", 165, 377, "F0");
        Add("70300", 165, 408);
        Add("70295", 165, 408);
        Add("00403", 165, 423);
        Add("00400", 165, 423);
        Add("00680", 165, 438);
        Add("00940", 165, 454);
        Add("01002", 165, 470);
        Add("00552", 165, 486);
        Add("32730", 165, 502);
        Add("00945", 165, 518);
        Add("00095", 165, 534);
        Add("00610", 165, 546);
        Add("CO610", 165, 546);
        Add("00625", 165, 579);

        // Middle column
        Add("00615", 440, 344);
        Add("00620", 440, 360);
        Add("00665", 440, 376);
        Add("70507", 440, 392);
        Add("00660", 440, 392);
        Add("01105", 440, 408);
        Add("01007", 440, 422);
        Add("00916", 440, 438);
        Add("01027", 440, 454);
        Add("01034", 440, 470);
        Add("01042", 440, 486);
        Add("01045", 440, 502);
        Add("71900", 440, 518);
        Add("00937", 440, 534);
        Add("00927", 440, 546);
        Add("01055", 440, 562);
        Add("01067", 440, 578);

        // Right column (Pb/Zn above Other)
        Add("01051", 683, 343);
        Add("01092", 683, 359);

        return slots;
    }
}

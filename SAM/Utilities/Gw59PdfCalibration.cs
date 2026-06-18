using SAM.ViewModels.Reports;

namespace SAM.Utilities;

public sealed class Gw59LabPdfSlot
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 55;
    public string ValueFormat { get; init; } = "F2";
}

public sealed class Gw59PdfTextSlot
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 260;
}

public sealed class Gw59PdfUnderlineSlot
{
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 55;
}

public sealed class Gw59PdfFieldMap
{
    public Gw59PdfTextSlot FacilityName { get; init; } = null!;
    public Gw59PdfTextSlot PermitNumber { get; init; } = null!;
    public Gw59PdfTextSlot Permittee { get; init; } = null!;
    public Gw59PdfTextSlot Address { get; init; } = null!;
    public Gw59PdfTextSlot City { get; init; } = null!;
    public Gw59PdfTextSlot ZipCode { get; init; } = null!;
    public Gw59PdfTextSlot State { get; init; } = null!;
    public Gw59PdfTextSlot County { get; init; } = null!;
    public Gw59PdfTextSlot PermitExpirationDate { get; init; } = null!;
    public Gw59PdfTextSlot ContactPerson { get; init; } = null!;
    public Gw59PdfTextSlot FacilityPhone { get; init; } = null!;
    public Gw59PdfTextSlot WellLocation { get; init; } = null!;
    public Gw59PdfTextSlot NumberOfWellsToBeSampled { get; init; } = null!;
    public Gw59PdfTextSlot WellId { get; init; } = null!;
    public Gw59PdfTextSlot SampleDate { get; init; } = null!;
    public Gw59PdfTextSlot WellDepthFeet { get; init; } = null!;
    public Gw59PdfTextSlot DiameterInches { get; init; } = null!;
    public Gw59PdfTextSlot ScreenedIntervalFromFeet { get; init; } = null!;
    public Gw59PdfTextSlot ScreenedIntervalToFeet { get; init; } = null!;
    public Gw59PdfTextSlot RelativeMpElevation { get; init; } = null!;
    public Gw59PdfTextSlot WaterLevel { get; init; } = null!;
    public Gw59PdfTextSlot MeasuringPointAboveLandSurface { get; init; } = null!;
    public Gw59PdfTextSlot GallonsPumped { get; init; } = null!;
    public Gw59PdfTextSlot PHField { get; init; } = null!;
    public Gw59PdfTextSlot TemperatureField { get; init; } = null!;
    public Gw59PdfTextSlot SpecificConductance { get; init; } = null!;
    public Gw59PdfTextSlot Odor { get; init; } = null!;
    public Gw59PdfTextSlot Appearance { get; init; } = null!;
    public Gw59PdfTextSlot MetalsUnfilteredYes { get; init; } = null!;
    public Gw59PdfTextSlot MetalsUnfilteredNo { get; init; } = null!;
    public Gw59PdfTextSlot MetalsAcidifiedYes { get; init; } = null!;
    public Gw59PdfTextSlot MetalsAcidifiedNo { get; init; } = null!;
    public Gw59PdfUnderlineSlot LabSampleAnalyzedDate { get; init; } = null!;
    public Gw59PdfUnderlineSlot LabName { get; init; } = null!;
    public Gw59PdfUnderlineSlot LabCertificationNumber { get; init; } = null!;
    public Gw59PdfTextSlot LabReportAttachedYes { get; init; } = null!;
    public Gw59PdfTextSlot LabReportAttachedNo { get; init; } = null!;
    public Gw59PdfUnderlineSlot VOCMethodNumber { get; init; } = null!;
    public Gw59PdfTextSlot OperationLagoonTick { get; init; } = null!;
    public Gw59PdfTextSlot OperationSprayFieldTick { get; init; } = null!;
    public Gw59PdfTextSlot CertificationNameTitle { get; init; } = null!;
    public Gw59PdfTextSlot CertificationDate { get; init; } = null!;
}

public static class Gw59PdfCalibration
{
    public const string TemplateFileName = "GW59_Resized.pdf";

    public const double PageWidth = 792;
    public const double PageHeight = 612;

    public const int OtherLineLimit = 10;
    public const double BaselineAboveUnderline = 2.5;

    // Value boxes sit on the underline to the right of the printed PCS label text.
    private const double LabLeftX = 172;
    private const double LabMiddleX = 392;
    private const double LabRightX = 554;
    private const double LabLeftWidth = 55;
    private const double LabMiddleWidth = 58;
    private const double LabRightWidth = 80;

    private static readonly double[] LabLeftRowY =
        [307.4, 320.4, 333.8, 360.2, 373.3, 386.6, 399.7, 413.0, 426.3, 439.4, 452.5, 465.8, 478.4, 505.3];

    private static readonly double[] LabMiddleRowY =
        [307.4, 320.4, 333.8, 346.4, 360.2, 373.3, 386.6, 399.7, 413.0, 426.3, 439.4, 452.5, 465.8, 478.4, 492.2, 505.3];

    // Ten "Other" slots: two columns in the wide block above ORGANICS (measured y≈360–413).
    private static readonly double[] OtherRowY = [360.2, 373.3, 386.6, 399.7, 413.0];

    private const double OtherLeftX = 498;
    private const double OtherRightX = 582;
    private const double OtherLeftWidth = 78;
    private const double OtherRightWidth = 120;

    public static Gw59PdfFieldMap Fields { get; } = BuildFields();

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

        var row = index % OtherRowY.Length;
        var isRightColumn = index >= OtherRowY.Length;
        return new Gw59LabPdfSlot
        {
            X = isRightColumn ? OtherRightX : OtherLeftX,
            Y = OtherRowY[row],
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

    private static Gw59PdfFieldMap BuildFields() => new()
    {
        FacilityName = TextOnLine(110, 86.8, 340),
        PermitNumber = TextOnLine(531.7, 86.8, 75),
        Permittee = TextOnLine(145.1, 99.8, 300),
        Address = TextOnLine(163.1, 113.6, 285),
        City = TextOnLine(82.9, 126.2, 118),
        State = TextOnLine(206.6, 126.2, 45),
        ZipCode = TextOnLine(248, 126.2, 50),
        County = TextOnLine(355, 126.2, 95),
        PermitExpirationDate = TextOnLine(664.8, 75.1, 55),
        ContactPerson = TextOnLine(110, 152.5, 240),
        FacilityPhone = TextOnLine(355, 152.5, 95),
        WellLocation = TextOnLine(136.6, 165.8, 180),
        NumberOfWellsToBeSampled = TextOnLine(417.8, 165.8, 28),
        WellId = TextOnLine(182.9, 196.1, 95),
        SampleDate = TextOnLine(395.4, 196.1, 70),
        WellDepthFeet = TextOnLine(145.1, 209.4, 21),
        DiameterInches = TextOnLine(395.3, 209.4, 23),
        WaterLevel = TextOnLine(145.1, 222.5, 38),
        ScreenedIntervalFromFeet = TextOnLine(395.3, 222.5, 23),
        ScreenedIntervalToFeet = TextOnLine(452.4, 222.5, 19),
        MeasuringPointAboveLandSurface = TextOnLine(127.3, 235.8, 38),
        RelativeMpElevation = TextOnLine(395.3, 235.8, 45),
        GallonsPumped = TextOnLine(229.2, 248.9, 52),
        PHField = TextOnLine(531.7, 209.4, 22),
        TemperatureField = TextOnLine(643.5, 209.4, 30),
        SpecificConductance = TextOnLine(566.0, 222.5, 70),
        Odor = TextOnLine(553.7, 235.8, 80),
        Appearance = TextOnLine(553.7, 248.9, 80),
        MetalsUnfilteredYes = CheckboxSlot(220.1, 252.6),
        MetalsUnfilteredNo = CheckboxSlot(271.2, 252.6),
        MetalsAcidifiedYes = CheckboxSlot(385.7, 252.6),
        MetalsAcidifiedNo = CheckboxSlot(430.0, 252.6),
        LabSampleAnalyzedDate = UnderlineOnLine(127.3, 283.2, 154),
        LabName = UnderlineOnLine(376.8, 283.2, 177),
        LabCertificationNumber = UnderlineOnLine(636.5, 283.2, 48),
        LabReportAttachedYes = CheckboxSlot(592.4, 442.6),
        LabReportAttachedNo = CheckboxSlot(648.8, 442.6),
        VOCMethodNumber = UnderlineOnLine(638, 465.4, 68),
        OperationLagoonTick = CheckboxSlot(483.6, 116.6),
        OperationSprayFieldTick = CheckboxSlot(483.6, 129.4),
        CertificationNameTitle = TextOnLine(40.4, 569.0, 315),
        CertificationDate = TextOnLine(637, 569.0, 55)
    };

    private static Dictionary<string, Gw59LabPdfSlot> BuildNamedSlots()
    {
        var slots = new Dictionary<string, Gw59LabPdfSlot>(StringComparer.OrdinalIgnoreCase);

        void AddLeft(string pcsCode, int row, string valueFormat = "F2") =>
            AddSlot(slots, pcsCode, LabLeftX, LabLeftRowY[row], LabLeftWidth, valueFormat);

        void AddMiddle(string pcsCode, int row, string valueFormat = "F2") =>
            AddSlot(slots, pcsCode, LabMiddleX, LabMiddleRowY[row], LabMiddleWidth, valueFormat);

        void AddRight(string pcsCode, int row, string valueFormat = "F2") =>
            AddSlot(slots, pcsCode, LabRightX, LabMiddleRowY[row], LabRightWidth, valueFormat);

        AddLeft("00340", 0);
        AddLeft("00335", 0);
        AddLeft("31616", 1, "F0");
        AddLeft("31613", 1, "F0");
        AddLeft("31504", 2, "F0");
        AddLeft("31505", 2, "F0");
        AddLeft("70300", 3);
        AddLeft("70295", 3);
        AddLeft("00403", 4);
        AddLeft("00400", 4);
        AddLeft("00680", 5);
        AddLeft("00940", 6);
        AddLeft("01002", 7);
        AddLeft("00552", 8);
        AddLeft("32730", 9);
        AddLeft("00945", 10);
        AddLeft("00095", 11);
        AddLeft("00610", 12);
        AddLeft("CO610", 12);
        AddLeft("00625", 13);

        AddMiddle("00615", 0);
        AddMiddle("00620", 1);
        AddMiddle("00665", 2);
        AddMiddle("70507", 3);
        AddMiddle("00660", 3);
        AddMiddle("01105", 4);
        AddMiddle("01007", 5);
        AddMiddle("00916", 6);
        AddMiddle("01027", 7);
        AddMiddle("01034", 8);
        AddMiddle("01042", 9);
        AddMiddle("01045", 10);
        AddMiddle("71900", 11);
        AddMiddle("00937", 12);
        AddMiddle("00927", 13);
        AddMiddle("01055", 14);
        AddMiddle("01067", 15);

        AddRight("01051", 0);
        AddRight("01092", 1);

        return slots;
    }

    private static void AddSlot(
        Dictionary<string, Gw59LabPdfSlot> slots,
        string pcsCode,
        double x,
        double underlineY,
        double width,
        string valueFormat)
    {
        slots[pcsCode] = new Gw59LabPdfSlot
        {
            X = x,
            Y = underlineY,
            Width = width,
            ValueFormat = valueFormat
        };
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

    private static Gw59PdfTextSlot TextOnLine(double x, double underlineY, double width) =>
        new()
        {
            X = x,
            Y = underlineY,
            Width = width
        };

    private static Gw59PdfTextSlot CheckboxSlot(double boxLeft, double boxTop) =>
        new()
        {
            X = boxLeft,
            Y = boxTop,
            Width = 8
        };

    private static Gw59PdfUnderlineSlot UnderlineOnLine(double x, double underlineY, double width) =>
        new()
        {
            X = x,
            Y = underlineY,
            Width = width
        };
}

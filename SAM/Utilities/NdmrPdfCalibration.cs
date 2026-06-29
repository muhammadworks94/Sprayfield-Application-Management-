namespace SAM.Utilities;

/// <summary>
/// PDF anchor points for NDMR Form 03-12.
/// PdfSharp DrawString uses TopLeft: Y is the top edge of the text box (Arial 8pt).
/// </summary>
public sealed class NdmrPdfTextSlot
{
    public double X { get; init; }
    public double Y { get; init; }
    /// <summary>When set, X/Y are the checkbox top-left corner for DrawCheckboxX.</summary>
    public double? BoxSize { get; init; }
}

public sealed class NdmrPdfMonitoringOptionSlot
{
    public double BoxLeft { get; init; }
    public double BoxTop { get; init; }
    public double BoxSize { get; init; } = 6.75d;
    /// <summary>Fine-tune X mark placement on the imported template.</summary>
    public double MarkOffsetX { get; init; }
    public double MarkOffsetY { get; init; } = 0.5d;
}

public sealed class NdmrPdfRectSlot
{
    public double Left { get; init; }
    public double Top { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
}

public sealed class NdmrPdfHeaderFieldMap
{
    public NdmrPdfTextSlot Ppi { get; init; } = null!;
    public double PpiWidth { get; init; }
    public double HeaderRowTop { get; init; }
    public double HeaderRowHeight { get; init; }
    public IReadOnlyList<NdmrPdfMonitoringOptionSlot> FlowOptions { get; init; } = Array.Empty<NdmrPdfMonitoringOptionSlot>();
    public IReadOnlyList<NdmrPdfMonitoringOptionSlot> ParameterOptions { get; init; } = Array.Empty<NdmrPdfMonitoringOptionSlot>();
}

public sealed class NdmrParameterColumnSlot
{
    public double Left { get; init; }
    public double Width { get; init; }
    public double CodeY { get; init; }
    public double NameTopY { get; init; }
    public double NameHeight { get; init; }
    public double UnitsY { get; init; }
    public double CenterX => Left + (Width / 2d);
}

public sealed class NdmrParameterGridMap
{
    public NdmrParameterColumnSlot FlowColumn { get; init; } = null!;
    public IReadOnlyList<NdmrParameterColumnSlot> DynamicColumns { get; init; } = Array.Empty<NdmrParameterColumnSlot>();
}

public sealed class NdmrCertificationFieldMap
{
    public NdmrPdfTextSlot PageNumber { get; init; } = null!;
    public NdmrPdfTextSlot TotalPages { get; init; } = null!;
    public NdmrPdfTextSlot SamplingPerson1 { get; init; } = null!;
    public NdmrPdfTextSlot SamplingPerson2 { get; init; } = null!;
    public NdmrPdfTextSlot CertifiedLab1 { get; init; } = null!;
    public NdmrPdfTextSlot CertifiedLab2 { get; init; } = null!;
    public NdmrPdfTextSlot Compliant { get; init; } = null!;
    public NdmrPdfTextSlot NonCompliant { get; init; } = null!;
    public NdmrPdfTextSlot OrcName { get; init; } = null!;
    public NdmrPdfTextSlot OrcCertificationNo { get; init; } = null!;
    public NdmrPdfTextSlot OrcGrade { get; init; } = null!;
    public NdmrPdfTextSlot OrcPhone { get; init; } = null!;
    public NdmrPdfTextSlot OrcChangedYes { get; init; } = null!;
    public NdmrPdfTextSlot OrcChangedNo { get; init; } = null!;
    public NdmrPdfTextSlot Permittee { get; init; } = null!;
    public NdmrPdfTextSlot SigningOfficial { get; init; } = null!;
    public NdmrPdfTextSlot SigningOfficialTitle { get; init; } = null!;
    public NdmrPdfTextSlot PermitteePhone { get; init; } = null!;
    public NdmrPdfTextSlot PermitExpiration { get; init; } = null!;
}

public static class NdmrPdfCalibration
{
    public const string TemplateFileName = "Non-Discharge Monitoring Report (NDMR) Form 0312.pdf";
    private const double CheckboxSize = 8d;

    /// <summary>Pre-printed flow column stripe on the NDMR template (#cccccc).</summary>
    public const byte FlowColumnShadeRed = 204;
    public const byte FlowColumnShadeGreen = 204;
    public const byte FlowColumnShadeBlue = 204;

    public static NdmrParameterGridMap ParameterGrid { get; } = BuildParameterGridMap();
    public static NdmrCertificationFieldMap Certification { get; } = BuildCertificationMap();
    public static NdmrPdfHeaderFieldMap Header { get; } = BuildHeaderMap();

    /// <summary>Inset patch over pre-printed GPD in the flow units cell (template-measured).</summary>
    public static NdmrPdfRectSlot FlowUnitsGpdCover { get; } = new()
    {
        Left = 122.5d,
        Top = 143.2d,
        Width = 20d,
        Height = 6.8d
    };

    /// <summary>Vertical fine-tune for the MGD units label in the flow column.</summary>
    public const double FlowUnitsLabelOffsetY = 2d;

    private static NdmrPdfHeaderFieldMap BuildHeaderMap() => new()
    {
        // Value only — "PPI:" label is pre-printed on the template.
        Ppi = Slot(60d, 58d),
        PpiWidth = 18d,
        HeaderRowTop = 54d,
        HeaderRowHeight = 10d,
        FlowOptions = new[]
        {
            MonitoringOption(230.75d, 54.75d, -18.2d),
            MonitoringOption(270.38d, 54.75d, -17d),
            MonitoringOption(305.5d, 54.12d, -18.1d)
        },
        ParameterOptions = new[]
        {
            MonitoringOption(548.5d, 54.38d, -20.2d),
            MonitoringOption(591.5d, 54.5d, -21.5d),
            MonitoringOption(633.5d, 54.25d, -23d),
            MonitoringOption(677.25d, 54.25d, -21.5d)
        }
    };

    private static NdmrPdfMonitoringOptionSlot MonitoringOption(double boxLeft, double boxTop, double markOffsetX) =>
        new()
        {
            BoxLeft = boxLeft,
            BoxTop = boxTop,
            MarkOffsetX = markOffsetX,
            MarkOffsetY = 1.25d
        };

    private static NdmrParameterGridMap BuildParameterGridMap() => new()
    {
        FlowColumn = Column(110.8, 43.4, 74.5, 87.0, 53.8, 142.5),
        DynamicColumns = new NdmrParameterColumnSlot[]
        {
            Column(154.8, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(195.8, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(236.8, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(277.8, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(318.8, 40.2, 74.5, 87.0, 53.8, 142.5),
            Column(360.0, 39.8, 74.5, 87.0, 53.8, 142.5),
            Column(400.8, 40.2, 74.5, 87.0, 53.8, 142.5),
            Column(442.0, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(483.0, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(524.0, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(565.0, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(606.0, 40.2, 74.5, 87.0, 53.8, 142.5),
            Column(647.2, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(688.2, 40.0, 74.5, 87.0, 53.8, 142.5),
            Column(729.2, 40.0, 74.5, 87.0, 53.8, 142.5),
        }
    };

    private static NdmrCertificationFieldMap BuildCertificationMap() => new()
    {
        PageNumber = Slot(685, 16),
        TotalPages = Slot(720, 16),
        SamplingPerson1 = Slot(64, 67),
        SamplingPerson2 = Slot(64, 90),
        CertifiedLab1 = Slot(435, 67),
        CertifiedLab2 = Slot(435, 90),
        Compliant = CheckSlot(624.5, 109),
        NonCompliant = CheckSlot(693.5, 109),
        OrcName = Slot(50, 326),
        OrcCertificationNo = Slot(95, 350),
        OrcGrade = Slot(52.5, 374),
        OrcPhone = Slot(220, 374),
        OrcChangedYes = Slot(245, 393),
        OrcChangedNo = Slot(284, 393),
        Permittee = Slot(449, 326),
        SigningOfficial = Slot(469, 350),
        SigningOfficialTitle = Slot(500, 374),
        PermitteePhone = Slot(470, 398),
        PermitExpiration = Slot(680, 398)
    };

    private static NdmrParameterColumnSlot Column(
        double left,
        double width,
        double codeY,
        double nameTopY,
        double nameHeight,
        double unitsY) =>
        new()
        {
            Left = left,
            Width = width,
            CodeY = codeY,
            NameTopY = nameTopY,
            NameHeight = nameHeight,
            UnitsY = unitsY
        };

    private static NdmrPdfTextSlot Slot(double x, double y) => new() { X = x, Y = y };

    private static NdmrPdfTextSlot CheckSlot(double x, double y) => new() { X = x, Y = y, BoxSize = CheckboxSize };
}

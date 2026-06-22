namespace SAM.Utilities;

/// <summary>
/// PDF anchor points for NDMR Form 03-12 page 2 (certification).
/// PdfSharp DrawString uses TopLeft: Y is the top edge of the text box (Arial 8pt).
/// </summary>
public sealed class NdmrPdfTextSlot
{
    public double X { get; init; }
    public double Y { get; init; }
    /// <summary>When set, X/Y are the checkbox top-left corner for DrawCheckboxX.</summary>
    public double? BoxSize { get; init; }
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

    public static NdmrCertificationFieldMap Certification { get; } = BuildCertificationMap();

    private static NdmrCertificationFieldMap BuildCertificationMap() => new()
    {
        PageNumber = Slot(685, 16),
        TotalPages = Slot(720, 16),
        // Sampling persons: x-8, y-5
        SamplingPerson1 = Slot(64, 67),
        SamplingPerson2 = Slot(64, 90),
        // Certified labs: x+3, y-5, then y+2
        CertifiedLab1 = Slot(435, 67),
        CertifiedLab2 = Slot(435, 90),
        // Compliance row checkboxes — measured + visual tune (-8 X, +2 Y)
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

    private static NdmrPdfTextSlot Slot(double x, double y) => new() { X = x, Y = y };

    private static NdmrPdfTextSlot CheckSlot(double x, double y) => new() { X = x, Y = y, BoxSize = CheckboxSize };
}

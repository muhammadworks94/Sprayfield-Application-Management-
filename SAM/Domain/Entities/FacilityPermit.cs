using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

public class FacilityPermit : CompanyScopedEntity
{
    public Guid FacilityId { get; set; }
    public string PermitNumber { get; set; } = string.Empty;
    public string PermitVersion { get; set; } = string.Empty;
    public DateTime EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public bool GwOperationLagoon { get; set; } = true;
    public bool GwOperationSprayField { get; set; } = true;
    public string? Notes { get; set; }
    public string? PermitPdfFileName { get; set; }
    public string? PermitPdfStoragePath { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? County { get; set; }
    public int? TotalNumberOfSprayfields { get; set; }
    public decimal? PermittedMinimumFreeboardFeet { get; set; }

    public Facility? Facility { get; set; }
    public ICollection<FacilityPermitTemplateParameter> TemplateParameters { get; set; } = new List<FacilityPermitTemplateParameter>();
    public ICollection<WWChar> WWChars { get; set; } = new List<WWChar>();
}

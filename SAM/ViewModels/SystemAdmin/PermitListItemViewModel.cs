namespace SAM.ViewModels.SystemAdmin;

public class PermitListItemViewModel
{
    public Guid Id { get; set; }
    public Guid FacilityId { get; set; }
    public string FacilityName { get; set; } = string.Empty;
    public string PermitNumber { get; set; } = string.Empty;
    public string PermitVersion { get; set; } = string.Empty;
    public DateTime EffectiveStartDate { get; set; }
    public DateTime? EffectiveEndDate { get; set; }
    public bool IsActive { get; set; }
    public bool GwOperationLagoon { get; set; }
    public bool GwOperationSprayField { get; set; }
    public string? County { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public int? TotalNumberOfSprayfields { get; set; }
    public bool HasPdf { get; set; }
}

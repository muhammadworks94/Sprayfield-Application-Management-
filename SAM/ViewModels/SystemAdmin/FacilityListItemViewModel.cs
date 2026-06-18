namespace SAM.ViewModels.SystemAdmin;

/// <summary>
/// Facility-owned fields for list views. Permit and lab data live on their respective tabs.
/// </summary>
public class FacilityListItemViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Permittee { get; set; } = string.Empty;
    public string FacilityClass { get; set; } = string.Empty;
}

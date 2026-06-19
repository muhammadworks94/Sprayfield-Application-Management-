using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Company-scoped certified laboratory option for GW-59 and related reports.
/// </summary>
public class CompanyLabOption : CompanyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string CertificationNumber { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WWChar> WWChars { get; set; } = new List<WWChar>();
    public ICollection<GWMonit> GWMonits { get; set; } = new List<GWMonit>();
}

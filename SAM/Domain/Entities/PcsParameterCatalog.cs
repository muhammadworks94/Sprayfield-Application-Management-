using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

public class PcsParameterCatalog : AuditableEntity
{
    public string PcsCode { get; set; } = string.Empty;
    public string UserFriendlyName { get; set; } = string.Empty;
    public string OfficialParameterName { get; set; } = string.Empty;
    public string AcceptedUnits { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<FacilityPermitTemplateParameter> PermitTemplateParameters { get; set; } = new List<FacilityPermitTemplateParameter>();
}


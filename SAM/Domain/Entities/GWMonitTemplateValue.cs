using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

public class GWMonitTemplateValue : CompanyScopedEntity
{
    public Guid GWMonitId { get; set; }
    public Guid FacilityPermitTemplateParameterId { get; set; }
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }

    public GWMonit? GWMonit { get; set; }
    public FacilityPermitTemplateParameter? FacilityPermitTemplateParameter { get; set; }
}


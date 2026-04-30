using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

public class WWCharTemplateValue : CompanyScopedEntity
{
    public Guid WWCharId { get; set; }
    public Guid FacilityPermitTemplateParameterId { get; set; }
    public int DayNo { get; set; } // 1..31
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }

    public WWChar? WWChar { get; set; }
    public FacilityPermitTemplateParameter? FacilityPermitTemplateParameter { get; set; }
}


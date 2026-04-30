using SAM.Domain.Entities.Base;
using SAM.Domain.Enums;

namespace SAM.Domain.Entities;

public class FacilityPermitTemplateParameter : CompanyScopedEntity
{
    public Guid FacilityPermitId { get; set; }
    public Guid PcsParameterCatalogId { get; set; }
    public string? ParameterDisplayOverride { get; set; }
    public string? UnitsOverride { get; set; }

    public decimal? MonthlyAverageLimit { get; set; }
    public decimal? MonthlyGeometricMeanLimit { get; set; }
    public decimal? DailyMinimumLimit { get; set; }
    public decimal? DailyMaximumLimit { get; set; }

    public SampleTypeEnum SampleType { get; set; }
    public MeasurementFrequencyEnum MeasurementFrequency { get; set; }
    public string? ScheduledMonthsCsv { get; set; } // e.g. "4,8,11"
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public PermitTemplateReportTypeEnum ReportTypes { get; set; } = PermitTemplateReportTypeEnum.Ndmr;

    public FacilityPermit? FacilityPermit { get; set; }
    public PcsParameterCatalog? PcsParameterCatalog { get; set; }
    public ICollection<WWCharTemplateValue> WWCharTemplateValues { get; set; } = new List<WWCharTemplateValue>();
    public ICollection<GWMonitTemplateValue> GWMonitTemplateValues { get; set; } = new List<GWMonitTemplateValue>();
}


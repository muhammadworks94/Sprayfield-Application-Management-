namespace SAM.Services.Models;

public class ComplianceProjectionRequest
{
    public Guid FacilityId { get; set; }
    public Guid SprayfieldId { get; set; }
    public DateTime ApplicationDate { get; set; }
    public decimal VolumeGallons { get; set; }
    public decimal NitrogenMgL { get; set; }
    public Guid? ExistingApplicationId { get; set; }
}

public class ComplianceProjectionResult
{
    public DateTime WindowStartDate { get; set; }
    public DateTime WindowEndDate { get; set; }

    public Guid SprayfieldId { get; set; }
    public string SprayfieldName { get; set; } = string.Empty;

    public decimal FieldAcres { get; set; }

    public decimal HistoricalPanLbs { get; set; }
    public decimal ProspectivePanLbs { get; set; }
    public decimal ProjectedPanLbs { get; set; }
    public decimal ProjectedPanLbsPerAcre { get; set; }
    public decimal? PanLimitLbsPerAcre { get; set; }
    public decimal? PanUtilizationPercent { get; set; }

    public decimal HistoricalGallons { get; set; }
    public decimal ProspectiveGallons { get; set; }
    public decimal ProjectedGallons { get; set; }
    public decimal ProjectedHydraulicInches { get; set; }
    public decimal? HydraulicLimitInchesPerYear { get; set; }
    public decimal? HydraulicUtilizationPercent { get; set; }

    public bool PanWarnAt85 { get; set; }
    public bool PanExceedsLimit { get; set; }
    public bool HydraulicExceedsLimit { get; set; }
    public bool RequiresConfirmation { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class FieldRollingMetricsResult
{
    public Guid FacilityId { get; set; }
    public Guid SprayfieldId { get; set; }
    public string SprayfieldName { get; set; } = string.Empty;
    public DateTime AsOfDate { get; set; }
    public DateTime WindowStartDate { get; set; }
    public DateTime WindowEndDate { get; set; }

    public decimal FieldAcres { get; set; }
    public decimal RollingPanLbs { get; set; }
    public decimal RollingPanLbsPerAcre { get; set; }
    public decimal? PanLimitLbsPerAcre { get; set; }
    public decimal? PanUtilizationPercent { get; set; }

    public decimal RollingGallons { get; set; }
    public decimal RollingHydraulicInches { get; set; }
    public decimal? HydraulicLimitInchesPerYear { get; set; }
    public decimal? HydraulicUtilizationPercent { get; set; }

    public List<string> Warnings { get; set; } = new();
}

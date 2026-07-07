using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using SAM.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.OperationalData;

public class WWCharViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
    public string? TemplateParametersStatusMessage { get; set; }
    public string? FacilityName { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; }

    public int DaysInMonth { get; set; }
    public int OrcCompleteDays { get; set; }
    public int FlowCompleteDays { get; set; }
    public int PhCompleteDays { get; set; }
    public int OrcCompletePercent { get; set; }
    public int FlowCompletePercent { get; set; }
    public int PhCompletePercent { get; set; }
    
    [Display(Name = "BOD5 Daily (mg/L)")]
    public List<decimal?> BOD5Daily { get; set; } = new();
    
    [Display(Name = "TSS Daily (mg/L)")]
    public List<decimal?> TSSDaily { get; set; } = new();
    
    [Display(Name = "Flow Rate Daily (GPM)")]
    public List<decimal?> FlowRateDaily { get; set; } = new();
    
    [Display(Name = "pH Daily")]
    public List<decimal?> PHDaily { get; set; } = new();
    
    [Display(Name = "NH3-N Daily (mg/L)")]
    public List<decimal?> NH3NDaily { get; set; } = new();
    
    [Display(Name = "Fecal Coliform Daily (CFU/100mL)")]
    public List<decimal?> FecalColiformDaily { get; set; } = new();
    
    [Display(Name = "Residual Chloride Daily (mg/L)")]
    public List<decimal?> ChlorideDaily { get; set; } = new();

    [Display(Name = "Ca Daily (mg/L)")]
    public List<decimal?> CaDaily { get; set; } = new();

    [Display(Name = "Mg Daily (mg/L)")]
    public List<decimal?> MgDaily { get; set; } = new();

    [Display(Name = "Na Daily (mg/L)")]
    public List<decimal?> NaDaily { get; set; } = new();

    [Display(Name = "SAR Daily (mg/L)")]
    public List<decimal?> SARDaily { get; set; } = new();

    [Display(Name = "TN Daily (mg/L)")]
    public List<decimal?> TNDaily { get; set; } = new();
    
    [Display(Name = "Composite Time Daily")]
    public List<string?> CompositeTime { get; set; } = new();
    
    [Display(Name = "ORC On Site Daily")]
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    
    [Display(Name = "Water Depth Daily (ft)")]
    public List<decimal?> LagoonWaterDepthFt { get; set; } = new();

    [Display(Name = "Storage Lagoon Freeboard Daily (ft)")]
    public List<decimal?> StorageLagoonFreeboardFt { get; set; } = new();

    [Display(Name = "ORC Arrival Time Daily")]
    public List<string?> ORCArrivalTime { get; set; } = new();

    [Display(Name = "ORC Time On Site Daily (hours)")]
    public List<decimal?> ORCTimeOnSiteHours { get; set; } = new();

    [Display(Name = "NO2 as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "NO2 as N must be 0 or greater.")]
    public decimal? NO2N { get; set; }

    [Display(Name = "TKN as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "TKN as N must be 0 or greater.")]
    public decimal? TKNN { get; set; }

    [Display(Name = "NO3 as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "NO3 as N must be 0 or greater.")]
    public decimal? NO3N { get; set; }

    [Display(Name = "Flow Measuring Point")]
    public FlowMeasuringPointEnum? FlowMeasuringPoint { get; set; }

    [Display(Name = "Parameter Monitoring Point")]
    public ParameterMonitoringPointEnum? ParameterMonitoringPoint { get; set; }

    [Display(Name = "Lab Options")]
    public Guid? LabOptionId { get; set; }

    public string? SelectedLabOptionName { get; set; }

    public string? SelectedLabCertificationNumber { get; set; }

    [Display(Name = "Lab Options 2")]
    public Guid? SecondaryLabOptionId { get; set; }

    public string? SelectedSecondaryLabOptionName { get; set; }

    public string? SelectedSecondaryLabCertificationNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Sampling Person 1")]
    public string SamplingPerson1 { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Sampling Person 2")]
    public string SamplingPerson2 { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;

    public List<WWCharTemplateParameterInputViewModel> TemplateParameters { get; set; } = new();
    public List<WWCharTestResultAttachmentViewModel> TestResultAttachments { get; set; } = new();
}

public class WWCharCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
    public string? TemplateParametersStatusMessage { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; } = (MonthEnum)DateTime.Now.Month;
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.Now.Year;
    
    [Display(Name = "BOD5 Daily (mg/L)")]
    public List<decimal?> BOD5Daily { get; set; } = new();
    
    [Display(Name = "TSS Daily (mg/L)")]
    public List<decimal?> TSSDaily { get; set; } = new();
    
    [Display(Name = "Flow Rate Daily (GPM)")]
    public List<decimal?> FlowRateDaily { get; set; } = new();
    
    [Display(Name = "pH Daily")]
    public List<decimal?> PHDaily { get; set; } = new();
    
    [Display(Name = "NH3-N Daily (mg/L)")]
    public List<decimal?> NH3NDaily { get; set; } = new();
    
    [Display(Name = "Fecal Coliform Daily (CFU/100mL)")]
    public List<decimal?> FecalColiformDaily { get; set; } = new();
    
    [Display(Name = "Residual Chloride Daily (mg/L)")]
    public List<decimal?> ChlorideDaily { get; set; } = new();

    [Display(Name = "Ca Daily (mg/L)")]
    public List<decimal?> CaDaily { get; set; } = new();

    [Display(Name = "Mg Daily (mg/L)")]
    public List<decimal?> MgDaily { get; set; } = new();

    [Display(Name = "Na Daily (mg/L)")]
    public List<decimal?> NaDaily { get; set; } = new();

    [Display(Name = "SAR Daily (mg/L)")]
    public List<decimal?> SARDaily { get; set; } = new();

    [Display(Name = "TN Daily (mg/L)")]
    public List<decimal?> TNDaily { get; set; } = new();
    
    [Display(Name = "Composite Time Daily")]
    public List<string?> CompositeTime { get; set; } = new();
    
    [Display(Name = "ORC On Site Daily")]
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    
    [Display(Name = "Water Depth Daily (ft)")]
    public List<decimal?> LagoonWaterDepthFt { get; set; } = new();

    [Display(Name = "Storage Lagoon Freeboard Daily (ft)")]
    public List<decimal?> StorageLagoonFreeboardFt { get; set; } = new();

    [Display(Name = "ORC Arrival Time Daily")]
    public List<string?> ORCArrivalTime { get; set; } = new();

    [Display(Name = "ORC Time On Site Daily (hours)")]
    public List<decimal?> ORCTimeOnSiteHours { get; set; } = new();

    [Display(Name = "NO2 as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "NO2 as N must be 0 or greater.")]
    public decimal? NO2N { get; set; }

    [Display(Name = "TKN as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "TKN as N must be 0 or greater.")]
    public decimal? TKNN { get; set; }

    [Display(Name = "NO3 as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "NO3 as N must be 0 or greater.")]
    public decimal? NO3N { get; set; }

    [Display(Name = "Flow Measuring Point")]
    public FlowMeasuringPointEnum? FlowMeasuringPoint { get; set; }

    [Display(Name = "Parameter Monitoring Point")]
    public ParameterMonitoringPointEnum? ParameterMonitoringPoint { get; set; }

    [Display(Name = "Lab Options")]
    public Guid? LabOptionId { get; set; }

    public string? SelectedLabOptionName { get; set; }

    public string? SelectedLabCertificationNumber { get; set; }

    [Display(Name = "Lab Options 2")]
    public Guid? SecondaryLabOptionId { get; set; }

    public string? SelectedSecondaryLabOptionName { get; set; }

    public string? SelectedSecondaryLabCertificationNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Sampling Person 1")]
    public string? SamplingPerson1 { get; set; }

    [StringLength(200)]
    [Display(Name = "Sampling Person 2")]
    public string? SamplingPerson2 { get; set; }
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;

    public List<WWCharTemplateParameterInputViewModel> TemplateParameters { get; set; } = new();
}

public class WWCharEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
    public string? TemplateParametersStatusMessage { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Display(Name = "BOD5 Daily (mg/L)")]
    public List<decimal?> BOD5Daily { get; set; } = new();
    
    [Display(Name = "TSS Daily (mg/L)")]
    public List<decimal?> TSSDaily { get; set; } = new();
    
    [Display(Name = "Flow Rate Daily (GPM)")]
    public List<decimal?> FlowRateDaily { get; set; } = new();
    
    [Display(Name = "pH Daily")]
    public List<decimal?> PHDaily { get; set; } = new();
    
    [Display(Name = "NH3-N Daily (mg/L)")]
    public List<decimal?> NH3NDaily { get; set; } = new();
    
    [Display(Name = "Fecal Coliform Daily (CFU/100mL)")]
    public List<decimal?> FecalColiformDaily { get; set; } = new();
    
    [Display(Name = "Residual Chloride Daily (mg/L)")]
    public List<decimal?> ChlorideDaily { get; set; } = new();

    [Display(Name = "Ca Daily (mg/L)")]
    public List<decimal?> CaDaily { get; set; } = new();

    [Display(Name = "Mg Daily (mg/L)")]
    public List<decimal?> MgDaily { get; set; } = new();

    [Display(Name = "Na Daily (mg/L)")]
    public List<decimal?> NaDaily { get; set; } = new();

    [Display(Name = "SAR Daily (mg/L)")]
    public List<decimal?> SARDaily { get; set; } = new();

    [Display(Name = "TN Daily (mg/L)")]
    public List<decimal?> TNDaily { get; set; } = new();
    
    [Display(Name = "Composite Time Daily")]
    public List<string?> CompositeTime { get; set; } = new();
    
    [Display(Name = "ORC On Site Daily")]
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    
    [Display(Name = "Water Depth Daily (ft)")]
    public List<decimal?> LagoonWaterDepthFt { get; set; } = new();

    [Display(Name = "Storage Lagoon Freeboard Daily (ft)")]
    public List<decimal?> StorageLagoonFreeboardFt { get; set; } = new();

    [Display(Name = "ORC Arrival Time Daily")]
    public List<string?> ORCArrivalTime { get; set; } = new();

    [Display(Name = "ORC Time On Site Daily (hours)")]
    public List<decimal?> ORCTimeOnSiteHours { get; set; } = new();

    [Display(Name = "NO2 as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "NO2 as N must be 0 or greater.")]
    public decimal? NO2N { get; set; }

    [Display(Name = "TKN as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "TKN as N must be 0 or greater.")]
    public decimal? TKNN { get; set; }

    [Display(Name = "NO3 as N (mg/L)")]
    [Range(0, double.MaxValue, ErrorMessage = "NO3 as N must be 0 or greater.")]
    public decimal? NO3N { get; set; }

    [Display(Name = "Flow Measuring Point")]
    public FlowMeasuringPointEnum? FlowMeasuringPoint { get; set; }

    [Display(Name = "Parameter Monitoring Point")]
    public ParameterMonitoringPointEnum? ParameterMonitoringPoint { get; set; }

    [Display(Name = "Lab Options")]
    public Guid? LabOptionId { get; set; }

    public string? SelectedLabOptionName { get; set; }

    public string? SelectedLabCertificationNumber { get; set; }

    [Display(Name = "Lab Options 2")]
    public Guid? SecondaryLabOptionId { get; set; }

    public string? SelectedSecondaryLabOptionName { get; set; }

    public string? SelectedSecondaryLabCertificationNumber { get; set; }

    [StringLength(200)]
    [Display(Name = "Sampling Person 1")]
    public string? SamplingPerson1 { get; set; }

    [StringLength(200)]
    [Display(Name = "Sampling Person 2")]
    public string? SamplingPerson2 { get; set; }
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;

    [Display(Name = "Test Date")]
    [DataType(DataType.Date)]
    public DateTime? TestResultDate { get; set; }

    [Display(Name = "Upload Test Result (PDF)")]
    public IFormFile? TestResultFile { get; set; }

    public List<WWCharTemplateParameterInputViewModel> TemplateParameters { get; set; } = new();
    public List<WWCharTestResultAttachmentViewModel> TestResultAttachments { get; set; } = new();
}

public class WWCharTemplateParameterInputViewModel
{
    public Guid FacilityPermitTemplateParameterId { get; set; }
    public string PcsCode { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string MeasurementFrequency { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public string? ScheduledMonthsCsv { get; set; }
    public string? Notes { get; set; }
    public decimal? MonthlyAverageLimit { get; set; }
    public decimal? MonthlyGeometricMeanLimit { get; set; }
    public decimal? DailyMinimumLimit { get; set; }
    public decimal? DailyMaximumLimit { get; set; }
    public List<decimal?> DailyValues { get; set; } = new();
    public List<bool> DailyIsReportingDetectionLimit { get; set; } = new();
}

public class WWCharTemplateSectionViewModel
{
    public Guid FacilityId { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
    public MonthEnum Month { get; set; }
    public int Year { get; set; }
    public Guid? RecordId { get; set; }
    public bool IsEdit { get; set; }
    public string? TemplateParametersStatusMessage { get; set; }
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    public List<decimal?> LagoonWaterDepthFt { get; set; } = new();
    public List<decimal?> StorageLagoonFreeboardFt { get; set; } = new();
    public decimal? LagoonBermHeightFeet { get; set; }
    public decimal? PermittedMinimumFreeboardFeet { get; set; }
    public List<string?> ORCArrivalTime { get; set; } = new();
    public List<decimal?> ORCTimeOnSiteHours { get; set; } = new();
    public List<WWCharTemplateParameterInputViewModel> TemplateParameters { get; set; } = new();
}

public class WWCharTestResultAttachmentViewModel
{
    public Guid Id { get; set; }
    public DateTime TestDate { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
}

public class WWCharFilterViewModel
{
    public Guid? FacilityId { get; set; }
    public int? Month { get; set; }
    public int? Year { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class WWCharSortViewModel
{
    public string SortBy { get; set; } = "period";
    public string SortDir { get; set; } = "desc";
}

public class WWCharsIndexViewModel
{
    public bool IsGlobalAdmin { get; set; }
    public Guid? SelectedCompanyId { get; set; }
    public SelectList? Facilities { get; set; }
    public WWCharFilterViewModel Filter { get; set; } = new();
    public WWCharSortViewModel Sort { get; set; } = new();
    public PagedResult<WWCharViewModel> WWChars { get; set; } = new();
}



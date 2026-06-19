using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.OperationalData;

public class GWMonitViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid MonitoringWellId { get; set; }
    public string? MonitoringWellName { get; set; }
    
    [Required]
    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; }
    
    [Display(Name = "Sample Depth (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? SampleDepth { get; set; }
    
    [Display(Name = "Depth to Water Level (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? WaterLevel { get; set; }

    [Display(Name = "Well Depth (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Well Diameter (in)")]
    [Range(0, double.MaxValue)]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Measuring Point (ft above land surface)")]
    [Range(0, double.MaxValue)]
    public decimal? MeasuringPointAboveLandSurface { get; set; }

    [Display(Name = "Relative M.P. Elevation (ft)")]
    public decimal? RelativeMpElevation { get; set; }

    [Display(Name = "Screened Interval From (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? ScreenedIntervalFromFeet { get; set; }

    [Display(Name = "Screened Interval To (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? ScreenedIntervalToFeet { get; set; }
    
    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? Temperature { get; set; }
    
    [Display(Name = "pH")]
    [Range(0, 14)]
    public decimal? PH { get; set; }
    
    [Display(Name = "Specific Conductance (uMhos)")]
    [Range(0, double.MaxValue)]
    public decimal? Conductivity { get; set; }

    [Display(Name = "Gallons Pumped")]
    [Range(0, double.MaxValue)]
    public decimal? GallonsPumped { get; set; }

    [StringLength(100)]
    [Display(Name = "Odor")]
    public string? Odor { get; set; }

    [StringLength(100)]
    [Display(Name = "Appearance")]
    public string? Appearance { get; set; }

    [Display(Name = "TSS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TSS { get; set; }
    
    [Display(Name = "NH3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NH3N { get; set; }
    
    [Display(Name = "NO3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NO3N { get; set; }
    
    [Display(Name = "TKN (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TKN { get; set; }
    
    [Display(Name = "Chloride (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Chloride { get; set; }

    [Display(Name = "TOC (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TOC { get; set; }

    [Display(Name = "Calcium (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Calcium { get; set; }

    [Display(Name = "Magnesium (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Magnesium { get; set; }

    [Display(Name = "Metals Samples Collected Unfiltered")]
    public bool? MetalsSamplesCollectedUnfiltered { get; set; }

    [Display(Name = "Metal Samples Field Acidified")]
    public bool? MetalSamplesFieldAcidified { get; set; }
    
    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? FecalColiform { get; set; }
    
    [Display(Name = "Total Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalColiform { get; set; }

    [Display(Name = "VOC Report Attached")]
    public bool VOCReportAttached { get; set; }

    public string? VOCReportFileName { get; set; }

    [StringLength(200)]
    [Display(Name = "VOC Method #")]
    public string? VOCMethodNumber { get; set; }

    [Display(Name = "Lab Options")]
    public Guid? LabOptionId { get; set; }

    [Display(Name = "Laboratory")]
    public string? ResolvedLabName { get; set; }

    [Display(Name = "Lab Certification No.")]
    public string? ResolvedLabCertificationNumber { get; set; }
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string? CollectedBy { get; set; }

    [Display(Name = "Lab Date Sample Analyzed")]
    [DataType(DataType.Date)]
    public DateTime? LabSampleAnalyzedDate { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [Display(Name = "Q1 Response")]
    public bool? GW59AQuestion1Response { get; set; }
    [Display(Name = "Q2 Response")]
    public bool? GW59AQuestion2Response { get; set; }
    [Display(Name = "Q3 Response")]
    public bool? GW59AQuestion3Response { get; set; }
    [Display(Name = "Q4 Response")]
    public bool? GW59AQuestion4Response { get; set; }
    [Display(Name = "Q5 Response")]
    public bool? GW59AQuestion5Response { get; set; }
    [Display(Name = "Q6 Response")]
    public bool? GW59AQuestion6Response { get; set; }
    [Display(Name = "Q7 Response")]
    public bool? GW59AQuestion7Response { get; set; }
    [Display(Name = "GW-59A Due Date")]
    [DataType(DataType.Date)]
    public DateTime? GW59ADueDate { get; set; }
    [StringLength(4000)]
    [Display(Name = "Q2 Details")]
    public string? GW59AQuestion2Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "Q4 Details")]
    public string? GW59AQuestion4Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "Q5 Details")]
    public string? GW59AQuestion5Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "Q7 Details")]
    public string? GW59AQuestion7Details { get; set; }
    [StringLength(200)]
    [Display(Name = "GW-59A Signer Name")]
    public string? GW59ASignerName { get; set; }
    [StringLength(200)]
    [Display(Name = "GW-59A Signer Title")]
    public string? GW59ASignerTitle { get; set; }
    [Display(Name = "GW-59A Signed Date")]
    [DataType(DataType.Date)]
    public DateTime? GW59ASignedDate { get; set; }

    public List<GWMonitTemplateParameterViewModel> TemplateParameters { get; set; } = new();
}

public class GWMonitCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Monitoring Well")]
    public Guid MonitoringWellId { get; set; }
    
    [Required]
    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; } = DateTime.Today;
    
    [Display(Name = "Sample Depth (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? SampleDepth { get; set; }
    
    [Display(Name = "Depth to Water Level (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? WaterLevel { get; set; }

    [Display(Name = "Well Depth (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Well Diameter (in)")]
    [Range(0, double.MaxValue)]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Measuring Point (ft above land surface)")]
    [Range(0, double.MaxValue)]
    public decimal? MeasuringPointAboveLandSurface { get; set; }

    [Display(Name = "Relative M.P. Elevation (ft)")]
    public decimal? RelativeMpElevation { get; set; }

    [Display(Name = "Screened Interval From (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? ScreenedIntervalFromFeet { get; set; }

    [Display(Name = "Screened Interval To (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? ScreenedIntervalToFeet { get; set; }
    
    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? Temperature { get; set; }
    
    [Display(Name = "pH")]
    [Range(0, 14)]
    public decimal? PH { get; set; }
    
    [Display(Name = "Specific Conductance (uMhos)")]
    [Range(0, double.MaxValue)]
    public decimal? Conductivity { get; set; }

    [Display(Name = "Gallons Pumped")]
    [Range(0, double.MaxValue)]
    public decimal? GallonsPumped { get; set; }

    [StringLength(100)]
    [Display(Name = "Odor")]
    public string? Odor { get; set; }

    [StringLength(100)]
    [Display(Name = "Appearance")]
    public string? Appearance { get; set; }
    [Display(Name = "TSS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TSS { get; set; }
    
    [Display(Name = "NH3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NH3N { get; set; }
    
    [Display(Name = "NO3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NO3N { get; set; }
    
    [Display(Name = "TKN (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TKN { get; set; }
    
    [Display(Name = "Chloride (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Chloride { get; set; }

    [Display(Name = "TOC (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TOC { get; set; }

    [Display(Name = "Calcium (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Calcium { get; set; }

    [Display(Name = "Magnesium (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Magnesium { get; set; }

    [Display(Name = "Metals Samples Collected Unfiltered")]
    public bool? MetalsSamplesCollectedUnfiltered { get; set; }

    [Display(Name = "Metal Samples Field Acidified")]
    public bool? MetalSamplesFieldAcidified { get; set; }
    
    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? FecalColiform { get; set; }
    
    [Display(Name = "Total Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalColiform { get; set; }

    [Display(Name = "VOC Report Attached")]
    public bool VOCReportAttached { get; set; }

    [Display(Name = "VOC Report (PDF)")]
    public IFormFile? VOCReportFile { get; set; }

    public string? VOCReportFileName { get; set; }

    [StringLength(200)]
    [Display(Name = "VOC Method #")]
    public string? VOCMethodNumber { get; set; }

    [Display(Name = "Lab Options")]
    public Guid? LabOptionId { get; set; }

    [Display(Name = "Laboratory")]
    public string? ResolvedLabName { get; set; }

    [Display(Name = "Lab Certification No.")]
    public string? ResolvedLabCertificationNumber { get; set; }
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string? CollectedBy { get; set; }

    [Display(Name = "Lab Date Sample Analyzed")]
    [DataType(DataType.Date)]
    public DateTime? LabSampleAnalyzedDate { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [Display(Name = "GW-59A Q1")]
    public bool? GW59AQuestion1Response { get; set; }
    [Display(Name = "GW-59A Q2")]
    public bool? GW59AQuestion2Response { get; set; }
    [Display(Name = "GW-59A Q3")]
    public bool? GW59AQuestion3Response { get; set; }
    [Display(Name = "GW-59A Q4")]
    public bool? GW59AQuestion4Response { get; set; }
    [Display(Name = "GW-59A Q5")]
    public bool? GW59AQuestion5Response { get; set; }
    [Display(Name = "GW-59A Q6")]
    public bool? GW59AQuestion6Response { get; set; }
    [Display(Name = "GW-59A Q7")]
    public bool? GW59AQuestion7Response { get; set; }
    [Display(Name = "GW-59A Due Date")]
    [DataType(DataType.Date)]
    public DateTime? GW59ADueDate { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q2 Details")]
    public string? GW59AQuestion2Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q4 Details")]
    public string? GW59AQuestion4Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q5 Details")]
    public string? GW59AQuestion5Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q7 Details")]
    public string? GW59AQuestion7Details { get; set; }
    [StringLength(200)]
    [Display(Name = "GW-59A Signer Name")]
    public string? GW59ASignerName { get; set; }
    [StringLength(200)]
    [Display(Name = "GW-59A Signer Title")]
    public string? GW59ASignerTitle { get; set; }
    [Display(Name = "GW-59A Signed Date")]
    [DataType(DataType.Date)]
    public DateTime? GW59ASignedDate { get; set; }

    public List<GWMonitTemplateParameterViewModel> TemplateParameters { get; set; } = new();
    public string? TemplateParametersStatusMessage { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
}

public class GWMonitEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Monitoring Well")]
    public Guid MonitoringWellId { get; set; }
    
    [Required]
    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; }
    
    [Display(Name = "Sample Depth (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? SampleDepth { get; set; }
    
    [Display(Name = "Depth to Water Level (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? WaterLevel { get; set; }

    [Display(Name = "Well Depth (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? WellDepthFeet { get; set; }

    [Display(Name = "Well Diameter (in)")]
    [Range(0, double.MaxValue)]
    public decimal? DiameterInches { get; set; }

    [Display(Name = "Measuring Point (ft above land surface)")]
    [Range(0, double.MaxValue)]
    public decimal? MeasuringPointAboveLandSurface { get; set; }

    [Display(Name = "Relative M.P. Elevation (ft)")]
    public decimal? RelativeMpElevation { get; set; }

    [Display(Name = "Screened Interval From (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? ScreenedIntervalFromFeet { get; set; }

    [Display(Name = "Screened Interval To (ft)")]
    [Range(0, double.MaxValue)]
    public decimal? ScreenedIntervalToFeet { get; set; }
    
    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? Temperature { get; set; }
    
    [Display(Name = "pH")]
    [Range(0, 14)]
    public decimal? PH { get; set; }
    
    [Display(Name = "Specific Conductance (uMhos)")]
    [Range(0, double.MaxValue)]
    public decimal? Conductivity { get; set; }

    [Display(Name = "Gallons Pumped")]
    [Range(0, double.MaxValue)]
    public decimal? GallonsPumped { get; set; }

    [StringLength(100)]
    [Display(Name = "Odor")]
    public string? Odor { get; set; }

    [StringLength(100)]
    [Display(Name = "Appearance")]
    public string? Appearance { get; set; }
    [Display(Name = "TSS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TSS { get; set; }
    
    [Display(Name = "NH3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NH3N { get; set; }
    
    [Display(Name = "NO3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NO3N { get; set; }
    
    [Display(Name = "TKN (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TKN { get; set; }
    
    [Display(Name = "Chloride (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Chloride { get; set; }

    [Display(Name = "TOC (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TOC { get; set; }

    [Display(Name = "Calcium (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Calcium { get; set; }

    [Display(Name = "Magnesium (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Magnesium { get; set; }

    [Display(Name = "Metals Samples Collected Unfiltered")]
    public bool? MetalsSamplesCollectedUnfiltered { get; set; }

    [Display(Name = "Metal Samples Field Acidified")]
    public bool? MetalSamplesFieldAcidified { get; set; }
    
    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? FecalColiform { get; set; }
    
    [Display(Name = "Total Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalColiform { get; set; }

    [Display(Name = "VOC Report Attached")]
    public bool VOCReportAttached { get; set; }

    [Display(Name = "VOC Report (PDF)")]
    public IFormFile? VOCReportFile { get; set; }

    public string? VOCReportFileName { get; set; }

    [Display(Name = "Remove Current VOC File")]
    public bool RemoveVocReportFile { get; set; }

    [StringLength(200)]
    [Display(Name = "VOC Method #")]
    public string? VOCMethodNumber { get; set; }

    [Display(Name = "Lab Options")]
    public Guid? LabOptionId { get; set; }

    [Display(Name = "Laboratory")]
    public string? ResolvedLabName { get; set; }

    [Display(Name = "Lab Certification No.")]
    public string? ResolvedLabCertificationNumber { get; set; }
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string? CollectedBy { get; set; }

    [Display(Name = "Lab Date Sample Analyzed")]
    [DataType(DataType.Date)]
    public DateTime? LabSampleAnalyzedDate { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string? Comments { get; set; }

    [Display(Name = "GW-59A Q1")]
    public bool? GW59AQuestion1Response { get; set; }
    [Display(Name = "GW-59A Q2")]
    public bool? GW59AQuestion2Response { get; set; }
    [Display(Name = "GW-59A Q3")]
    public bool? GW59AQuestion3Response { get; set; }
    [Display(Name = "GW-59A Q4")]
    public bool? GW59AQuestion4Response { get; set; }
    [Display(Name = "GW-59A Q5")]
    public bool? GW59AQuestion5Response { get; set; }
    [Display(Name = "GW-59A Q6")]
    public bool? GW59AQuestion6Response { get; set; }
    [Display(Name = "GW-59A Q7")]
    public bool? GW59AQuestion7Response { get; set; }
    [Display(Name = "GW-59A Due Date")]
    [DataType(DataType.Date)]
    public DateTime? GW59ADueDate { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q2 Details")]
    public string? GW59AQuestion2Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q4 Details")]
    public string? GW59AQuestion4Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q5 Details")]
    public string? GW59AQuestion5Details { get; set; }
    [StringLength(4000)]
    [Display(Name = "GW-59A Q7 Details")]
    public string? GW59AQuestion7Details { get; set; }
    [StringLength(200)]
    [Display(Name = "GW-59A Signer Name")]
    public string? GW59ASignerName { get; set; }
    [StringLength(200)]
    [Display(Name = "GW-59A Signer Title")]
    public string? GW59ASignerTitle { get; set; }
    [Display(Name = "GW-59A Signed Date")]
    [DataType(DataType.Date)]
    public DateTime? GW59ASignedDate { get; set; }

    public List<GWMonitTemplateParameterViewModel> TemplateParameters { get; set; } = new();
    public string? TemplateParametersStatusMessage { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
}

public class GWMonitTemplateParameterViewModel
{
    public Guid FacilityPermitTemplateParameterId { get; set; }
    public string PcsCode { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public string MeasurementFrequency { get; set; } = string.Empty;
    public string SampleType { get; set; } = string.Empty;
    public string? ScheduledMonthsCsv { get; set; }
    public decimal? DailyMaximumLimit { get; set; }
    public string? Notes { get; set; }
    public decimal? EnteredValue { get; set; }
    public bool IsRequiredForSelectedMonth { get; set; }
    public string? RequirementMessage { get; set; }
}

public class GWMonitTemplateSectionViewModel
{
    public Guid FacilityId { get; set; }
    public DateTime SampleDate { get; set; }
    public Guid? RecordId { get; set; }
    public bool IsEdit { get; set; }
    public Guid? FacilityPermitId { get; set; }
    public string? FacilityPermitDisplay { get; set; }
    public string? TemplateParametersStatusMessage { get; set; }
    public List<GWMonitTemplateParameterViewModel> TemplateParameters { get; set; } = new();
}

public class GWMonitFilterViewModel
{
    public Guid? FacilityId { get; set; }
    public Guid? MonitoringWellId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class GWMonitSortViewModel
{
    public string SortBy { get; set; } = "sampleDate";
    public string SortDir { get; set; } = "desc";
}

public class GWMonitsIndexViewModel
{
    public bool IsGlobalAdmin { get; set; }
    public Guid? SelectedCompanyId { get; set; }
    public SelectList? Facilities { get; set; }
    public SelectList? MonitoringWells { get; set; }
    public GWMonitFilterViewModel Filter { get; set; } = new();
    public GWMonitSortViewModel Sort { get; set; } = new();
    public PagedResult<GWMonitViewModel> GWMonits { get; set; } = new();
}

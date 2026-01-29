using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.Reports;

public class IrrRprtViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    public int Year { get; set; }
    
    [Display(Name = "Total Volume Applied (gallons)")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    public decimal TotalVolumeApplied { get; set; }
    
    [Display(Name = "Total Application Rate (inches)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal TotalApplicationRate { get; set; }
    
    [Display(Name = "Hydraulic Loading Rate (inches/year)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal HydraulicLoadingRate { get; set; }
    
    [Display(Name = "Nitrogen Loading Rate (lbs/acre/year)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal NitrogenLoadingRate { get; set; }
    
    [Display(Name = "PAN Uptake Rate (lbs/acre/year)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal PanUptakeRate { get; set; }
    
    [Display(Name = "Application Efficiency (%)")]
    [DisplayFormat(DataFormatString = "{0:F1}")]
    public decimal ApplicationEfficiency { get; set; }
    
    [Display(Name = "Weather Summary")]
    public string WeatherSummary { get; set; } = string.Empty;
    
    [Display(Name = "Operational Notes")]
    public string OperationalNotes { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Compliance Status")]
    public ComplianceStatusEnum ComplianceStatus { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
    
    [Display(Name = "Updated Date")]
    public DateTime? UpdatedDate { get; set; }
}

public class IrrRprtCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; } = (MonthEnum)DateTime.Now.Month;
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.Now.Year;
}

public class IrrRprtEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Display(Name = "Total Volume Applied (gallons)")]
    [DisplayFormat(DataFormatString = "{0:N2}")]
    public decimal TotalVolumeApplied { get; set; }
    
    [Display(Name = "Total Application Rate (inches)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal TotalApplicationRate { get; set; }
    
    [Display(Name = "Hydraulic Loading Rate (inches/year)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal HydraulicLoadingRate { get; set; }
    
    [Display(Name = "Nitrogen Loading Rate (lbs/acre/year)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal NitrogenLoadingRate { get; set; }
    
    [Display(Name = "PAN Uptake Rate (lbs/acre/year)")]
    [DisplayFormat(DataFormatString = "{0:F2}")]
    public decimal PanUptakeRate { get; set; }
    
    [Display(Name = "Application Efficiency (%)")]
    [DisplayFormat(DataFormatString = "{0:F1}")]
    public decimal ApplicationEfficiency { get; set; }
    
    [StringLength(1000)]
    [Display(Name = "Weather Summary")]
    public string WeatherSummary { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Operational Notes")]
    public string OperationalNotes { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Compliance Status")]
    public ComplianceStatusEnum ComplianceStatus { get; set; }
}



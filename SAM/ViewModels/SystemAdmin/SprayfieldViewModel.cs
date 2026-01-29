using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class SprayfieldViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Field ID")]
    public string FieldId { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Size (acres)")]
    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal SizeAcres { get; set; }
    
    [Required]
    [Display(Name = "Soil")]
    public Guid SoilId { get; set; }
    public string? SoilName { get; set; }
    
    [Required]
    [Display(Name = "Crop")]
    public Guid CropId { get; set; }
    public string? CropName { get; set; }
    
    [Required]
    [Display(Name = "Nozzle")]
    public Guid NozzleId { get; set; }
    public string? NozzleName { get; set; }
    
    [Display(Name = "Facility")]
    public Guid? FacilityId { get; set; }
    public string? FacilityName { get; set; }
    
    [Required]
    [Display(Name = "Hydraulic Loading Limit (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hydraulic loading limit must be a positive number.")]
    public decimal HydraulicLoadingLimitInPerYr { get; set; }
    
    [Display(Name = "Hourly Rate (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal? HourlyRateInches { get; set; }
    
    [Display(Name = "Annual Rate (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Annual rate must be a positive number.")]
    public decimal? AnnualRateInches { get; set; }
}

public class SprayfieldCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Field ID")]
    public string FieldId { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Size (acres)")]
    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal SizeAcres { get; set; }
    
    [Required]
    [Display(Name = "Soil")]
    public Guid SoilId { get; set; }
    
    [Required]
    [Display(Name = "Crop")]
    public Guid CropId { get; set; }
    
    [Required]
    [Display(Name = "Nozzle")]
    public Guid NozzleId { get; set; }
    
    [Display(Name = "Facility")]
    public Guid? FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Hydraulic Loading Limit (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hydraulic loading limit must be a positive number.")]
    public decimal HydraulicLoadingLimitInPerYr { get; set; }
    
    [Display(Name = "Hourly Rate (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal? HourlyRateInches { get; set; }
    
    [Display(Name = "Annual Rate (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Annual rate must be a positive number.")]
    public decimal? AnnualRateInches { get; set; }
}

public class SprayfieldEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Field ID")]
    public string FieldId { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Size (acres)")]
    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal SizeAcres { get; set; }
    
    [Required]
    [Display(Name = "Soil")]
    public Guid SoilId { get; set; }
    
    [Required]
    [Display(Name = "Crop")]
    public Guid CropId { get; set; }
    
    [Required]
    [Display(Name = "Nozzle")]
    public Guid NozzleId { get; set; }
    
    [Display(Name = "Facility")]
    public Guid? FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Hydraulic Loading Limit (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hydraulic loading limit must be a positive number.")]
    public decimal HydraulicLoadingLimitInPerYr { get; set; }
    
    [Display(Name = "Hourly Rate (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal? HourlyRateInches { get; set; }
    
    [Display(Name = "Annual Rate (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Annual rate must be a positive number.")]
    public decimal? AnnualRateInches { get; set; }
}



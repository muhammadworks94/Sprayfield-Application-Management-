using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.OperationalData;

public class IrrigateViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid SprayfieldId { get; set; }
    public string? SprayfieldName { get; set; }
    
    [Required]
    [Display(Name = "Irrigation Date")]
    [DataType(DataType.Date)]
    public DateTime IrrigationDate { get; set; }
    
    [Required]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public string StartTime { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "End Time")]
    [DataType(DataType.Time)]
    public string EndTime { get; set; } = string.Empty;
    
    [Display(Name = "Duration (hours)")]
    [Range(0, double.MaxValue)]
    public decimal? DurationHours { get; set; }
    
    [Display(Name = "Flow Rate (GPM)")]
    [Range(0, double.MaxValue)]
    public decimal? FlowRateGpm { get; set; }
    
    [Display(Name = "Total Volume (gallons)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalVolumeGallons { get; set; }
    
    [Display(Name = "Application Rate (inches)")]
    [Range(0, double.MaxValue)]
    public decimal? ApplicationRateInches { get; set; }

    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? TemperatureF { get; set; }

    [Display(Name = "Precipitation (in)")]
    [Range(0, double.MaxValue)]
    public decimal? PrecipitationIn { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string WeatherConditions { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;

    [Display(Name = "Modified by")]
    public string? ModifiedBy { get; set; }
}

public class IrrigateCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Sprayfield")]
    public Guid SprayfieldId { get; set; }
    
    [Required]
    [Display(Name = "Irrigation Date")]
    [DataType(DataType.Date)]
    public DateTime IrrigationDate { get; set; } = DateTime.Today;
    
    [Required]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public string StartTime { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "End Time")]
    [DataType(DataType.Time)]
    public string EndTime { get; set; } = string.Empty;
    
    [Display(Name = "Duration (hours)")]
    [Range(0, double.MaxValue)]
    public decimal? DurationHours { get; set; }
    
    [Display(Name = "Flow Rate (GPM)")]
    [Range(0, double.MaxValue)]
    public decimal? FlowRateGpm { get; set; }
    
    [Display(Name = "Total Volume (gallons)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalVolumeGallons { get; set; }
    
    [Display(Name = "Application Rate (inches)")]
    [Range(0, double.MaxValue)]
    public decimal? ApplicationRateInches { get; set; }

    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? TemperatureF { get; set; }

    [Display(Name = "Precipitation (in)")]
    [Range(0, double.MaxValue)]
    public decimal? PrecipitationIn { get; set; }

    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string WeatherConditions { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;
}

public class IrrigateEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Sprayfield")]
    public Guid SprayfieldId { get; set; }
    
    [Required]
    [Display(Name = "Irrigation Date")]
    [DataType(DataType.Date)]
    public DateTime IrrigationDate { get; set; }
    
    [Required]
    [Display(Name = "Start Time")]
    [DataType(DataType.Time)]
    public string StartTime { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "End Time")]
    [DataType(DataType.Time)]
    public string EndTime { get; set; } = string.Empty;
    
    [Display(Name = "Duration (hours)")]
    [Range(0, double.MaxValue)]
    public decimal? DurationHours { get; set; }
    
    [Display(Name = "Flow Rate (GPM)")]
    [Range(0, double.MaxValue)]
    public decimal? FlowRateGpm { get; set; }
    
    [Display(Name = "Total Volume (gallons)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalVolumeGallons { get; set; }
    
    [Display(Name = "Application Rate (inches)")]
    [Range(0, double.MaxValue)]
    public decimal? ApplicationRateInches { get; set; }

    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? TemperatureF { get; set; }

    [Display(Name = "Precipitation (in)")]
    [Range(0, double.MaxValue)]
    public decimal? PrecipitationIn { get; set; }

    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string WeatherConditions { get; set; } = string.Empty;

    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;
}



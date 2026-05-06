using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.OperationalData;

public class OperatorLogViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    
    [Required]
    [Display(Name = "Log Date")]
    [DataType(DataType.Date)]
    public DateTime LogDate { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Operator Name")]
    public string OperatorName { get; set; } = string.Empty;
    
    [Display(Name = "Weather Conditions")]
    [StringLength(500)]
    public string? WeatherConditions { get; set; }

    [Display(Name = "Temperature (°F)")]
    public decimal? TemperatureF { get; set; }

    [Display(Name = "Precipitation (in)")]
    public decimal? PrecipitationIn { get; set; }

    [Display(Name = "ORC On Site")]
    public ORCOnSiteEnum? ORCOnSite { get; set; }

    [Display(Name = "Storage Lagoon Freeboard (ft)")]
    public decimal? StorageFt { get; set; }

    [Display(Name = "5-Day Upset (ft)")]
    public decimal? FiveDayUpsetFt { get; set; }

    [Display(Name = "Arrival Time")]
    [DataType(DataType.Time)]
    public string ArrivalTime { get; set; } = string.Empty;

    [Display(Name = "Time on Site (hours)")]
    [Range(0, double.MaxValue)]
    public decimal? TimeOnSiteHours { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Maintenance Performed")]
    public string? MaintenancePerformed { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Equipment Inspected")]
    public string? EquipmentInspected { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Issues Noted")]
    public string? IssuesNoted { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Corrective Actions")]
    public string? CorrectiveActions { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Next Shift Notes")]
    public string? NextShiftNotes { get; set; }
}

public class OperatorLogCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Log Date")]
    [DataType(DataType.Date)]
    public DateTime LogDate { get; set; } = DateTime.Today;
    
    [StringLength(200)]
    [Display(Name = "Operator Name")]
    public string OperatorName { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string? WeatherConditions { get; set; }

    [Display(Name = "Temperature (°F)")]
    public decimal? TemperatureF { get; set; }

    [Display(Name = "Precipitation (in)")]
    public decimal? PrecipitationIn { get; set; }

    [Display(Name = "ORC On Site")]
    public ORCOnSiteEnum? ORCOnSite { get; set; }

    [Display(Name = "Storage Lagoon Freeboard (ft)")]
    public decimal? StorageFt { get; set; }

    [Display(Name = "5-Day Upset (ft)")]
    public decimal? FiveDayUpsetFt { get; set; }

    [Required]
    [Display(Name = "Arrival Time")]
    [DataType(DataType.Time)]
    public string ArrivalTime { get; set; } = string.Empty;

    [Display(Name = "Time on Site (hours)")]
    [Range(0, double.MaxValue)]
    public decimal? TimeOnSiteHours { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Maintenance Performed")]
    public string? MaintenancePerformed { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Equipment Inspected")]
    public string? EquipmentInspected { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Issues Noted")]
    public string? IssuesNoted { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Corrective Actions")]
    public string? CorrectiveActions { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Next Shift Notes")]
    public string? NextShiftNotes { get; set; }
}

public class OperatorLogEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Log Date")]
    [DataType(DataType.Date)]
    public DateTime LogDate { get; set; }
    
    [StringLength(200)]
    [Display(Name = "Operator Name")]
    public string OperatorName { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string? WeatherConditions { get; set; }

    [Display(Name = "Temperature (°F)")]
    public decimal? TemperatureF { get; set; }

    [Display(Name = "Precipitation (in)")]
    public decimal? PrecipitationIn { get; set; }

    [Display(Name = "ORC On Site")]
    public ORCOnSiteEnum? ORCOnSite { get; set; }

    [Display(Name = "Storage Lagoon Freeboard (ft)")]
    public decimal? StorageFt { get; set; }

    [Display(Name = "5-Day Upset (ft)")]
    public decimal? FiveDayUpsetFt { get; set; }

    [Required]
    [Display(Name = "Arrival Time")]
    [DataType(DataType.Time)]
    public string ArrivalTime { get; set; } = string.Empty;

    [Display(Name = "Time on Site (hours)")]
    [Range(0, double.MaxValue)]
    public decimal? TimeOnSiteHours { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Maintenance Performed")]
    public string? MaintenancePerformed { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Equipment Inspected")]
    public string? EquipmentInspected { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Issues Noted")]
    public string? IssuesNoted { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Corrective Actions")]
    public string? CorrectiveActions { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Next Shift Notes")]
    public string? NextShiftNotes { get; set; }
}



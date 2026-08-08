using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using SAM.Services.Models;

namespace SAM.ViewModels.OperationalData;

public interface IOperatorLogLagoonFields
{
    decimal? WaterDepthFt { get; set; }
    decimal? StorageFt { get; set; }
    decimal? LagoonBermHeightFeet { get; set; }
    decimal? PermittedMinimumFreeboardFeet { get; set; }
    bool IsLegacyFreeboardEntry { get; set; }
}

public class OperatorLogViewModel : IOperatorLogLagoonFields
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

    [Display(Name = "Water Depth (ft)")]
    public decimal? WaterDepthFt { get; set; }

    [Display(Name = "Storage Lagoon Freeboard (ft)")]
    public decimal? StorageFt { get; set; }

    [Display(Name = "Lagoon Berm Height (ft)")]
    public decimal? LagoonBermHeightFeet { get; set; }

    [Display(Name = "Required Minimum Freeboard (ft)")]
    public decimal? PermittedMinimumFreeboardFeet { get; set; }

    public bool IsLegacyFreeboardEntry { get; set; }

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

public class OperatorLogCreateViewModel : IOperatorLogLagoonFields
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

    [Required]
    [Display(Name = "ORC On Site")]
    public ORCOnSiteEnum? ORCOnSite { get; set; }

    [Display(Name = "Water Depth (ft)")]
    public decimal? WaterDepthFt { get; set; }

    [Display(Name = "Storage Lagoon Freeboard (ft)")]
    public decimal? StorageFt { get; set; }

    [Display(Name = "Lagoon Berm Height (ft)")]
    public decimal? LagoonBermHeightFeet { get; set; }

    [Display(Name = "Required Minimum Freeboard (ft)")]
    public decimal? PermittedMinimumFreeboardFeet { get; set; }

    public bool IsLegacyFreeboardEntry { get; set; }

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

public class OperatorLogEditViewModel : IOperatorLogLagoonFields
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

    [Required]
    [Display(Name = "ORC On Site")]
    public ORCOnSiteEnum? ORCOnSite { get; set; }

    [Display(Name = "Water Depth (ft)")]
    public decimal? WaterDepthFt { get; set; }

    [Display(Name = "Storage Lagoon Freeboard (ft)")]
    public decimal? StorageFt { get; set; }

    [Display(Name = "Lagoon Berm Height (ft)")]
    public decimal? LagoonBermHeightFeet { get; set; }

    [Display(Name = "Required Minimum Freeboard (ft)")]
    public decimal? PermittedMinimumFreeboardFeet { get; set; }

    public bool IsLegacyFreeboardEntry { get; set; }

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

public class OperatorLogFilterViewModel
{
    public Guid? FacilityId { get; set; }
    public string? OperatorName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class OperatorLogSortViewModel
{
    public string SortBy { get; set; } = "logDate";
    public string SortDir { get; set; } = "desc";
}

public class OperatorLogsIndexViewModel
{
    public bool IsGlobalAdmin { get; set; }
    public Guid? SelectedCompanyId { get; set; }
    public Guid? SelectedFacilityId { get; set; }
    public SelectList? Facilities { get; set; }
    public SelectList? Operators { get; set; }
    public OperatorLogFilterViewModel Filter { get; set; } = new();
    public OperatorLogSortViewModel Sort { get; set; } = new();
    public PagedResult<OperatorLogViewModel> Logs { get; set; } = new();
}



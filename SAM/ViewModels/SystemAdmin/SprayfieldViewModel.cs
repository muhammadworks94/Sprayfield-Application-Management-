using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class ApplicationZoneInputViewModel
{
    public Guid? Id { get; set; }

    [Required]
    [StringLength(100)]
    [Display(Name = "Zone Name")]
    public string ZoneName { get; set; } = string.Empty;

    [Display(Name = "Percent Of Field")]
    [Range(0.01, 100, ErrorMessage = "Percent must be between 0.01 and 100.")]
    public decimal PercentOfField { get; set; }

    [Required]
    [Display(Name = "Soil")]
    public Guid SoilId { get; set; }

    [Required]
    [Display(Name = "Nozzle")]
    public Guid NozzleId { get; set; }

    [Display(Name = "Crop")]
    public Guid? CropId { get; set; }

    [Display(Name = "Active")]
    public bool Active { get; set; } = true;
}

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
    [Display(Name = "Size (Wetted Acres)")]
    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal SizeAcres { get; set; }

    public string? SoilName { get; set; }

    public string? CropName { get; set; }

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
    
    [Display(Name = "Weekly Rate (inches/week)")]
    [Range(0, double.MaxValue, ErrorMessage = "Weekly rate must be a positive number.")]
    public decimal? WeeklyRateInches { get; set; }

    public List<SprayfieldZoneDetailViewModel> Zones { get; set; } = new();
}

public class SprayfieldZoneDetailViewModel
{
    public Guid Id { get; set; }

    public string ZoneName { get; set; } = string.Empty;

    public decimal PercentOfField { get; set; }

    public decimal Acres { get; set; }

    public string? SoilName { get; set; }

    public string? NozzleName { get; set; }

    public string? CropName { get; set; }

    public bool Active { get; set; }
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
    [Display(Name = "Size (Wetted Acres)")]
    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal SizeAcres { get; set; }

    [Display(Name = "Facility")]
    public Guid? FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Hydraulic Loading Limit (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hydraulic loading limit must be a positive number.")]
    public decimal HydraulicLoadingLimitInPerYr { get; set; }
    
    [Display(Name = "Hourly Rate (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal? HourlyRateInches { get; set; }
    
    [Display(Name = "Weekly Rate (inches/week)")]
    [Range(0, double.MaxValue, ErrorMessage = "Weekly rate must be a positive number.")]
    public decimal? WeeklyRateInches { get; set; }

    public List<ApplicationZoneInputViewModel> Zones { get; set; } = new();
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
    [Display(Name = "Size (Wetted Acres)")]
    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal SizeAcres { get; set; }

    [Display(Name = "Facility")]
    public Guid? FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Hydraulic Loading Limit (inches/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hydraulic loading limit must be a positive number.")]
    public decimal HydraulicLoadingLimitInPerYr { get; set; }
    
    [Display(Name = "Hourly Rate (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal? HourlyRateInches { get; set; }
    
    [Display(Name = "Weekly Rate (inches/week)")]
    [Range(0, double.MaxValue, ErrorMessage = "Weekly rate must be a positive number.")]
    public decimal? WeeklyRateInches { get; set; }

    public List<ApplicationZoneInputViewModel> Zones { get; set; } = new();

    [Display(Name = "Removed Zone Action")]
    public string? RemovedZoneAction { get; set; } = "delete";

    [Display(Name = "Reassign To Zone")]
    public Guid? ReassignToZoneId { get; set; }
}

public class SprayfieldBulkEditViewModel
{
    public List<Guid> SelectedSprayfieldIds { get; set; } = new();

    [Range(0, double.MaxValue, ErrorMessage = "Size must be a positive number.")]
    public decimal? SizeAcres { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Hourly rate must be a positive number.")]
    public decimal? HourlyRateInches { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Weekly rate must be a positive number.")]
    public decimal? WeeklyRateInches { get; set; }

    public Guid? FacilityId { get; set; }

    public List<SprayfieldBulkZoneEditViewModel> ZoneEdits { get; set; } = new();
}

public class SprayfieldBulkZoneEditViewModel
{
    public Guid SprayfieldId { get; set; }

    public string FieldId { get; set; } = string.Empty;

    public bool ApplyZoneChanges { get; set; }

    public List<ApplicationZoneInputViewModel> Zones { get; set; } = new();

    [Display(Name = "Removed Zone Action")]
    public string? RemovedZoneAction { get; set; } = "delete";

    [Display(Name = "Reassign To Zone")]
    public Guid? ReassignToZoneId { get; set; }
}



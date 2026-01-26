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
    
    [Required]
    [Display(Name = "Shift")]
    public ShiftEnum Shift { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string WeatherConditions { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "System Status")]
    public SystemStatusEnum SystemStatus { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Maintenance Performed")]
    public string MaintenancePerformed { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Equipment Inspected")]
    public string EquipmentInspected { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Issues Noted")]
    public string IssuesNoted { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Corrective Actions")]
    public string CorrectiveActions { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Next Shift Notes")]
    public string NextShiftNotes { get; set; } = string.Empty;
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
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Operator Name")]
    public string OperatorName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Shift")]
    public ShiftEnum Shift { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string WeatherConditions { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "System Status")]
    public SystemStatusEnum SystemStatus { get; set; } = SystemStatusEnum.Normal;
    
    [StringLength(2000)]
    [Display(Name = "Maintenance Performed")]
    public string MaintenancePerformed { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Equipment Inspected")]
    public string EquipmentInspected { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Issues Noted")]
    public string IssuesNoted { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Corrective Actions")]
    public string CorrectiveActions { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Next Shift Notes")]
    public string NextShiftNotes { get; set; } = string.Empty;
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
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Operator Name")]
    public string OperatorName { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Shift")]
    public ShiftEnum Shift { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Weather Conditions")]
    public string WeatherConditions { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "System Status")]
    public SystemStatusEnum SystemStatus { get; set; }
    
    [StringLength(2000)]
    [Display(Name = "Maintenance Performed")]
    public string MaintenancePerformed { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Equipment Inspected")]
    public string EquipmentInspected { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Issues Noted")]
    public string IssuesNoted { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Corrective Actions")]
    public string CorrectiveActions { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Next Shift Notes")]
    public string NextShiftNotes { get; set; } = string.Empty;
}



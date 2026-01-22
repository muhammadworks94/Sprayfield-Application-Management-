using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class NozzleViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Flow Rate (GPM)")]
    [Range(0, double.MaxValue, ErrorMessage = "Flow rate must be a positive number.")]
    public decimal FlowRateGpm { get; set; }
    
    [Required]
    [Display(Name = "Spray Arc (degrees)")]
    [Range(0, 360, ErrorMessage = "Spray arc must be between 0 and 360 degrees.")]
    public int SprayArc { get; set; }
}

public class NozzleCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Flow Rate (GPM)")]
    [Range(0, double.MaxValue, ErrorMessage = "Flow rate must be a positive number.")]
    public decimal FlowRateGpm { get; set; }
    
    [Required]
    [Display(Name = "Spray Arc (degrees)")]
    [Range(0, 360, ErrorMessage = "Spray arc must be between 0 and 360 degrees.")]
    public int SprayArc { get; set; }
}

public class NozzleEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Flow Rate (GPM)")]
    [Range(0, double.MaxValue, ErrorMessage = "Flow rate must be a positive number.")]
    public decimal FlowRateGpm { get; set; }
    
    [Required]
    [Display(Name = "Spray Arc (degrees)")]
    [Range(0, 360, ErrorMessage = "Spray arc must be between 0 and 360 degrees.")]
    public int SprayArc { get; set; }
}


using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class CropViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Crop Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "PAN Factor")]
    [Range(0, 1, ErrorMessage = "PAN factor must be between 0 and 1.")]
    public decimal PanFactor { get; set; }
    
    [Required]
    [Display(Name = "N Uptake (lbs/acre/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "N Uptake must be a positive number.")]
    public decimal NUptake { get; set; }
}

public class CropCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Crop Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "PAN Factor")]
    [Range(0, 1, ErrorMessage = "PAN factor must be between 0 and 1.")]
    public decimal PanFactor { get; set; }
    
    [Required]
    [Display(Name = "N Uptake (lbs/acre/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "N Uptake must be a positive number.")]
    public decimal NUptake { get; set; }
}

public class CropEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Crop Name")]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "PAN Factor")]
    [Range(0, 1, ErrorMessage = "PAN factor must be between 0 and 1.")]
    public decimal PanFactor { get; set; }
    
    [Required]
    [Display(Name = "N Uptake (lbs/acre/year)")]
    [Range(0, double.MaxValue, ErrorMessage = "N Uptake must be a positive number.")]
    public decimal NUptake { get; set; }
}



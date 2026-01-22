using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class MonitoringWellViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Well ID")]
    public string WellId { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Location Description")]
    public string LocationDescription { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal Latitude { get; set; }
    
    [Required]
    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal Longitude { get; set; }
}

public class MonitoringWellCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Well ID")]
    public string WellId { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Location Description")]
    public string LocationDescription { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal Latitude { get; set; }
    
    [Required]
    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal Longitude { get; set; }
}

public class MonitoringWellEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(100)]
    [Display(Name = "Well ID")]
    public string WellId { get; set; } = string.Empty;
    
    [StringLength(500)]
    [Display(Name = "Location Description")]
    public string LocationDescription { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Latitude")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public decimal Latitude { get; set; }
    
    [Required]
    [Display(Name = "Longitude")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public decimal Longitude { get; set; }
}


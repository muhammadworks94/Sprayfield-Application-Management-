using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class SoilViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Type Name")]
    public string TypeName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Permeability (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Permeability must be a positive number.")]
    public decimal Permeability { get; set; }
}

public class SoilCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Type Name")]
    public string TypeName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Permeability (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Permeability must be a positive number.")]
    public decimal Permeability { get; set; }
}

public class SoilEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [StringLength(200)]
    [Display(Name = "Type Name")]
    public string TypeName { get; set; } = string.Empty;
    
    [StringLength(1000)]
    [Display(Name = "Description")]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Display(Name = "Permeability (inches/hour)")]
    [Range(0, double.MaxValue, ErrorMessage = "Permeability must be a positive number.")]
    public decimal Permeability { get; set; }
}


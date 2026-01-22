using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.OperationalData;

public class GWMonitViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    public Guid MonitoringWellId { get; set; }
    public string? MonitoringWellName { get; set; }
    
    [Required]
    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; }
    
    [Display(Name = "Sample Depth (feet)")]
    [Range(0, double.MaxValue)]
    public decimal? SampleDepth { get; set; }
    
    [Display(Name = "Water Level (feet)")]
    [Range(0, double.MaxValue)]
    public decimal? WaterLevel { get; set; }
    
    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? Temperature { get; set; }
    
    [Display(Name = "pH")]
    [Range(0, 14)]
    public decimal? PH { get; set; }
    
    [Display(Name = "Conductivity (µS/cm)")]
    [Range(0, double.MaxValue)]
    public decimal? Conductivity { get; set; }
    
    [Display(Name = "TDS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TDS { get; set; }
    
    [Display(Name = "Turbidity (NTU)")]
    [Range(0, double.MaxValue)]
    public decimal? Turbidity { get; set; }
    
    [Display(Name = "BOD5 (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? BOD5 { get; set; }
    
    [Display(Name = "COD (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? COD { get; set; }
    
    [Display(Name = "TSS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TSS { get; set; }
    
    [Display(Name = "NH3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NH3N { get; set; }
    
    [Display(Name = "NO3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NO3N { get; set; }
    
    [Display(Name = "TKN (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TKN { get; set; }
    
    [Display(Name = "Total Phosphorus (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalPhosphorus { get; set; }
    
    [Display(Name = "Chloride (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Chloride { get; set; }
    
    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? FecalColiform { get; set; }
    
    [Display(Name = "Total Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalColiform { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Lab Certification")]
    public string LabCertification { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;
}

public class GWMonitCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Monitoring Well")]
    public Guid MonitoringWellId { get; set; }
    
    [Required]
    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; } = DateTime.Today;
    
    [Display(Name = "Sample Depth (feet)")]
    [Range(0, double.MaxValue)]
    public decimal? SampleDepth { get; set; }
    
    [Display(Name = "Water Level (feet)")]
    [Range(0, double.MaxValue)]
    public decimal? WaterLevel { get; set; }
    
    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? Temperature { get; set; }
    
    [Display(Name = "pH")]
    [Range(0, 14)]
    public decimal? PH { get; set; }
    
    [Display(Name = "Conductivity (µS/cm)")]
    [Range(0, double.MaxValue)]
    public decimal? Conductivity { get; set; }
    
    [Display(Name = "TDS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TDS { get; set; }
    
    [Display(Name = "Turbidity (NTU)")]
    [Range(0, double.MaxValue)]
    public decimal? Turbidity { get; set; }
    
    [Display(Name = "BOD5 (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? BOD5 { get; set; }
    
    [Display(Name = "COD (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? COD { get; set; }
    
    [Display(Name = "TSS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TSS { get; set; }
    
    [Display(Name = "NH3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NH3N { get; set; }
    
    [Display(Name = "NO3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NO3N { get; set; }
    
    [Display(Name = "TKN (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TKN { get; set; }
    
    [Display(Name = "Total Phosphorus (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalPhosphorus { get; set; }
    
    [Display(Name = "Chloride (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Chloride { get; set; }
    
    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? FecalColiform { get; set; }
    
    [Display(Name = "Total Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalColiform { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Lab Certification")]
    public string LabCertification { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;
}

public class GWMonitEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Monitoring Well")]
    public Guid MonitoringWellId { get; set; }
    
    [Required]
    [Display(Name = "Sample Date")]
    [DataType(DataType.Date)]
    public DateTime SampleDate { get; set; }
    
    [Display(Name = "Sample Depth (feet)")]
    [Range(0, double.MaxValue)]
    public decimal? SampleDepth { get; set; }
    
    [Display(Name = "Water Level (feet)")]
    [Range(0, double.MaxValue)]
    public decimal? WaterLevel { get; set; }
    
    [Display(Name = "Temperature (°F)")]
    [Range(0, double.MaxValue)]
    public decimal? Temperature { get; set; }
    
    [Display(Name = "pH")]
    [Range(0, 14)]
    public decimal? PH { get; set; }
    
    [Display(Name = "Conductivity (µS/cm)")]
    [Range(0, double.MaxValue)]
    public decimal? Conductivity { get; set; }
    
    [Display(Name = "TDS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TDS { get; set; }
    
    [Display(Name = "Turbidity (NTU)")]
    [Range(0, double.MaxValue)]
    public decimal? Turbidity { get; set; }
    
    [Display(Name = "BOD5 (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? BOD5 { get; set; }
    
    [Display(Name = "COD (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? COD { get; set; }
    
    [Display(Name = "TSS (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TSS { get; set; }
    
    [Display(Name = "NH3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NH3N { get; set; }
    
    [Display(Name = "NO3-N (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? NO3N { get; set; }
    
    [Display(Name = "TKN (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TKN { get; set; }
    
    [Display(Name = "Total Phosphorus (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalPhosphorus { get; set; }
    
    [Display(Name = "Chloride (mg/L)")]
    [Range(0, double.MaxValue)]
    public decimal? Chloride { get; set; }
    
    [Display(Name = "Fecal Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? FecalColiform { get; set; }
    
    [Display(Name = "Total Coliform (CFU/100mL)")]
    [Range(0, double.MaxValue)]
    public decimal? TotalColiform { get; set; }
    
    [StringLength(500)]
    [Display(Name = "Lab Certification")]
    public string LabCertification { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;
    
    [StringLength(2000)]
    [Display(Name = "Comments")]
    public string Comments { get; set; } = string.Empty;
}


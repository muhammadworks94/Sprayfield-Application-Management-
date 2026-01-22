using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.OperationalData;

public class WWCharViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public Guid FacilityId { get; set; }
    public string? FacilityName { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Display(Name = "BOD5 Daily (mg/L)")]
    public List<decimal?> BOD5Daily { get; set; } = new();
    
    [Display(Name = "TSS Daily (mg/L)")]
    public List<decimal?> TSSDaily { get; set; } = new();
    
    [Display(Name = "Flow Rate Daily (GPM)")]
    public List<decimal?> FlowRateDaily { get; set; } = new();
    
    [Display(Name = "pH Daily")]
    public List<decimal?> PHDaily { get; set; } = new();
    
    [Display(Name = "NH3-N Daily (mg/L)")]
    public List<decimal?> NH3NDaily { get; set; } = new();
    
    [Display(Name = "Fecal Coliform Daily (CFU/100mL)")]
    public List<decimal?> FecalColiformDaily { get; set; } = new();
    
    [Display(Name = "Total Coliform Daily (CFU/100mL)")]
    public List<decimal?> TotalColiformDaily { get; set; } = new();
    
    [Display(Name = "Chloride Daily (mg/L)")]
    public List<decimal?> ChlorideDaily { get; set; } = new();
    
    [Display(Name = "TDS Daily (mg/L)")]
    public List<decimal?> TDSDaily { get; set; } = new();
    
    [Display(Name = "Composite Time Daily")]
    public List<string?> CompositeTime { get; set; } = new();
    
    [Display(Name = "ORC On Site Daily")]
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    
    [Display(Name = "Lagoon Freeboard Daily (inches)")]
    public List<decimal?> LagoonFreeboard { get; set; } = new();
    
    [StringLength(500)]
    [Display(Name = "Lab Certification")]
    public string LabCertification { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;
}

public class WWCharCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; } = (MonthEnum)DateTime.Now.Month;
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; } = DateTime.Now.Year;
    
    [Display(Name = "BOD5 Daily (mg/L)")]
    public List<decimal?> BOD5Daily { get; set; } = new();
    
    [Display(Name = "TSS Daily (mg/L)")]
    public List<decimal?> TSSDaily { get; set; } = new();
    
    [Display(Name = "Flow Rate Daily (GPM)")]
    public List<decimal?> FlowRateDaily { get; set; } = new();
    
    [Display(Name = "pH Daily")]
    public List<decimal?> PHDaily { get; set; } = new();
    
    [Display(Name = "NH3-N Daily (mg/L)")]
    public List<decimal?> NH3NDaily { get; set; } = new();
    
    [Display(Name = "Fecal Coliform Daily (CFU/100mL)")]
    public List<decimal?> FecalColiformDaily { get; set; } = new();
    
    [Display(Name = "Total Coliform Daily (CFU/100mL)")]
    public List<decimal?> TotalColiformDaily { get; set; } = new();
    
    [Display(Name = "Chloride Daily (mg/L)")]
    public List<decimal?> ChlorideDaily { get; set; } = new();
    
    [Display(Name = "TDS Daily (mg/L)")]
    public List<decimal?> TDSDaily { get; set; } = new();
    
    [Display(Name = "Composite Time Daily")]
    public List<string?> CompositeTime { get; set; } = new();
    
    [Display(Name = "ORC On Site Daily")]
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    
    [Display(Name = "Lagoon Freeboard Daily (inches)")]
    public List<decimal?> LagoonFreeboard { get; set; } = new();
    
    [StringLength(500)]
    [Display(Name = "Lab Certification")]
    public string LabCertification { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;
}

public class WWCharEditViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }
    
    [Required]
    [Display(Name = "Facility")]
    public Guid FacilityId { get; set; }
    
    [Required]
    [Display(Name = "Month")]
    public MonthEnum Month { get; set; }
    
    [Required]
    [Display(Name = "Year")]
    [Range(2000, 2100)]
    public int Year { get; set; }
    
    [Display(Name = "BOD5 Daily (mg/L)")]
    public List<decimal?> BOD5Daily { get; set; } = new();
    
    [Display(Name = "TSS Daily (mg/L)")]
    public List<decimal?> TSSDaily { get; set; } = new();
    
    [Display(Name = "Flow Rate Daily (GPM)")]
    public List<decimal?> FlowRateDaily { get; set; } = new();
    
    [Display(Name = "pH Daily")]
    public List<decimal?> PHDaily { get; set; } = new();
    
    [Display(Name = "NH3-N Daily (mg/L)")]
    public List<decimal?> NH3NDaily { get; set; } = new();
    
    [Display(Name = "Fecal Coliform Daily (CFU/100mL)")]
    public List<decimal?> FecalColiformDaily { get; set; } = new();
    
    [Display(Name = "Total Coliform Daily (CFU/100mL)")]
    public List<decimal?> TotalColiformDaily { get; set; } = new();
    
    [Display(Name = "Chloride Daily (mg/L)")]
    public List<decimal?> ChlorideDaily { get; set; } = new();
    
    [Display(Name = "TDS Daily (mg/L)")]
    public List<decimal?> TDSDaily { get; set; } = new();
    
    [Display(Name = "Composite Time Daily")]
    public List<string?> CompositeTime { get; set; } = new();
    
    [Display(Name = "ORC On Site Daily")]
    public List<ORCOnSiteEnum?> ORCOnSite { get; set; } = new();
    
    [Display(Name = "Lagoon Freeboard Daily (inches)")]
    public List<decimal?> LagoonFreeboard { get; set; } = new();
    
    [StringLength(500)]
    [Display(Name = "Lab Certification")]
    public string LabCertification { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Collected By")]
    public string CollectedBy { get; set; } = string.Empty;
    
    [StringLength(200)]
    [Display(Name = "Analyzed By")]
    public string AnalyzedBy { get; set; } = string.Empty;
}


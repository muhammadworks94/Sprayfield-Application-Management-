using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.CompanyManagement;

public class NewClientSetupPage1ViewModel
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(200)]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;

    [Range(1, 12)]
    [Display(Name = "First Reporting Month")]
    public int FirstReportingMonth { get; set; } = DateTime.UtcNow.Month;

    [Range(2000, 2100)]
    [Display(Name = "First Reporting Year")]
    public int FirstReportingYear { get; set; } = DateTime.UtcNow.Year;

    [Range(1, 10)]
    [Display(Name = "Number of Facilities")]
    public int FacilityCount { get; set; } = 1;

    public List<NewClientSetupFacilityViewModel> Facilities { get; set; } = new();
}

public class NewClientSetupFacilityViewModel
{
    [Required]
    [StringLength(200)]
    [Display(Name = "Facility Name")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 150)]
    [Display(Name = "Number of Sprayfields")]
    public int SprayfieldCount { get; set; } = 1;

    public List<string> SprayfieldFieldCodes { get; set; } = new();
}

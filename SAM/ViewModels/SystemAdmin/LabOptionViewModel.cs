using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemAdmin;

public class LabOptionViewModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CertificationNumber { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LabOptionsPartialViewModel
{
    public IEnumerable<LabOptionViewModel> LabOptions { get; set; } = new List<LabOptionViewModel>();
    public bool IsGlobalAdmin { get; set; }
    public bool CanManageRecords { get; set; }
    public Microsoft.AspNetCore.Mvc.Rendering.SelectList? Companies { get; set; }
    public Guid? SelectedCompanyId { get; set; }
}

public class LabOptionCreateViewModel
{
    [Required]
    [Display(Name = "Company")]
    public Guid CompanyId { get; set; }

    [Required]
    [StringLength(200)]
    [Display(Name = "Certified Laboratory")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Lab Certification No.")]
    public string CertificationNumber { get; set; } = string.Empty;
}

public class LabOptionEditViewModel : LabOptionCreateViewModel
{
    public Guid Id { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

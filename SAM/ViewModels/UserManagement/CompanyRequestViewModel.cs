using System.ComponentModel.DataAnnotations;
using SAM.Domain.Enums;

namespace SAM.ViewModels.UserManagement;

public class CompanyRequestViewModel
{
    public Guid Id { get; set; }
    
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;
    
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Display(Name = "Website")]
    public string? Website { get; set; }
    
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Display(Name = "Requester Name")]
    public string RequesterFullName { get; set; } = string.Empty;
    
    [Display(Name = "Requester Email")]
    public string RequesterEmail { get; set; } = string.Empty;
    
    [Display(Name = "Status")]
    public RequestStatusEnum Status { get; set; }
    
    [Display(Name = "Created Date")]
    public DateTime CreatedDate { get; set; }
    
    public Guid? CreatedCompanyId { get; set; }
    public Guid? CreatedUserId { get; set; }
    
    [Display(Name = "Rejection Reason")]
    public string? RejectionReason { get; set; }
}

public class CompanyRequestApproveViewModel
{
    public Guid Id { get; set; }
    
    [Required]
    [Display(Name = "Company Name")]
    public string CompanyName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [Display(Name = "Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;
    
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = string.Empty;
    
    [Url]
    [Display(Name = "Website")]
    public string? Website { get; set; }
    
    [Display(Name = "Description")]
    public string? Description { get; set; }
    
    [Required]
    [Display(Name = "Requester Name")]
    public string RequesterFullName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [Display(Name = "Requester Email")]
    public string RequesterEmail { get; set; } = string.Empty;
}


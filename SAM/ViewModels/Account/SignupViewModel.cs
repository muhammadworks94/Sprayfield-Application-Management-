using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.Account;

/// <summary>
/// ViewModel for user signup/registration.
/// </summary>
public class SignupViewModel
{
    /// <summary>
    /// Type of signup: Join existing company or create new company.
    /// </summary>
    [Required]
    [Display(Name = "Signup Type")]
    public SignupType SignupType { get; set; } = SignupType.JoinExisting;

    /// <summary>
    /// Full name of the user.
    /// </summary>
    [Required]
    [StringLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the user.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Selected company ID (for JoinExisting signup type).
    /// </summary>
    [Display(Name = "Select Company")]
    public Guid? SelectedCompanyId { get; set; }

    /// <summary>
    /// Company name (for CreateNew signup type).
    /// </summary>
    [Display(Name = "Company Name")]
    [StringLength(200)]
    public string? CompanyName { get; set; }

    /// <summary>
    /// Company phone number (for CreateNew signup type).
    /// </summary>
    [Phone]
    [Display(Name = "Phone Number")]
    [StringLength(50)]
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Company website URL (for CreateNew signup type).
    /// </summary>
    [Url]
    [Display(Name = "Website")]
    [StringLength(500)]
    public string? Website { get; set; }

    /// <summary>
    /// Company description (for CreateNew signup type).
    /// </summary>
    [Display(Name = "Description")]
    [StringLength(1000)]
    public string? Description { get; set; }
}

/// <summary>
/// Enumeration for signup types.
/// </summary>
public enum SignupType
{
    JoinExisting,
    CreateNew
}


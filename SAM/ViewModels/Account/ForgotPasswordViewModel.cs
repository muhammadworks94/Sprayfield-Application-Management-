using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.Account;

/// <summary>
/// ViewModel for initiating a password reset (forgot password).
/// </summary>
public class ForgotPasswordViewModel
{
    /// <summary>
    /// Email address of the user requesting a password reset.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}



using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemSettings;

/// <summary>
/// View model for editing SMTP settings in system settings.
/// </summary>
public class SmtpSettingsViewModel
{
    [Required]
    [Display(Name = "SMTP Host")]
    [StringLength(255)]
    public string Host { get; set; } = string.Empty;

    [Required]
    [Range(1, 65535)]
    [Display(Name = "SMTP Port")]
    public int Port { get; set; } = 587;

    [Display(Name = "Username")]
    [StringLength(256)]
    public string? Username { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string? Password { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "From Email")]
    [StringLength(256)]
    public string FromEmail { get; set; } = string.Empty;

    [Display(Name = "Enable SSL/TLS")]
    public bool EnableSsl { get; set; } = true;

    public bool HasPasswordConfigured { get; set; }
}

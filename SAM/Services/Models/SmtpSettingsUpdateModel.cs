namespace SAM.Services.Models;

/// <summary>
/// Input model for creating/updating global SMTP settings.
/// </summary>
public class SmtpSettingsUpdateModel
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

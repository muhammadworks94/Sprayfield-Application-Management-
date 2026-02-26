namespace SAM.Services.Models;

/// <summary>
/// Runtime SMTP options with decrypted password for outbound email.
/// </summary>
public class SmtpRuntimeOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? Username { get; set; }
    public string Password { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

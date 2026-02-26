using SAM.Domain.Entities.Base;

namespace SAM.Domain.Entities;

/// <summary>
/// Stores global SMTP settings used for outbound emails.
/// </summary>
public class SmtpSettings : AuditableEntity
{
    /// <summary>
    /// Scope key for singleton configuration.
    /// </summary>
    public string ScopeKey { get; set; } = "SMTP_GLOBAL";

    /// <summary>
    /// SMTP server host.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Optional username for SMTP auth.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Encrypted SMTP password.
    /// </summary>
    public string PasswordEncrypted { get; set; } = string.Empty;

    /// <summary>
    /// From email address for outgoing mail.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS.
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}

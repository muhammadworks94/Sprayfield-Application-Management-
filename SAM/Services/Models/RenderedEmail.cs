namespace SAM.Services.Models;

/// <summary>
/// Represents a rendered email message.
/// </summary>
public class RenderedEmail
{
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public bool UsedEmergencyFallback { get; set; }
}

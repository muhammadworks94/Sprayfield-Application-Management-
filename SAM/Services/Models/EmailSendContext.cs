namespace SAM.Services.Models;

public class EmailSendContext
{
    public string? TemplateKey { get; set; }
    public string? TemplateDisplayName { get; set; }
    public string? InitiatedByUserId { get; set; }
    public string? InitiatedByEmail { get; set; }
    public string? InitiatedByDisplayName { get; set; }
}

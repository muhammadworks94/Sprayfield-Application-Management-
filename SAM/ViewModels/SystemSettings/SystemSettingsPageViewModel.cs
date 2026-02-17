using System.ComponentModel.DataAnnotations;

namespace SAM.ViewModels.SystemSettings;

public class SystemSettingsPageViewModel
{
    public SmtpSettingsViewModel Smtp { get; set; } = new();
    public EmailTemplateEditorViewModel TemplateEditor { get; set; } = new();
    public List<EmailTemplateSummaryViewModel> Templates { get; set; } = [];
}

public class EmailTemplateSummaryViewModel
{
    public string TemplateKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class EmailTemplateEditorViewModel
{
    [Required]
    [StringLength(100)]
    public string TemplateKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    [Display(Name = "Email Subject")]
    public string SubjectTemplate { get; set; } = string.Empty;

    [Required]
    [StringLength(8000)]
    [Display(Name = "Email Body (HTML)")]
    public string BodyTemplate { get; set; } = string.Empty;

    public IReadOnlyList<string> AllowedTokens { get; set; } = Array.Empty<string>();
}

namespace SAM.Services.Models;

/// <summary>
/// System email template keys and supported tokens.
/// All outbound system emails render from the EmailTemplates table via IEmailTemplateService.
/// </summary>
public static class EmailTemplateCatalog
{
    public const string PasswordReset = "password_reset";
    public const string UserCredentials = "user_credentials";
    public const string SmtpTest = "smtp_test";
    public const string CompanyRequestApproved = "company_request_approved";
    public const string CompanyRequestRejected = "company_request_rejected";
    public const string UserRequestRejected = "user_request_rejected";

    public const string AppName = "SAM";

    public static readonly string[] SystemKeys =
    [
        PasswordReset,
        UserCredentials,
        SmtpTest,
        CompanyRequestApproved,
        CompanyRequestRejected,
        UserRequestRejected
    ];

    public static bool IsSystemKey(string templateKey) =>
        SystemKeys.Contains(templateKey, StringComparer.Ordinal);

    public static int GetOrder(string templateKey)
    {
        var index = Array.IndexOf(SystemKeys, templateKey);
        return index >= 0 ? index : int.MaxValue;
    }

    public static string GetDisplayName(string templateKey)
    {
        return templateKey switch
        {
            PasswordReset => "Password Reset",
            UserCredentials => "User Credentials",
            SmtpTest => "SMTP Test Email",
            CompanyRequestApproved => "Company Request Approved",
            CompanyRequestRejected => "Company Request Rejected",
            UserRequestRejected => "User Request Rejected",
            _ => templateKey
        };
    }

    public static IReadOnlyList<string> GetAllowedTokens(string templateKey)
    {
        return templateKey switch
        {
            PasswordReset =>
            [
                "{{AppName}}",
                "{{ResetLink}}",
                "{{RecipientEmail}}"
            ],
            UserCredentials =>
            [
                "{{AppName}}",
                "{{RecipientEmail}}",
                "{{TemporaryPassword}}",
                "{{CompanyName}}",
                "{{RoleName}}"
            ],
            SmtpTest =>
            [
                "{{AppName}}",
                "{{RecipientEmail}}",
                "{{SentAtUtc}}"
            ],
            CompanyRequestApproved =>
            [
                "{{AppName}}",
                "{{RecipientEmail}}",
                "{{CompanyName}}",
                "{{TemporaryPassword}}",
                "{{RoleName}}"
            ],
            CompanyRequestRejected =>
            [
                "{{AppName}}",
                "{{RecipientEmail}}",
                "{{CompanyName}}",
                "{{RejectionReason}}"
            ],
            UserRequestRejected =>
            [
                "{{AppName}}",
                "{{RecipientEmail}}",
                "{{CompanyName}}",
                "{{RejectionReason}}"
            ],
            _ => Array.Empty<string>()
        };
    }
}

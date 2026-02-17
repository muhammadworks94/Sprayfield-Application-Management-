namespace SAM.Services.Models;

/// <summary>
/// System email template keys and supported tokens.
/// </summary>
public static class EmailTemplateCatalog
{
    public const string PasswordReset = "password_reset";
    public const string UserCredentials = "user_credentials";
    public const string SmtpTest = "smtp_test";

    public const string AppName = "SAM";

    public static readonly string[] SystemKeys =
    [
        PasswordReset,
        UserCredentials,
        SmtpTest
    ];

    public static bool IsSystemKey(string templateKey) =>
        SystemKeys.Contains(templateKey, StringComparer.Ordinal);

    public static int GetOrder(string templateKey)
    {
        var index = Array.IndexOf(SystemKeys, templateKey);
        return index >= 0 ? index : int.MaxValue;
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
            _ => Array.Empty<string>()
        };
    }
}

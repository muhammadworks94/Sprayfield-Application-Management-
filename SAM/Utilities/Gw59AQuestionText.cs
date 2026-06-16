namespace SAM.Utilities;

/// <summary>
/// GW-59A questionnaire text as printed on the official form.
/// </summary>
public static class Gw59AQuestionText
{
    public static string FormatQuestion1(DateTime? dueDate)
    {
        var dueDateText = dueDate?.ToString("MM/dd/yyyy") ?? "__________";
        return $"1. Enter date monitoring results were due. ({dueDateText}) Will this monitoring report (GW-59 and GW-59A) be submitted after the established due date?";
    }

    public const string Question2 =
        "2. Was any required information missing on the GW-59 report forms?";

    public const string Question3 =
        "3. Are any of the monitor wells in need of repair or maintenance (damaged casing, unlocked or missing cap, missing identification plate, area overgrown, etc.)? If the answer is \"Yes\", contact the Regional Office for guidance.";

    public const string Question4 =
        "4. Are any monitored constituents equal to or above the established standards?";

    public const string Question5 =
        "5. For the constituents identified in question 4 above, have standards been exceeded previously for the same constituent(s) in the same well(s) in the last two years?";

    public const string Question6 =
        "6. Are the monitoring wells listed in section 5 located at or beyond the review boundary?";

    public const string Question7 =
        "7. Is the permittee implementing previously approved actions required by the Division involving this groundwater quality problem?";

    public const string Question2DetailsPrompt =
        "If the answer to question 1 or 2 is \"YES\", list the well identification number(s) and explain the problems encountered in obtaining the required information.";

    public const string Question4DetailsPrompt =
        "If the answer to question 4 is \"YES\", list the affected wells individually with constituent(s) and concentration(s) exceeding standards.";

    public const string Question5DetailsPrompt =
        "If the answer to question 5 is \"YES\", list each well with constituent(s) exceeding standards, concentration(s) reported, and sample collection date for each occurrence (for the last two years).";

    public const string Question7DetailsPrompt =
        "If the answer to question 7 is \"YES\", describe those actions.";
}

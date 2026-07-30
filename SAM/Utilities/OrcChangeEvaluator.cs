namespace SAM.Utilities;

/// <summary>
/// Computes whether reports should mark "Has the ORC changed since the previous …?".
/// Yes only for the report month immediately after an ORC assignment end month.
/// </summary>
public static class OrcChangeEvaluator
{
    public static bool HasOrcChangedSincePrevious(IEnumerable<DateTime?> assignmentEndDates, int reportYear, int reportMonth)
    {
        var (prevYear, prevMonth) = GetPreviousReportMonth(reportYear, reportMonth);

        foreach (var endDate in assignmentEndDates)
        {
            if (!endDate.HasValue)
            {
                continue;
            }

            var end = endDate.Value.Date;
            if (end.Year == prevYear && end.Month == prevMonth)
            {
                return true;
            }
        }

        return false;
    }

    public static (int Year, int Month) GetPreviousReportMonth(int reportYear, int reportMonth)
    {
        if (reportMonth <= 1)
        {
            return (reportYear - 1, 12);
        }

        return (reportYear, reportMonth - 1);
    }
}

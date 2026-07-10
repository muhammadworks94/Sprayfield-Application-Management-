namespace SAM.Services.Helpers;

public static class BaselineWindowHelper
{
    public const int HistoricalMonthCount = 11;

    public static IReadOnlyList<(int Year, int Month)> GetHistoricalMonths(int firstReportingYear, int firstReportingMonth)
    {
        var firstReportingMonthDate = new DateTime(firstReportingYear, firstReportingMonth, 1);
        return Enumerable.Range(1, HistoricalMonthCount)
            .Select(offset =>
            {
                var monthDate = firstReportingMonthDate.AddMonths(-offset);
                return (monthDate.Year, monthDate.Month);
            })
            .ToList();
    }
}

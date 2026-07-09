namespace SAM.Services.Helpers;

public static class BaselineWindowHelper
{
    public const int HistoricalMonthCount = 12;

    public static IReadOnlyList<(int Year, int Month)> GetHistoricalMonths(int firstReportingYear, int firstReportingMonth)
    {
        var endMonth = new DateTime(firstReportingYear, firstReportingMonth, 1);
        return Enumerable.Range(0, HistoricalMonthCount)
            .Select(offset =>
            {
                var monthDate = endMonth.AddMonths(-offset);
                return (monthDate.Year, monthDate.Month);
            })
            .ToList();
    }
}

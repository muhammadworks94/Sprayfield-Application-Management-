using SAM.Domain.Entities;
using SAM.Domain.Enums;
using SAM.Utilities;

namespace SAM.Services.Helpers;

public static class WWCharCompletenessCalculator
{
    public const string PhPcsCode = "00400";

    public static int ToCompletePercent(int completeDays, int daysInMonth) =>
        daysInMonth == 0 ? 0 : (int)Math.Round(100.0 * completeDays / daysInMonth);

    public static int CountOrcCompleteDays(
        IReadOnlyList<ORCOnSiteEnum?> orcOnSite,
        IReadOnlyList<string?> orcArrivalTime,
        IReadOnlyList<decimal?> orcTimeOnSiteHours,
        int daysInMonth)
    {
        var count = 0;
        for (var day = 1; day <= daysInMonth; day++)
        {
            var index = day - 1;
            var hasOrcOnSite = index < orcOnSite.Count && orcOnSite[index].HasValue;
            var hasArrivalTime = index < orcArrivalTime.Count && !string.IsNullOrWhiteSpace(orcArrivalTime[index]);
            var hasTimeOnSite = index < orcTimeOnSiteHours.Count && orcTimeOnSiteHours[index].HasValue;

            if (hasOrcOnSite && hasArrivalTime && hasTimeOnSite)
            {
                count++;
            }
        }

        return count;
    }

    public static int CountDailyValueDays(IReadOnlyList<decimal?> mergedDaily, int daysInMonth)
    {
        var count = 0;
        for (var day = 1; day <= daysInMonth; day++)
        {
            var index = day - 1;
            if (index < mergedDaily.Count && mergedDaily[index].HasValue)
            {
                count++;
            }
        }

        return count;
    }

    public static List<decimal?> BuildMergedDailyFromTemplate(
        Guid wwCharId,
        string pcsCode,
        IEnumerable<WWCharTemplateValue> templateValues,
        IReadOnlyDictionary<Guid, string> pcsByTemplateParameterId,
        IReadOnlyList<decimal?>? legacyDaily)
    {
        var merged = Enumerable.Repeat<decimal?>(null, 31).ToList();

        foreach (var value in templateValues.Where(v => v.WWCharId == wwCharId && v.DayNo >= 1 && v.DayNo <= 31))
        {
            if (!pcsByTemplateParameterId.TryGetValue(value.FacilityPermitTemplateParameterId, out var mappedPcs)
                || !string.Equals(mappedPcs, pcsCode, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            merged[value.DayNo - 1] = value.NumericValue;
        }

        if (legacyDaily != null)
        {
            for (var index = 0; index < Math.Min(legacyDaily.Count, 31); index++)
            {
                merged[index] ??= legacyDaily[index];
            }
        }

        return merged;
    }

    public static (List<ORCOnSiteEnum?> ORCOnSite, List<string?> ORCArrivalTime, List<decimal?> ORCTimeOnSiteHours)
        BuildCanonicalOrcDailyFromLogs(IEnumerable<OperatorLog> monthLogs)
    {
        var orc = Enumerable.Repeat<ORCOnSiteEnum?>(null, 31).ToList();
        var arrivalTime = Enumerable.Repeat<string?>(null, 31).ToList();
        var timeOnSiteHours = Enumerable.Repeat<decimal?>(null, 31).ToList();

        foreach (var dayGroup in monthLogs.GroupBy(x => x.LogDate.Date))
        {
            var day = dayGroup.Key.Day;
            if (day < 1 || day > 31)
            {
                continue;
            }

            var canonical = dayGroup
                .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                .ThenByDescending(x => x.CreatedDate)
                .First();

            orc[day - 1] = canonical.ORCOnSite;
            arrivalTime[day - 1] = canonical.ArrivalTime != TimeSpan.Zero
                ? canonical.ArrivalTime.ToString(@"hh\:mm")
                : null;
            timeOnSiteHours[day - 1] = canonical.TimeOnSiteHours != 0m
                ? canonical.TimeOnSiteHours
                : null;
        }

        return (orc, arrivalTime, timeOnSiteHours);
    }

    public static string FlowPcsCode => NdmrFlowFormatting.FlowPcsCode;
}

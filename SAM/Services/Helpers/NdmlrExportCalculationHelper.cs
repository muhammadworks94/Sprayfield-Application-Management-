using SAM.Domain.Entities;
using SAM.Utilities;

namespace SAM.Services.Helpers;

public sealed record NdmlrPanChemistryInputs(decimal? TknMgL, decimal Nh3MgL, decimal? No2MgL, decimal? No3MgL);

public static class NdmlrExportCalculationHelper
{
    public const decimal MonthlyLoadConversionFactor = 8.34e-6m;

    public static (decimal MineralizationRate, decimal VolatilizationRate) ResolvePanRates(Facility? facility)
    {
        var mr = (facility?.MineralizationRatePercent ?? 40m) / 100m;
        var vr = (facility?.VolatilizationRatePercent ?? 50m) / 100m;
        return (mr, vr);
    }

    public static IReadOnlyDictionary<(int Year, int Month), NdmlrPanChemistryInputs> BuildChemistryByMonth(
        IEnumerable<WWChar> wwChars,
        IEnumerable<WWCharTemplateValue> templateValues,
        IReadOnlyDictionary<Guid, string> pcsByTemplateParameterId)
    {
        return wwChars
            .GroupBy(w => (w.Year, (int)w.Month))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g
                        .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
                        .First();
                    var nh3Average = latest.NH3NDaily.Where(v => v.HasValue).Select(v => v!.Value).DefaultIfEmpty(0m).Average();
                    var tknFromTemplate = WWCharChemistryResolver.AverageTemplateValueForPcs(
                        latest.Id,
                        WWCharChemistryResolver.TknPcsCode,
                        templateValues,
                        pcsByTemplateParameterId);
                    var no3FromTemplate = WWCharChemistryResolver.AverageTemplateValueForPcs(
                        latest.Id,
                        WWCharChemistryResolver.No3PcsCode,
                        templateValues,
                        pcsByTemplateParameterId);

                    return new NdmlrPanChemistryInputs(
                        tknFromTemplate ?? latest.TKNN,
                        nh3Average,
                        latest.NO2N,
                        no3FromTemplate ?? latest.NO3N);
                });
    }

    public static decimal? ComputeMonthlyAveragePanMgL(
        NdmlrPanChemistryInputs chemistry,
        decimal mineralizationRate,
        decimal volatilizationRate)
    {
        if (!chemistry.TknMgL.HasValue)
        {
            return null;
        }

        var tkn = chemistry.TknMgL.Value;
        var nh3 = chemistry.Nh3MgL;
        var no2 = chemistry.No2MgL ?? 0m;
        var no3 = chemistry.No3MgL ?? 0m;

        var organicN = (tkn - nh3) > 0m ? (tkn - nh3) : 0m;
        var mineralized = mineralizationRate * organicN;
        var nh3AfterVolatilization = (1m - volatilizationRate) * nh3;
        return mineralized + nh3AfterVolatilization + no2 + no3;
    }

    public static decimal? TryGetMonthlyAveragePanMgL(
        int year,
        int month,
        IReadOnlyDictionary<(int Year, int Month), NdmlrPanChemistryInputs> chemistryByMonth,
        decimal mineralizationRate,
        decimal volatilizationRate)
    {
        if (!chemistryByMonth.TryGetValue((year, month), out var chemistry))
        {
            return null;
        }

        return ComputeMonthlyAveragePanMgL(chemistry, mineralizationRate, volatilizationRate);
    }

    public static decimal? ComputeMonthlyLoadLbsPerAcre(decimal volumeGallons, decimal panMgL, decimal acres)
    {
        if (volumeGallons <= 0m || acres <= 0m)
        {
            return null;
        }

        return (volumeGallons * panMgL * MonthlyLoadConversionFactor) / acres;
    }

    public static Dictionary<int, decimal> BuildForwardCumulativeLoadsByMonthKey(
        IReadOnlyList<int> monthKeysAscending,
        IReadOnlyDictionary<int, decimal> monthlyLoadsByMonthKey)
    {
        var result = new Dictionary<int, decimal>();
        var running = 0m;
        foreach (var monthKey in monthKeysAscending)
        {
            if (monthlyLoadsByMonthKey.TryGetValue(monthKey, out var monthlyLoad))
            {
                running += monthlyLoad;
            }

            result[monthKey] = running;
        }

        return result;
    }

    public static Dictionary<Guid, Dictionary<int, decimal>> BuildMonthlyLoadsByFieldAndMonth(
        IReadOnlyList<Sprayfield> fields,
        IReadOnlyList<int> monthKeys,
        IReadOnlyDictionary<int, Dictionary<Guid, decimal>> monthlyVolumesByFieldByMonth,
        IReadOnlyDictionary<(int Year, int Month), NdmlrPanChemistryInputs> chemistryByMonth,
        decimal mineralizationRate,
        decimal volatilizationRate)
    {
        var result = new Dictionary<Guid, Dictionary<int, decimal>>();

        foreach (var field in fields)
        {
            var area = SprayfieldReportHelper.GetReportAcres(field);
            var loadsByMonth = new Dictionary<int, decimal>();

            foreach (var monthKey in monthKeys)
            {
                var (year, month) = ParseMonthKey(monthKey);
                var panMgL = TryGetMonthlyAveragePanMgL(year, month, chemistryByMonth, mineralizationRate, volatilizationRate);
                if (!panMgL.HasValue)
                {
                    continue;
                }

                monthlyVolumesByFieldByMonth.TryGetValue(monthKey, out var monthVolumes);
                var volume = 0m;
                if (monthVolumes != null && monthVolumes.TryGetValue(field.Id, out var mappedVolume))
                {
                    volume = mappedVolume;
                }

                var monthlyLoad = ComputeMonthlyLoadLbsPerAcre(volume, panMgL.Value, area);
                if (monthlyLoad.HasValue)
                {
                    loadsByMonth[monthKey] = monthlyLoad.Value;
                }
            }

            result[field.Id] = loadsByMonth;
        }

        return result;
    }

    public static bool TryGetCumulativeLoadForDisplay(
        Guid fieldId,
        int monthKey,
        IReadOnlyDictionary<Guid, Dictionary<int, decimal>> cumulativeByField,
        out decimal cumulativeLoad)
    {
        cumulativeLoad = 0m;
        return cumulativeByField.TryGetValue(fieldId, out var fieldCumulative) &&
               fieldCumulative.TryGetValue(monthKey, out cumulativeLoad) &&
               cumulativeLoad > 0m;
    }

    public static int BuildMonthKey(int year, int month) => (year * 100) + month;

    public static (int Year, int Month) ParseMonthKey(int monthKey) => (monthKey / 100, monthKey % 100);
}

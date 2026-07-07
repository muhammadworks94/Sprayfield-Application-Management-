using Microsoft.AspNetCore.Http;
using SAM.ViewModels.OperationalData;

namespace SAM.Utilities;

public static class ReportingDetectionLimitHelper
{
    public const string TooltipText =
        "Reporting Detection Limit: Testing results at a lower concentration than the laboratory can reliably quantify during routine analysis.";

    private const string FecalColiformPcsCode = "31616";

    public static string FormatDisplayValue(decimal value, bool isRdl, string format)
    {
        var formatted = value.ToString(format);
        return isRdl ? $"<{formatted}" : formatted;
    }

    public static decimal GetCalculationValue(decimal value, bool isRdl) =>
        isRdl ? 0m : value;

    public static decimal? AverageForReporting(
        IReadOnlyList<decimal?> values,
        IReadOnlyList<bool>? rdlFlags,
        string pcsCode)
    {
        var calculationValues = new List<decimal>();
        for (var i = 0; i < values.Count; i++)
        {
            if (!values[i].HasValue)
            {
                continue;
            }

            var isRdl = rdlFlags != null && i < rdlFlags.Count && rdlFlags[i];
            calculationValues.Add(GetCalculationValue(values[i]!.Value, isRdl));
        }

        if (calculationValues.Count == 0)
        {
            return null;
        }

        if (string.Equals(pcsCode, FecalColiformPcsCode, StringComparison.OrdinalIgnoreCase) &&
            calculationValues.All(v => v > 0m))
        {
            return (decimal)Math.Exp(calculationValues.Select(v => Math.Log((double)v)).Average());
        }

        return calculationValues.Average();
    }

    public static bool IsRdlAtIndex(IReadOnlyList<bool>? rdlFlags, int index) =>
        rdlFlags != null && index >= 0 && index < rdlFlags.Count && rdlFlags[index];

    public static bool IsFormCheckboxChecked(IFormCollection form, string key)
    {
        if (!form.TryGetValue(key, out var values))
        {
            return false;
        }

        return values.Any(v =>
            string.Equals(v, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(v, "on", StringComparison.OrdinalIgnoreCase));
    }

    public static void ApplyGwReportingDetectionLimitsFromForm(
        IFormCollection form,
        IList<GWMonitTemplateParameterViewModel> rows)
    {
        for (var i = 0; ; i++)
        {
            var idKey = $"TemplateParameters[{i}].FacilityPermitTemplateParameterId";
            if (!form.TryGetValue(idKey, out var idValues) ||
                !Guid.TryParse(idValues.FirstOrDefault(), out var templateId))
            {
                break;
            }

            var row = rows.FirstOrDefault(r => r.FacilityPermitTemplateParameterId == templateId);
            if (row == null)
            {
                continue;
            }

            row.IsReportingDetectionLimit = IsFormCheckboxChecked(form, $"TemplateParameters[{i}].IsReportingDetectionLimit");
        }
    }

    public static void ApplyWwReportingDetectionLimitsFromForm(
        IFormCollection form,
        IList<WWCharTemplateParameterInputViewModel> parameters)
    {
        for (var p = 0; ; p++)
        {
            var idKey = $"TemplateParameters[{p}].FacilityPermitTemplateParameterId";
            if (!form.TryGetValue(idKey, out var idValues) ||
                !Guid.TryParse(idValues.FirstOrDefault(), out var templateId))
            {
                break;
            }

            var parameter = parameters.FirstOrDefault(x => x.FacilityPermitTemplateParameterId == templateId)
                ?? (p < parameters.Count ? parameters[p] : null);
            if (parameter == null)
            {
                continue;
            }

            while (parameter.DailyIsReportingDetectionLimit.Count < 31)
            {
                parameter.DailyIsReportingDetectionLimit.Add(false);
            }

            for (var d = 0; d < 31; d++)
            {
                parameter.DailyIsReportingDetectionLimit[d] = IsFormCheckboxChecked(
                    form,
                    $"TemplateParameters[{p}].DailyIsReportingDetectionLimit[{d}]");
            }
        }
    }
}

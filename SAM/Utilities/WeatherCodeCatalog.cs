namespace SAM.Utilities;

public static class WeatherCodeCatalog
{
    public static readonly string[] AllowedCodes = ["C", "CL", "PC", "R", "SL", "SN"];

    public static bool TryNormalizeAbbreviation(string? rawCode, out string? normalized)
    {
        normalized = null;
        if (string.IsNullOrWhiteSpace(rawCode))
        {
            return true;
        }

        var token = rawCode.Trim().ToUpperInvariant();
        token = token.Replace("-", string.Empty).Replace(" ", string.Empty);

        normalized = token switch
        {
            "C" => "C",
            "CL" => "CL",
            "PC" => "PC",
            "R" => "R",
            "SL" => "SL",
            "SN" => "SN",
            "CLEAR" => "C",
            "CLOUDY" => "CL",
            "PARTLYCLOUDY" => "PC",
            "RAIN" => "R",
            "SLEET" => "SL",
            "SNOW" => "SN",
            "S" => "C",
            _ => null
        };

        return normalized != null;
    }
}

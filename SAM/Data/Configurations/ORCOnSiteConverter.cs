using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SAM.Domain.Enums;
using System.Text.Json;

namespace SAM.Data.Configurations;

public class ORCOnSiteConverter : ValueConverter<List<ORCOnSiteEnum?>, string>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public ORCOnSiteConverter() : base(
        v => ConvertToDatabase(v),
        v => ConvertFromDatabase(v))
    {
    }

    private static string ConvertToDatabase(List<ORCOnSiteEnum?> value)
    {
        if (value == null) return "[]";
        var stringList = value.Select(x => x.HasValue ? x.Value.ToString() : null).ToList();
        return JsonSerializer.Serialize(stringList, JsonOptions);
    }

    private static List<ORCOnSiteEnum?> ConvertFromDatabase(string value)
    {
        if (string.IsNullOrEmpty(value)) return new List<ORCOnSiteEnum?>();
        
        var deserialized = JsonSerializer.Deserialize<List<string?>>(value, JsonOptions);
        if (deserialized == null) return new List<ORCOnSiteEnum?>();
        
        var result = new List<ORCOnSiteEnum?>();
        foreach (var item in deserialized)
        {
            if (!string.IsNullOrEmpty(item) && Enum.TryParse<ORCOnSiteEnum>(item, out var enumValue))
            {
                result.Add(enumValue);
            }
            else
            {
                result.Add(null);
            }
        }
        return result;
    }
}



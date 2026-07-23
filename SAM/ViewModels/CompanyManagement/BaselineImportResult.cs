namespace SAM.ViewModels.CompanyManagement;

public class BaselineImportResult
{
    public int ImportedCellCount { get; set; }

    public int ClearedCellCount { get; set; }

    public int SkippedOperationalMonthCount { get; set; }

    public int SkippedOutOfWindowCount { get; set; }

    public List<string> Errors { get; set; } = new();

    public bool HasErrors => Errors.Count > 0;

    public string SummaryMessage
    {
        get
        {
            var parts = new List<string>
            {
                $"Imported {ImportedCellCount} value(s)",
                $"cleared {ClearedCellCount}",
                $"skipped {SkippedOperationalMonthCount} operational month(s)",
                $"skipped {SkippedOutOfWindowCount} out-of-window cell(s)"
            };

            if (HasErrors)
            {
                parts.Add($"{Errors.Count} error(s)");
            }

            return string.Join("; ", parts) + ".";
        }
    }
}

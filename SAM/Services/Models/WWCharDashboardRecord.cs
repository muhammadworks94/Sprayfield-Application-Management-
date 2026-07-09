namespace SAM.Services.Models;

public class WWCharDashboardRecord
{
    public int Month { get; set; }

    public int Year { get; set; }

    public List<decimal?>? BOD5Daily { get; set; }

    public List<decimal?>? TSSDaily { get; set; }

    public DateTime CreatedDate { get; set; }
}

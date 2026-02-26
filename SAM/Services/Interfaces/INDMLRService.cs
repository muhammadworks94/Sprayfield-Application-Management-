namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for generating Non-Discharge Mass Loading Report (NDMLR)
/// Excel outputs based on NDAR-1 and groundwater monitoring data.
/// </summary>
public interface INDMLRService
{
    /// <summary>
    /// Exports an NDMLR Excel file for the specified NDAR-1 report.
    /// The NDAR-1 report provides the facility, month, year, and field data
    /// used to populate the NDMLR template (volume applied, concentration, loads).
    /// </summary>
    /// <param name="ndar1Id">The ID of the NDAR-1 report to base the NDMLR on.</param>
    /// <returns>Byte array containing the generated Excel workbook.</returns>
    Task<byte[]> ExportToExcelAsync(Guid ndar1Id);
}

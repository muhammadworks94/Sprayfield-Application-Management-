namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for generating Non-Discharge Mass Loading Report (NDMLR)
/// Excel outputs based on NDAR-1 and groundwater monitoring data.
/// </summary>
public interface INDMLRService
{
    /// <summary>
    /// Exports an annual NDMLR Excel file for the specified NDMLR record.
    /// Source NDAR-1 and groundwater data are aggregated over Jan-Dec.
    /// </summary>
    /// <param name="ndmlrId">The ID of the NDMLR annual record.</param>
    /// <returns>Byte array containing the generated Excel workbook.</returns>
    Task<byte[]> ExportToExcelAsync(Guid ndmlrId);
}

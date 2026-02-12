using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for generating Non-Discharge Monitoring Report (NDMR)
/// Excel outputs based on existing monitoring and irrigation data.
/// </summary>
public interface INDMRService
{
    /// <summary>
    /// Exports an NDMR Excel file for the specified NDAR-1 report.
    /// The NDAR-1 report provides the facility, month, and year context
    /// used to aggregate monitoring data into the NDMR template.
    /// </summary>
    /// <param name="ndar1Id">The ID of the NDAR-1 report to base the NDMR on.</param>
    /// <returns>Byte array containing the generated Excel workbook.</returns>
    Task<byte[]> ExportToExcelAsync(Guid ndar1Id);
}


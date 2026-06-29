using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for generating Non-Discharge Monitoring Report (NDMR)
/// Excel outputs based on existing monitoring and irrigation data.
/// </summary>
public interface INDMRService
{
    /// <summary>
    /// Exports an NDMR Excel file for the specified NDMR report.
    /// </summary>
    /// <param name="ndmrId">The ID of the NDMR (IrrRprt) report to export.</param>
    /// <returns>Byte array containing the generated Excel workbook.</returns>
    Task<byte[]> ExportToExcelAsync(Guid ndmrId);
}


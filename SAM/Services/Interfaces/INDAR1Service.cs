using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for NDAR1 (Non-Discharge Application Report) entity operations.
/// </summary>
public interface INDAR1Service
{
    Task<IEnumerable<NDAR1>> GetAllAsync(Guid? companyId = null, Guid? facilityId = null);
    Task<NDAR1?> GetByIdAsync(Guid id);
    Task<NDAR1> CreateAsync(NDAR1 ndar1);
    Task<NDAR1> UpdateAsync(NDAR1 ndar1);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<NDAR1?> GetByFacilityMonthYearAsync(Guid facilityId, int month, int year);
    Task<IEnumerable<NDAR1>> GetByFacilityIdAsync(Guid facilityId);
    Task<NDAR1> GenerateMonthlyReportAsync(Guid facilityId, int month, int year);
    Task<byte[]> ExportToExcelAsync(Guid id);
}


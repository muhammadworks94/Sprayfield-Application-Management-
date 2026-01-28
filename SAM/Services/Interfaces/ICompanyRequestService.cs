using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for CompanyRequest entity operations.
/// </summary>
public interface ICompanyRequestService
{
    Task<IEnumerable<CompanyRequest>> GetAllAsync();
    Task<CompanyRequest?> GetByIdAsync(Guid id);
    Task<CompanyRequest> CreateAsync(CompanyRequest companyRequest);
    Task<CompanyRequest> ApproveRequestAsync(Guid requestId, string approvedByEmail);
    Task<CompanyRequest> RejectRequestAsync(Guid requestId, string rejectedByEmail, string? reason = null);
    Task<IEnumerable<CompanyRequest>> GetPendingRequestsAsync();
    Task<bool> ExistsAsync(Guid id);
}


using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for AdminRequest entity operations.
/// </summary>
public interface IAdminRequestService
{
    Task<IEnumerable<AdminRequest>> GetAllAsync();
    Task<AdminRequest?> GetByIdAsync(Guid id);
    Task<AdminRequest> CreateAsync(AdminRequest adminRequest);
    Task<AdminRequest> UpdateAsync(AdminRequest adminRequest);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<AdminRequest>> GetPendingRequestsAsync();
    Task<AdminRequest> ApproveRequestAsync(Guid requestId, string approvedByEmail);
    Task<AdminRequest> RejectRequestAsync(Guid requestId, string rejectedByEmail, string? reason = null);
}


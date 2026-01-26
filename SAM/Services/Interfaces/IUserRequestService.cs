using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

/// <summary>
/// Service interface for UserRequest entity operations.
/// </summary>
public interface IUserRequestService
{
    Task<IEnumerable<UserRequest>> GetAllAsync(Guid? companyId = null);
    Task<UserRequest?> GetByIdAsync(Guid id);
    Task<UserRequest> CreateAsync(UserRequest userRequest);
    Task<UserRequest> UpdateAsync(UserRequest userRequest);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<IEnumerable<UserRequest>> GetPendingRequestsAsync(Guid? companyId = null);
    Task<UserRequest> ApproveRequestAsync(Guid requestId, string approvedByEmail);
    Task<UserRequest> RejectRequestAsync(Guid requestId, string rejectedByEmail, string? reason = null);
}



using SAM.ViewModels.Reports;

namespace SAM.Services.Interfaces;

public interface INDAR1RowEditService
{
    Task<NDAR1EditGridViewModel> BuildGridAsync(Guid ndar1Id, string? currentUserId = null);
    Task<NDAR1RowEditLockResult> BeginRowEditAsync(Guid ndar1Id, int dayNo, string userId, string userDisplayName);
    Task<NDAR1GridEditBeginResult> BeginGridEditAsync(Guid ndar1Id, string userId, string userDisplayName);
    Task<NDAR1RowEditResult> UpdateRowAsync(Guid ndar1Id, NDAR1DayRowUpdateRequest request, string userId);
    Task CancelRowEditAsync(Guid ndar1Id, int dayNo, Guid lockToken, string userId);
    Task ReleaseGridEditAsync(Guid ndar1Id, IEnumerable<Guid> lockTokens, string userId);
    Task<NDAR1GridFooterTotalsResult> GetGridFooterTotalsAsync(Guid ndar1Id);
}


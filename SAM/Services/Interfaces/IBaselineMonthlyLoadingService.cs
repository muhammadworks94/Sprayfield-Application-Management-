using SAM.ViewModels.CompanyManagement;

namespace SAM.Services.Interfaces;

public interface IBaselineMonthlyLoadingService
{
    Task<ClientSetupViewModel> GetSetupGridAsync(Guid facilityId, int throughYear, int throughMonth, CancellationToken cancellationToken = default);

    Task SaveSetupGridAsync(Guid facilityId, int throughYear, int throughMonth, IReadOnlyList<ClientSetupCellSaveRequest> cells, string userId, CancellationToken cancellationToken = default);

    Task RefreshNdarReportsForWindowAsync(Guid facilityId, int throughYear, int throughMonth, CancellationToken cancellationToken = default);
}

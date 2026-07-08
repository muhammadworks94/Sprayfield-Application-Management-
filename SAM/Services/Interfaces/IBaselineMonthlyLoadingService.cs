using SAM.ViewModels.CompanyManagement;

namespace SAM.Services.Interfaces;

public interface IBaselineMonthlyLoadingService
{
    Task<NewClientSetupBaselineViewModel> GetWizardGridAsync(Guid companyId, int throughYear, int throughMonth, CancellationToken cancellationToken = default);

    Task SaveSetupCellAsync(Guid facilityId, Guid sprayfieldId, int throughYear, int throughMonth, int year, int month, decimal? loadingInches, string userId, CancellationToken cancellationToken = default);

    Task<int> CountSavedBaselineCellsAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task RefreshNdarReportsForWindowAsync(Guid facilityId, int throughYear, int throughMonth, CancellationToken cancellationToken = default);

    Task RefreshNdarReportsForCompanyWindowAsync(Guid companyId, int throughYear, int throughMonth, CancellationToken cancellationToken = default);
}

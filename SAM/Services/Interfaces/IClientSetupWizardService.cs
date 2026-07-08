using SAM.ViewModels.CompanyManagement;

namespace SAM.Services.Interfaces;

public interface IClientSetupWizardService
{
    Task<Guid> ProvisionCompanyAsync(NewClientSetupPage1ViewModel model, string userId, CancellationToken cancellationToken = default);
}

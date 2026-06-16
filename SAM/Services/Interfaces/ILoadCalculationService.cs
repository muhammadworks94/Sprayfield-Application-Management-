using SAM.Domain.Entities;

namespace SAM.Services.Interfaces;

public interface ILoadCalculationService
{
    Task<LoadCalculation> RecalculateForApplicationAsync(Guid applicationId);
}

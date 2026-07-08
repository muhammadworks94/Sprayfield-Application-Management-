using Microsoft.EntityFrameworkCore;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Infrastructure.Exceptions;
using SAM.Services.Interfaces;
using SAM.ViewModels.CompanyManagement;

namespace SAM.Services.Implementations;

public class ClientSetupWizardService : IClientSetupWizardService
{
    private readonly ApplicationDbContext _context;

    public ClientSetupWizardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> ProvisionCompanyAsync(
        NewClientSetupPage1ViewModel model,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var companyName = model.CompanyName.Trim();
        if (string.IsNullOrWhiteSpace(companyName))
        {
            throw new BusinessRuleException("Company name is required.");
        }

        var nameExists = await _context.Companies
            .AnyAsync(c => c.Name.ToLower() == companyName.ToLower(), cancellationToken);

        if (nameExists)
        {
            throw new BusinessRuleException($"A company with the name '{companyName}' already exists.");
        }

        if (model.Facilities.Count == 0)
        {
            throw new BusinessRuleException("At least one facility is required.");
        }

        if (model.Facilities.Count > 10)
        {
            throw new BusinessRuleException("A maximum of 10 facilities is allowed.");
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var companyId = ProvisionCompanyEntities(model, companyName, userId, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return companyId;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private Guid ProvisionCompanyEntities(
        NewClientSetupPage1ViewModel model,
        string companyName,
        string userId,
        CancellationToken cancellationToken)
    {
        var companyId = Guid.NewGuid();
        var company = new Company
        {
            Id = companyId,
            Name = companyName,
            ContactEmail = $"pending+{companyId:N}@setup.local",
            PhoneNumber = "000-000-0000",
            IsActive = true,
            IsVerified = false,
            FirstReportingMonth = model.FirstReportingMonth,
            FirstReportingYear = model.FirstReportingYear,
            CreatedBy = userId
        };

        _context.Companies.Add(company);

        var soil = new Soil
        {
            CompanyId = companyId,
            TypeName = "TBD Soil",
            Description = "Placeholder soil type — update during company setup.",
            Permeability = 0m,
            CreatedBy = userId
        };

        var nozzle = new Nozzle
        {
            CompanyId = companyId,
            Model = "TBD Nozzle",
            Manufacturer = "TBD",
            FlowRateGpm = 0m,
            SprayArc = 360,
            CreatedBy = userId
        };

        _context.Soils.Add(soil);
        _context.Nozzles.Add(nozzle);

        var usedFieldIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var facilityIndex = 0; facilityIndex < model.Facilities.Count; facilityIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var facilityModel = model.Facilities[facilityIndex];
            var facilityName = string.IsNullOrWhiteSpace(facilityModel.Name)
                ? $"Facility {facilityIndex + 1}"
                : facilityModel.Name.Trim();

            var facility = new Facility
            {
                CompanyId = companyId,
                Name = facilityName,
                Permittee = companyName,
                CreatedBy = userId
            };

            _context.Facilities.Add(facility);

            var sprayfieldCount = Math.Clamp(facilityModel.SprayfieldCount, 1, 150);
            for (var sprayfieldIndex = 0; sprayfieldIndex < sprayfieldCount; sprayfieldIndex++)
            {
                var fieldCode = sprayfieldIndex < facilityModel.SprayfieldFieldCodes.Count
                    ? facilityModel.SprayfieldFieldCodes[sprayfieldIndex]?.Trim()
                    : null;

                if (string.IsNullOrWhiteSpace(fieldCode))
                {
                    fieldCode = (sprayfieldIndex + 1).ToString();
                }

                var fieldId = ResolveUniqueFieldId(fieldCode, facilityIndex, sprayfieldIndex, model.Facilities.Count, usedFieldIds);

                _context.Sprayfields.Add(new Sprayfield
                {
                    CompanyId = companyId,
                    FacilityId = facility.Id,
                    FieldId = fieldId,
                    SizeAcres = 0m,
                    HydraulicLoadingLimitInPerYr = 0m,
                    Active = true,
                    SoilId = soil.Id,
                    NozzleId = nozzle.Id,
                    CreatedBy = userId
                });
            }
        }

        return companyId;
    }

    private static string ResolveUniqueFieldId(
        string fieldCode,
        int facilityIndex,
        int sprayfieldIndex,
        int facilityCount,
        HashSet<string> usedFieldIds)
    {
        var candidate = facilityCount > 1 ? $"{facilityIndex + 1}-{fieldCode}" : fieldCode;
        var suffix = 1;

        while (usedFieldIds.Contains(candidate))
        {
            candidate = facilityCount > 1
                ? $"{facilityIndex + 1}-{fieldCode}-{suffix}"
                : $"{fieldCode}-{suffix}";
            suffix++;
        }

        usedFieldIds.Add(candidate);
        return candidate;
    }
}

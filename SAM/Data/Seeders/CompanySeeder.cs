using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;

namespace SAM.Data.Seeders;

/// <summary>
/// Seeds company and reference data for development/demo purposes.
/// </summary>
public static class CompanySeeder
{
    /// <summary>
    /// Seeds companies with their reference data (facilities, soils, crops, nozzles, sprayfields, monitoring wells).
    /// </summary>
    public static async Task SeedCompaniesAsync(
        ApplicationDbContext context,
        ILogger logger)
    {
        if (await context.Companies.AnyAsync())
        {
            logger.LogDebug("Companies already exist. Skipping company seeding.");
            return;
        }

        logger.LogInformation("Seeding companies and reference data...");

        // Company 1
        var company1 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Acme Environmental Services",
            ContactEmail = "contact@acme-env.com",
            PhoneNumber = "+1-555-0101",
            Website = "https://www.acme-env.com",
            Description = "Leading environmental services provider specializing in wastewater treatment and sprayfield management.",
            TaxId = "12-3456789",
            FaxNumber = "+1-555-0102",
            LicenseNumber = "ENV-LIC-2024-001",
            IsActive = true,
            IsVerified = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Soils
        var soil1 = new Soil
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            TypeName = "Cecil Sandy Loam",
            Description = "Well-drained sandy loam soil",
            Permeability = 2.5m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        var soil2 = new Soil
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            TypeName = "Davidson Clay Loam",
            Description = "Moderately well-drained clay loam",
            Permeability = 1.2m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Crops
        var crop1 = new Crop
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            Name = "Fescue",
            PanFactor = 0.75m,
            NUptake = 150m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        var crop2 = new Crop
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            Name = "Bermuda Grass",
            PanFactor = 0.80m,
            NUptake = 180m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Nozzles
        var nozzle1 = new Nozzle
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            Model = "SR-100",
            Manufacturer = "Rain Bird",
            FlowRateGpm = 10.5m,
            SprayArc = 360,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        var nozzle2 = new Nozzle
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            Model = "SR-200",
            Manufacturer = "Rain Bird",
            FlowRateGpm = 15.0m,
            SprayArc = 180,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Facilities
        var facility1 = new Facility
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            Name = "North Treatment Facility",
            PermitNumber = "NPDES-001",
            Permittee = "Acme Environmental Services",
            FacilityClass = "Class A",
            Address = "123 Industrial Blvd",
            City = "Raleigh",
            State = "NC",
            ZipCode = "27601",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Monitoring Wells
        var well1 = new MonitoringWell
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            WellId = "MW-001",
            LocationDescription = "Northwest corner of facility",
            Latitude = 35.7796m,
            Longitude = -78.6382m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        var well2 = new MonitoringWell
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            WellId = "MW-002",
            LocationDescription = "Southeast corner of facility",
            Latitude = 35.7780m,
            Longitude = -78.6360m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Sprayfields
        var sprayfield1 = new Sprayfield
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            FacilityId = facility1.Id,
            FieldId = "SF-001",
            SizeAcres = 25.5m,
            SoilId = soil1.Id,
            CropId = crop1.Id,
            NozzleId = nozzle1.Id,
            HydraulicLoadingLimitInPerYr = 48.0m,
            WeeklyRateInches = 1.0m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        var sprayfield2 = new Sprayfield
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            FacilityId = facility1.Id,
            FieldId = "SF-002",
            SizeAcres = 30.0m,
            SoilId = soil2.Id,
            CropId = crop2.Id,
            NozzleId = nozzle2.Id,
            HydraulicLoadingLimitInPerYr = 45.0m,
            WeeklyRateInches = 0.9m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2
        var company2 = new Company
        {
            Id = Guid.NewGuid(),
            Name = "Green Valley Waste Management",
            ContactEmail = "info@greenvalley-wm.com",
            PhoneNumber = "+1-555-0201",
            Website = "https://www.greenvalley-wm.com",
            Description = "Comprehensive waste management solutions with focus on sustainable practices and regulatory compliance.",
            TaxId = "98-7654321",
            FaxNumber = "+1-555-0202",
            LicenseNumber = "WM-LIC-2024-002",
            IsActive = true,
            IsVerified = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2 - Soils
        var soil3 = new Soil
        {
            Id = Guid.NewGuid(),
            CompanyId = company2.Id,
            TypeName = "Norfolk Sandy Loam",
            Description = "Well-drained sandy loam",
            Permeability = 2.0m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2 - Crops
        var crop3 = new Crop
        {
            Id = Guid.NewGuid(),
            CompanyId = company2.Id,
            Name = "Ryegrass",
            PanFactor = 0.70m,
            NUptake = 140m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2 - Nozzles
        var nozzle3 = new Nozzle
        {
            Id = Guid.NewGuid(),
            CompanyId = company2.Id,
            Model = "SR-150",
            Manufacturer = "Hunter",
            FlowRateGpm = 12.0m,
            SprayArc = 270,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2 - Facilities
        var facility2 = new Facility
        {
            Id = Guid.NewGuid(),
            CompanyId = company2.Id,
            Name = "South Treatment Facility",
            PermitNumber = "NPDES-002",
            Permittee = "Green Valley Waste Management",
            FacilityClass = "Class B",
            Address = "456 Environmental Way",
            City = "Charlotte",
            State = "NC",
            ZipCode = "28202",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2 - Monitoring Wells
        var well3 = new MonitoringWell
        {
            Id = Guid.NewGuid(),
            CompanyId = company2.Id,
            WellId = "MW-101",
            LocationDescription = "Northeast corner",
            Latitude = 35.2271m,
            Longitude = -80.8431m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 2 - Sprayfields
        var sprayfield3 = new Sprayfield
        {
            Id = Guid.NewGuid(),
            CompanyId = company2.Id,
            FacilityId = facility2.Id,
            FieldId = "SF-101",
            SizeAcres = 20.0m,
            SoilId = soil3.Id,
            CropId = crop3.Id,
            NozzleId = nozzle3.Id,
            HydraulicLoadingLimitInPerYr = 50.0m,
            WeeklyRateInches = 1.1m,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Add all entities to context
        context.Companies.AddRange(company1, company2);
        context.Soils.AddRange(soil1, soil2, soil3);
        context.Crops.AddRange(crop1, crop2, crop3);
        context.Nozzles.AddRange(nozzle1, nozzle2, nozzle3);
        context.Facilities.AddRange(facility1, facility2);
        context.MonitoringWells.AddRange(well1, well2, well3);
        context.Sprayfields.AddRange(sprayfield1, sprayfield2, sprayfield3);

        await context.SaveChangesAsync();

        logger.LogInformation("Successfully seeded 2 companies with reference data.");
    }
}



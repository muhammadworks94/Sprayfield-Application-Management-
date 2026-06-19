using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SAM.Data;
using SAM.Domain.Entities;
using SAM.Domain.Enums;

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
            Permittee = "Acme Environmental Services",
            FacilityClass = "Class A",
            FacilityContactPerson = "Steven Dodds",
            FacilityContactPersonPhone = "919-867-5309",
            OrcName = "Steven Dodds",
            OperatorPhone = "919-867-5309",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };

        // Company 1 - Monitoring Wells
        var well1 = new MonitoringWell
        {
            Id = Guid.NewGuid(),
            CompanyId = company1.Id,
            WellId = "1-Upgradient",
            WellPermitNumber = "WQ0004332",
            LocationDescription = "614 Macedonia Rd.",
            DiameterInches = 2m,
            WellDepthFeet = 30m,
            DepthToScreenFeet = null,
            LowScreenDepthFeet = 15m,
            HighScreenDepthFeet = null,
            TopOfCasingElevationMsl = 2m,
            TreatmentSystemLocation = TreatmentSystemLocationEnum.Influent,
            NumberOfWellsToBeSampled = 6,
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
            WellPermitNumber = "WQ0004333",
            LocationDescription = "Southeast corner of facility",
            DiameterInches = 2m,
            WellDepthFeet = 28m,
            DepthToScreenFeet = 12m,
            LowScreenDepthFeet = 14m,
            HighScreenDepthFeet = 18m,
            TopOfCasingElevationMsl = 3m,
            TreatmentSystemLocation = TreatmentSystemLocationEnum.Effluent,
            NumberOfWellsToBeSampled = 6,
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
            NozzleId = nozzle1.Id,
            CropId = crop1.Id,
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
            NozzleId = nozzle2.Id,
            CropId = crop2.Id,
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
            Permittee = "Green Valley Waste Management",
            FacilityClass = "Class B",
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
            WellPermitNumber = "WQ0004400",
            LocationDescription = "Northeast corner",
            DiameterInches = 2m,
            WellDepthFeet = 25m,
            DepthToScreenFeet = 10m,
            LowScreenDepthFeet = 12m,
            HighScreenDepthFeet = 16m,
            TopOfCasingElevationMsl = 2.5m,
            TreatmentSystemLocation = TreatmentSystemLocationEnum.Influent,
            NumberOfWellsToBeSampled = 4,
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
            NozzleId = nozzle3.Id,
            CropId = crop3.Id,
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

        await SeedAcmeGw59DemoAsync(context, company1, facility1, well1, logger);

        logger.LogInformation("Successfully seeded 2 companies with reference data.");
    }

    private static async Task SeedAcmeGw59DemoAsync(
        ApplicationDbContext context,
        Company company,
        Facility facility,
        MonitoringWell monitoringWell,
        ILogger logger)
    {
        var pcsRows = new (string Code, string Friendly, string Official, string Units, decimal DemoValue)[]
        {
            ("00400", "pH", "pH", "su", 7.10m),
            ("00680", "Total Organic Carbon", "Carbon, Tot Organic (TOC)", "mg/L", 2.50m),
            ("00940", "Chloride", "Chloride (as Cl)", "mg/L", 12.00m),
            ("70300", "Total Dissolved Solids", "Solids, Total Dissolved- 180 Deg.C", "mg/L", 350.00m),
            ("00552", "Grease and Oils", "Grease and Oils", "mg/L", 1.25m),
            ("00095", "Specific Conductance", "Specific Conductance", "uMhos/cm", 450.00m),
            ("70507", "Orthophosphate", "Orthophosphate (as PO4)", "mg/L", 0.08m),
            ("01105", "Al - Aluminum", "Aluminum, Total (as Al)", "mg/L", 0.05m)
        };

        var catalogByCode = new Dictionary<string, PcsParameterCatalog>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in pcsRows)
        {
            var catalog = new PcsParameterCatalog
            {
                Id = Guid.NewGuid(),
                PcsCode = row.Code,
                UserFriendlyName = row.Friendly,
                OfficialParameterName = row.Official,
                AcceptedUnits = row.Units,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "system",
                IsDeleted = false
            };
            context.PcsParameterCatalogs.Add(catalog);
            catalogByCode[row.Code] = catalog;
        }

        var labOption = new CompanyLabOption
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            Name = "Demo Certified Laboratory",
            CertificationNumber = "NC-DEMO-001",
            SortOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };
        context.CompanyLabOptions.Add(labOption);

        var permit = new FacilityPermit
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            FacilityId = facility.Id,
            PermitNumber = "NPDES-001",
            PermitVersion = "1.0",
            EffectiveStartDate = new DateTime(2020, 1, 1),
            EffectiveEndDate = new DateTime(2026, 7, 31),
            IsActive = true,
            GwOperationLagoon = true,
            GwOperationSprayField = true,
            Address = "123 Industrial Blvd",
            City = "Raleigh",
            State = "NC",
            ZipCode = "27601",
            County = "Wake",
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };
        context.FacilityPermits.Add(permit);
        facility.DefaultFacilityPermitId = permit.Id;

        var sortOrder = 1;
        var templateParameters = new List<FacilityPermitTemplateParameter>();
        foreach (var row in pcsRows)
        {
            var templateParameter = new FacilityPermitTemplateParameter
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                FacilityPermitId = permit.Id,
                PcsParameterCatalogId = catalogByCode[row.Code].Id,
                SampleType = SampleTypeEnum.Grab,
                MeasurementFrequency = MeasurementFrequencyEnum.Annual,
                SortOrder = sortOrder++,
                IsRequired = false,
                ReportTypes = PermitTemplateReportTypeEnum.Gw59,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "system",
                IsDeleted = false
            };
            templateParameters.Add(templateParameter);
            context.FacilityPermitTemplateParameters.Add(templateParameter);
        }

        var gwMonit = new GWMonit
        {
            Id = Guid.NewGuid(),
            CompanyId = company.Id,
            FacilityId = facility.Id,
            MonitoringWellId = monitoringWell.Id,
            SampleDate = new DateTime(2026, 6, 16),
            LabSampleAnalyzedDate = new DateTime(2026, 6, 18),
            WaterLevel = 12.5m,
            PH = 7.2m,
            Temperature = 18.5m,
            Conductivity = 450m,
            GallonsPumped = 3m,
            Odor = "None",
            Appearance = "Clear",
            MetalsSamplesCollectedUnfiltered = true,
            MetalSamplesFieldAcidified = false,
            VOCReportAttached = true,
            CollectedBy = "Steven Dodds",
            AnalyzedBy = string.Empty,
            LabOptionId = labOption.Id,
            GW59ADueDate = new DateTime(2026, 7, 15),
            GW59AQuestion1Response = true,
            GW59AQuestion2Response = false,
            GW59AQuestion2Details = "No exceedances during calibration test period.",
            GW59AQuestion3Response = true,
            GW59AQuestion4Response = false,
            GW59AQuestion5Response = true,
            GW59AQuestion5Details = "All wells sampled per permit schedule.",
            GW59AQuestion6Response = false,
            GW59AQuestion7Response = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "system",
            IsDeleted = false
        };
        context.GWMonits.Add(gwMonit);

        foreach (var row in pcsRows)
        {
            var templateParameter = templateParameters.First(x =>
                x.PcsParameterCatalogId == catalogByCode[row.Code].Id);
            context.GWMonitTemplateValues.Add(new GWMonitTemplateValue
            {
                Id = Guid.NewGuid(),
                CompanyId = company.Id,
                GWMonitId = gwMonit.Id,
                FacilityPermitTemplateParameterId = templateParameter.Id,
                NumericValue = row.DemoValue,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "system",
                IsDeleted = false
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Seeded Acme GW-59 demo permit, parameters, and monitoring record {GwMonitId}.", gwMonit.Id);
    }
}

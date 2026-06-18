using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class BackfillGw59MissingPcsParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @MigrationUser nvarchar(64) = N'migration-gw59-pcs';
DECLARE @Gw59ReportType int = 4;

DECLARE @PcsSeed TABLE (
    PcsCode nvarchar(32) NOT NULL,
    UserFriendlyName nvarchar(255) NOT NULL,
    OfficialParameterName nvarchar(500) NOT NULL,
    AcceptedUnits nvarchar(100) NOT NULL,
    DemoValue decimal(18, 6) NOT NULL
);

INSERT INTO @PcsSeed (PcsCode, UserFriendlyName, OfficialParameterName, AcceptedUnits, DemoValue)
VALUES
    (N'00552', N'Grease and Oils', N'Grease and Oils', N'mg/L', 1.25),
    (N'00095', N'Specific Conductance', N'Specific Conductance', N'uMhos/cm', 450),
    (N'70507', N'Orthophosphate', N'Orthophosphate (as PO4)', N'mg/L', 0.08),
    (N'01105', N'Al - Aluminum', N'Aluminum, Total (as Al)', N'mg/L', 0.05);

INSERT INTO PcsParameterCatalogs (
    Id, PcsCode, UserFriendlyName, OfficialParameterName, AcceptedUnits, IsActive,
    CreatedDate, UpdatedDate, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    s.PcsCode,
    s.UserFriendlyName,
    s.OfficialParameterName,
    s.AcceptedUnits,
    1,
    SYSUTCDATETIME(),
    NULL,
    @MigrationUser,
    0
FROM @PcsSeed s
WHERE NOT EXISTS (
    SELECT 1 FROM PcsParameterCatalogs pc WHERE pc.PcsCode = s.PcsCode
);

UPDATE pc
SET
    pc.UserFriendlyName = s.UserFriendlyName,
    pc.OfficialParameterName = s.OfficialParameterName,
    pc.AcceptedUnits = s.AcceptedUnits,
    pc.IsActive = 1,
    pc.UpdatedDate = SYSUTCDATETIME()
FROM PcsParameterCatalogs pc
INNER JOIN @PcsSeed s ON s.PcsCode = pc.PcsCode;

INSERT INTO FacilityPermitTemplateParameters (
    Id, CompanyId, FacilityPermitId, PcsParameterCatalogId,
    ParameterDisplayOverride, UnitsOverride,
    MonthlyAverageLimit, MonthlyGeometricMeanLimit, DailyMinimumLimit, DailyMaximumLimit,
    SampleType, MeasurementFrequency, ScheduledMonthsCsv, Notes,
    SortOrder, IsRequired, ReportTypes,
    CreatedDate, UpdatedDate, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    fp.CompanyId,
    fp.Id,
    pc.Id,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    NULL,
    1,
    6,
    NULL,
    NULL,
    COALESCE((
        SELECT MAX(tp.SortOrder)
        FROM FacilityPermitTemplateParameters tp
        WHERE tp.FacilityPermitId = fp.Id AND (tp.ReportTypes & @Gw59ReportType) <> 0
    ), 0) + ROW_NUMBER() OVER (PARTITION BY fp.Id ORDER BY s.PcsCode),
    0,
    @Gw59ReportType,
    SYSUTCDATETIME(),
    NULL,
    @MigrationUser,
    0
FROM FacilityPermits fp
INNER JOIN Companies c ON c.Id = fp.CompanyId AND c.Name = N'Acme Environmental Services'
INNER JOIN @PcsSeed s ON 1 = 1
INNER JOIN PcsParameterCatalogs pc ON pc.PcsCode = s.PcsCode
WHERE EXISTS (
    SELECT 1
    FROM FacilityPermitTemplateParameters existing
    WHERE existing.FacilityPermitId = fp.Id
      AND (existing.ReportTypes & @Gw59ReportType) <> 0
)
AND NOT EXISTS (
    SELECT 1
    FROM FacilityPermitTemplateParameters existing
    WHERE existing.FacilityPermitId = fp.Id
      AND existing.PcsParameterCatalogId = pc.Id
      AND existing.ReportTypes = @Gw59ReportType
);

INSERT INTO GWMonitTemplateValues (
    Id, CompanyId, GWMonitId, FacilityPermitTemplateParameterId,
    NumericValue, TextValue,
    CreatedDate, UpdatedDate, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    g.CompanyId,
    g.Id,
    tp.Id,
    s.DemoValue,
    NULL,
    SYSUTCDATETIME(),
    NULL,
    @MigrationUser,
    0
FROM GWMonits g
INNER JOIN Companies c ON c.Id = g.CompanyId AND c.Name = N'Acme Environmental Services'
INNER JOIN Facilities f ON f.Id = g.FacilityId
INNER JOIN FacilityPermits fp ON fp.FacilityId = f.Id AND fp.IsActive = 1
INNER JOIN FacilityPermitTemplateParameters tp ON tp.FacilityPermitId = fp.Id AND tp.ReportTypes = @Gw59ReportType
INNER JOIN PcsParameterCatalogs pc ON pc.Id = tp.PcsParameterCatalogId
INNER JOIN @PcsSeed s ON s.PcsCode = pc.PcsCode
WHERE NOT EXISTS (
    SELECT 1
    FROM GWMonitTemplateValues existing
    WHERE existing.GWMonitId = g.Id
      AND existing.FacilityPermitTemplateParameterId = tp.Id
);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DECLARE @MigrationUser nvarchar(64) = N'migration-gw59-pcs';

DELETE tv
FROM GWMonitTemplateValues tv
INNER JOIN FacilityPermitTemplateParameters tp ON tp.Id = tv.FacilityPermitTemplateParameterId
WHERE tp.CreatedBy = @MigrationUser;

DELETE FROM FacilityPermitTemplateParameters
WHERE CreatedBy = @MigrationUser;
");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class SeedAndBackfillZoneModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
UPDATE sf
SET
    PermitFieldName = COALESCE(sf.PermitFieldName, sf.FieldId),
    AcresTotal = COALESCE(sf.AcresTotal, sf.SizeAcres),
    Active = 1,
    PermitNumber = COALESCE(sf.PermitNumber, f.PermitNumber)
FROM Sprayfields sf
LEFT JOIN Facilities f ON f.Id = sf.FacilityId;");

            migrationBuilder.Sql(@"
INSERT INTO ApplicationZones (Id, SprayfieldId, ZoneName, PercentOfField, Acres, SoilId, NozzleId, CropId, Active, CreatedDate, UpdatedDate, CreatedBy, IsDeleted, CompanyId)
SELECT
    NEWID(),
    sf.Id,
    'Default',
    CAST(100.0000 AS decimal(9,4)),
    CAST(COALESCE(sf.AcresTotal, sf.SizeAcres) AS decimal(18,4)),
    sf.SoilId,
    sf.NozzleId,
    sf.CropId,
    1,
    SYSUTCDATETIME(),
    NULL,
    'migration',
    0,
    sf.CompanyId
FROM Sprayfields sf
WHERE NOT EXISTS (
    SELECT 1 FROM ApplicationZones z WHERE z.SprayfieldId = sf.Id
);");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1
    FROM ApplicationZones
    WHERE Active = 1
    GROUP BY SprayfieldId
    HAVING ABS(SUM(PercentOfField) - 100.0) > 0.01
)
BEGIN
    THROW 50001, 'Zone percentage validation failed. Active zones must total 100 +/- 0.01 per sprayfield.', 1;
END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM ApplicationZones WHERE ZoneName = 'Default' AND CreatedBy = 'migration';");
        }
    }
}

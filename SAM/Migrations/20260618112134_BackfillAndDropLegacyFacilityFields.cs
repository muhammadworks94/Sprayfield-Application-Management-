using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class BackfillAndDropLegacyFacilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- 1) Backfill empty permit location/operational fields from legacy facility columns.
UPDATE fp
SET
    fp.Address = COALESCE(NULLIF(LTRIM(RTRIM(fp.Address)), ''), NULLIF(LTRIM(RTRIM(f.Address)), '')),
    fp.City = COALESCE(NULLIF(LTRIM(RTRIM(fp.City)), ''), NULLIF(LTRIM(RTRIM(f.City)), '')),
    fp.State = COALESCE(NULLIF(LTRIM(RTRIM(fp.State)), ''), NULLIF(LTRIM(RTRIM(f.State)), '')),
    fp.ZipCode = COALESCE(NULLIF(LTRIM(RTRIM(fp.ZipCode)), ''), NULLIF(LTRIM(RTRIM(f.ZipCode)), '')),
    fp.County = COALESCE(NULLIF(LTRIM(RTRIM(fp.County)), ''), NULLIF(LTRIM(RTRIM(f.County)), '')),
    fp.TotalNumberOfSprayfields = COALESCE(fp.TotalNumberOfSprayfields, f.TotalNumberOfSprayfields),
    fp.PermittedMinimumFreeboardFeet = COALESCE(fp.PermittedMinimumFreeboardFeet, f.PermittedMinimumFreeboardFeet)
FROM FacilityPermits fp
INNER JOIN Facilities f ON f.Id = fp.FacilityId
WHERE f.IsDeleted = 0
  AND fp.IsDeleted = 0;

-- 2) Backfill permit number and expiration from legacy facility columns when missing on permit rows.
UPDATE fp
SET fp.PermitNumber = f.PermitNumber
FROM FacilityPermits fp
INNER JOIN Facilities f ON f.Id = fp.FacilityId
WHERE f.IsDeleted = 0
  AND fp.IsDeleted = 0
  AND NULLIF(LTRIM(RTRIM(f.PermitNumber)), '') IS NOT NULL
  AND (NULLIF(LTRIM(RTRIM(fp.PermitNumber)), '') IS NULL OR fp.PermitNumber = N'—');

UPDATE fp
SET fp.EffectiveEndDate = f.PermitExpirationDate
FROM FacilityPermits fp
INNER JOIN Facilities f ON f.Id = fp.FacilityId
WHERE f.IsDeleted = 0
  AND fp.IsDeleted = 0
  AND fp.EffectiveEndDate IS NULL
  AND f.PermitExpirationDate IS NOT NULL;

-- 3) Create a permit row for facilities that have legacy data but no permit versions yet.
INSERT INTO FacilityPermits (
    Id, FacilityId, CompanyId, PermitNumber, PermitVersion,
    EffectiveStartDate, EffectiveEndDate, IsActive,
    Address, City, State, ZipCode, County,
    TotalNumberOfSprayfields, PermittedMinimumFreeboardFeet,
    CreatedDate, UpdatedDate, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    f.Id,
    f.CompanyId,
    COALESCE(NULLIF(LTRIM(RTRIM(f.PermitNumber)), ''), CONCAT(N'FAC-', LEFT(CAST(f.Id AS nvarchar(36)), 8))),
    N'1',
    f.CreatedDate,
    f.PermitExpirationDate,
    1,
    NULLIF(LTRIM(RTRIM(f.Address)), ''),
    NULLIF(LTRIM(RTRIM(f.City)), ''),
    NULLIF(LTRIM(RTRIM(f.State)), ''),
    NULLIF(LTRIM(RTRIM(f.ZipCode)), ''),
    NULLIF(LTRIM(RTRIM(f.County)), ''),
    f.TotalNumberOfSprayfields,
    f.PermittedMinimumFreeboardFeet,
    SYSUTCDATETIME(),
    NULL,
    N'migration-legacy-facility-permit',
    0
FROM Facilities f
WHERE f.IsDeleted = 0
  AND NOT EXISTS (
      SELECT 1 FROM FacilityPermits fp
      WHERE fp.FacilityId = f.Id AND fp.IsDeleted = 0)
  AND (
      NULLIF(LTRIM(RTRIM(f.PermitNumber)), '') IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.Address)), '') IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.City)), '') IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.State)), '') IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.ZipCode)), '') IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.County)), '') IS NOT NULL
      OR f.PermitExpirationDate IS NOT NULL
      OR f.TotalNumberOfSprayfields IS NOT NULL
      OR f.PermittedMinimumFreeboardFeet IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.CertifiedLaboratory1Name)), '') IS NOT NULL
      OR NULLIF(LTRIM(RTRIM(f.CertifiedLaboratory2Name)), '') IS NOT NULL);

-- 4) Ensure each facility has a default permit when permits exist.
UPDATE f
SET f.DefaultFacilityPermitId = picked.PermitId
FROM Facilities f
CROSS APPLY (
    SELECT TOP (1) fp.Id AS PermitId
    FROM FacilityPermits fp
    WHERE fp.FacilityId = f.Id AND fp.IsDeleted = 0
    ORDER BY
        CASE WHEN f.DefaultFacilityPermitId = fp.Id THEN 0 ELSE 1 END,
        fp.IsActive DESC,
        fp.EffectiveStartDate DESC,
        fp.CreatedDate DESC
) picked
WHERE f.IsDeleted = 0
  AND f.DefaultFacilityPermitId IS NULL;

-- 5) Backfill company lab options from legacy facility lab columns (idempotent).
INSERT INTO CompanyLabOptions (
    Id, CompanyId, Name, CertificationNumber, SortOrder, IsActive,
    CreatedDate, UpdatedDate, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    f.CompanyId,
    f.CertifiedLaboratory1Name,
    ISNULL(f.LabCertificationNumber1, ''),
    1,
    1,
    SYSUTCDATETIME(),
    NULL,
    N'migration-lab-options',
    0
FROM Facilities f
WHERE f.IsDeleted = 0
  AND NULLIF(LTRIM(RTRIM(f.CertifiedLaboratory1Name)), '') IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM CompanyLabOptions c
      WHERE c.CompanyId = f.CompanyId
        AND c.Name = f.CertifiedLaboratory1Name
        AND c.IsDeleted = 0);

INSERT INTO CompanyLabOptions (
    Id, CompanyId, Name, CertificationNumber, SortOrder, IsActive,
    CreatedDate, UpdatedDate, CreatedBy, IsDeleted)
SELECT
    NEWID(),
    f.CompanyId,
    f.CertifiedLaboratory2Name,
    ISNULL(f.LabCertificationNumber2, ''),
    2,
    1,
    SYSUTCDATETIME(),
    NULL,
    N'migration-lab-options',
    0
FROM Facilities f
WHERE f.IsDeleted = 0
  AND NULLIF(LTRIM(RTRIM(f.CertifiedLaboratory2Name)), '') IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM CompanyLabOptions c
      WHERE c.CompanyId = f.CompanyId
        AND c.Name = f.CertifiedLaboratory2Name
        AND c.IsDeleted = 0);

UPDATE f
SET f.DefaultLabOptionId = c.Id
FROM Facilities f
INNER JOIN CompanyLabOptions c ON c.CompanyId = f.CompanyId AND c.SortOrder = 1 AND c.IsDeleted = 0
WHERE f.IsDeleted = 0
  AND f.DefaultLabOptionId IS NULL
  AND NULLIF(LTRIM(RTRIM(f.CertifiedLaboratory1Name)), '') IS NOT NULL;
");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_PermitNumber",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "CertifiedLaboratory1Name",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "CertifiedLaboratory2Name",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "LabCertificationNumber1",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "LabCertificationNumber2",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PermitExpirationDate",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PermitNumber",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PermittedMinimumFreeboardFeet",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "State",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "TotalNumberOfSprayfields",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "Facilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Facilities",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CertifiedLaboratory1Name",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CertifiedLaboratory2Name",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabCertificationNumber1",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabCertificationNumber2",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PermitExpirationDate",
                table: "Facilities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitNumber",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PermittedMinimumFreeboardFeet",
                table: "Facilities",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "Facilities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalNumberOfSprayfields",
                table: "Facilities",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "Facilities",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_PermitNumber",
                table: "Facilities",
                column: "PermitNumber");
        }
    }
}

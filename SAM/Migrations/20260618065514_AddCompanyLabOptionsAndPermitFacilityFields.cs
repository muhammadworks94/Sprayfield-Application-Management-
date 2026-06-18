using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyLabOptionsAndPermitFacilityFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "FacilityPermits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "FacilityPermits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "FacilityPermits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PermittedMinimumFreeboardFeet",
                table: "FacilityPermits",
                type: "decimal(8,2)",
                precision: 8,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "FacilityPermits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalNumberOfSprayfields",
                table: "FacilityPermits",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "FacilityPermits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultFacilityPermitId",
                table: "Facilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultLabOptionId",
                table: "Facilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyLabOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CertificationNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyLabOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyLabOptions_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_DefaultFacilityPermitId",
                table: "Facilities",
                column: "DefaultFacilityPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_DefaultLabOptionId",
                table: "Facilities",
                column: "DefaultLabOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLabOptions_CompanyId",
                table: "CompanyLabOptions",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_CompanyLabOptions_DefaultLabOptionId",
                table: "Facilities",
                column: "DefaultLabOptionId",
                principalTable: "CompanyLabOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_FacilityPermits_DefaultFacilityPermitId",
                table: "Facilities",
                column: "DefaultFacilityPermitId",
                principalTable: "FacilityPermits",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.Sql(@"
UPDATE fp
SET
    fp.Address = f.Address,
    fp.City = f.City,
    fp.State = f.State,
    fp.ZipCode = f.ZipCode,
    fp.County = f.County,
    fp.TotalNumberOfSprayfields = f.TotalNumberOfSprayfields,
    fp.PermittedMinimumFreeboardFeet = f.PermittedMinimumFreeboardFeet
FROM FacilityPermits fp
INNER JOIN Facilities f ON f.Id = fp.FacilityId;

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
WHERE f.DefaultLabOptionId IS NULL
  AND NULLIF(LTRIM(RTRIM(f.CertifiedLaboratory1Name)), '') IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_CompanyLabOptions_DefaultLabOptionId",
                table: "Facilities");

            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_FacilityPermits_DefaultFacilityPermitId",
                table: "Facilities");

            migrationBuilder.DropTable(
                name: "CompanyLabOptions");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_DefaultFacilityPermitId",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_DefaultLabOptionId",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "City",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "County",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "PermittedMinimumFreeboardFeet",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "State",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "TotalNumberOfSprayfields",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "DefaultFacilityPermitId",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "DefaultLabOptionId",
                table: "Facilities");
        }
    }
}

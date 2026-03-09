using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddZoneModelCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AcresTotal",
                table: "Sprayfields",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Sprayfields",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitFieldName",
                table: "Sprayfields",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitNumber",
                table: "Sprayfields",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InfiltrationRate",
                table: "Soils",
                type: "decimal(18,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PANFactor",
                table: "Soils",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Pressure",
                table: "Nozzles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PANLimit",
                table: "Crops",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ApplicationZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SprayfieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZoneName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PercentOfField = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    Acres = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    SoilId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NozzleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CropId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationZones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationZones_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationZones_Crops_CropId",
                        column: x => x.CropId,
                        principalTable: "Crops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationZones_Nozzles_NozzleId",
                        column: x => x.NozzleId,
                        principalTable: "Nozzles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationZones_Soils_SoilId",
                        column: x => x.SoilId,
                        principalTable: "Soils",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ApplicationZones_Sprayfields_SprayfieldId",
                        column: x => x.SprayfieldId,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ZoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VolumeGallons = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NitrogenMgL = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    OperatorUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OperatorSnapshotName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyApplications_ApplicationZones_ZoneId",
                        column: x => x.ZoneId,
                        principalTable: "ApplicationZones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyApplications_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyApplications_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyApplications_Users_OperatorUserId",
                        column: x => x.OperatorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoadCalculations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LbsApplied = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    LbsPerAcre = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    ZoneAcresSnapshot = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ZonePercentSnapshot = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    FormulaVersion = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadCalculations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoadCalculations_MonthlyApplications_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "MonthlyApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationZones_CompanyId",
                table: "ApplicationZones",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationZones_CropId",
                table: "ApplicationZones",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationZones_NozzleId",
                table: "ApplicationZones",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationZones_SoilId",
                table: "ApplicationZones",
                column: "SoilId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationZones_SprayfieldId",
                table: "ApplicationZones",
                column: "SprayfieldId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationZones_SprayfieldId_ZoneName",
                table: "ApplicationZones",
                columns: new[] { "SprayfieldId", "ZoneName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoadCalculations_ApplicationId",
                table: "LoadCalculations",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoadCalculations_CalculatedAtUtc",
                table: "LoadCalculations",
                column: "CalculatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyApplications_ApplicationDate",
                table: "MonthlyApplications",
                column: "ApplicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyApplications_CompanyId",
                table: "MonthlyApplications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyApplications_FacilityId",
                table: "MonthlyApplications",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyApplications_OperatorUserId",
                table: "MonthlyApplications",
                column: "OperatorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyApplications_ZoneId",
                table: "MonthlyApplications",
                column: "ZoneId");

            migrationBuilder.Sql(@"
CREATE OR ALTER VIEW [dbo].[MonthlyFieldAggregation]
AS
SELECT
    z.SprayfieldId,
    YEAR(a.ApplicationDate) AS [Year],
    MONTH(a.ApplicationDate) AS [Month],
    SUM(a.VolumeGallons) AS TotalVolumeGallons,
    SUM(c.LbsApplied) AS TotalLbsApplied
FROM MonthlyApplications a
INNER JOIN ApplicationZones z ON z.Id = a.ZoneId
LEFT JOIN LoadCalculations c ON c.ApplicationId = a.Id
GROUP BY z.SprayfieldId, YEAR(a.ApplicationDate), MONTH(a.ApplicationDate);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoadCalculations");

            migrationBuilder.DropTable(
                name: "MonthlyApplications");

            migrationBuilder.DropTable(
                name: "ApplicationZones");

            migrationBuilder.DropColumn(
                name: "AcresTotal",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "PermitFieldName",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "PermitNumber",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "InfiltrationRate",
                table: "Soils");

            migrationBuilder.DropColumn(
                name: "PANFactor",
                table: "Soils");

            migrationBuilder.DropColumn(
                name: "Pressure",
                table: "Nozzles");

            migrationBuilder.DropColumn(
                name: "PANLimit",
                table: "Crops");

            migrationBuilder.Sql("DROP VIEW IF EXISTS [dbo].[MonthlyFieldAggregation];");
        }
    }
}

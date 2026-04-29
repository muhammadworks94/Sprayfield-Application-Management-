using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class HardCutover_RemoveApplicationZones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CropId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NozzleId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SoilId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
;WITH z AS (
    SELECT
        az.SprayfieldId,
        az.SoilId,
        az.NozzleId,
        az.CropId,
        ROW_NUMBER() OVER (PARTITION BY az.SprayfieldId ORDER BY az.ZoneName) AS rn
    FROM ApplicationZones az
)
UPDATE s
SET
    s.SoilId = z.SoilId,
    s.NozzleId = z.NozzleId,
    s.CropId = z.CropId
FROM Sprayfields s
INNER JOIN z ON z.SprayfieldId = s.Id AND z.rn = 1;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Sprayfields WHERE SoilId IS NULL OR NozzleId IS NULL)
    THROW 51000, 'Hard cutover failed: one or more sprayfields could not be backfilled with Soil/Nozzle from ApplicationZones.', 1;
");

            migrationBuilder.AlterColumn<Guid>(
                name: "NozzleId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SoilId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyApplications_ApplicationZones_ZoneId",
                table: "MonthlyApplications");

            migrationBuilder.Sql("DELETE FROM MonthlyApplications;");

            migrationBuilder.DropTable(
                name: "ApplicationZones");

            migrationBuilder.RenameColumn(
                name: "ZoneId",
                table: "MonthlyApplications",
                newName: "SprayfieldId");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlyApplications_ZoneId",
                table: "MonthlyApplications",
                newName: "IX_MonthlyApplications_SprayfieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprayfields_CropId",
                table: "Sprayfields",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprayfields_NozzleId",
                table: "Sprayfields",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprayfields_SoilId",
                table: "Sprayfields",
                column: "SoilId");

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyApplications_Sprayfields_SprayfieldId",
                table: "MonthlyApplications",
                column: "SprayfieldId",
                principalTable: "Sprayfields",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprayfields_Crops_CropId",
                table: "Sprayfields",
                column: "CropId",
                principalTable: "Crops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprayfields_Nozzles_NozzleId",
                table: "Sprayfields",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprayfields_Soils_SoilId",
                table: "Sprayfields",
                column: "SoilId",
                principalTable: "Soils",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MonthlyApplications_Sprayfields_SprayfieldId",
                table: "MonthlyApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprayfields_Crops_CropId",
                table: "Sprayfields");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprayfields_Nozzles_NozzleId",
                table: "Sprayfields");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprayfields_Soils_SoilId",
                table: "Sprayfields");

            migrationBuilder.DropIndex(
                name: "IX_Sprayfields_CropId",
                table: "Sprayfields");

            migrationBuilder.DropIndex(
                name: "IX_Sprayfields_NozzleId",
                table: "Sprayfields");

            migrationBuilder.DropIndex(
                name: "IX_Sprayfields_SoilId",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "NozzleId",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "SoilId",
                table: "Sprayfields");

            migrationBuilder.RenameColumn(
                name: "SprayfieldId",
                table: "MonthlyApplications",
                newName: "ZoneId");

            migrationBuilder.RenameIndex(
                name: "IX_MonthlyApplications_SprayfieldId",
                table: "MonthlyApplications",
                newName: "IX_MonthlyApplications_ZoneId");

            migrationBuilder.CreateTable(
                name: "ApplicationZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CropId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NozzleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SoilId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SprayfieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Acres = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    PercentOfField = table.Column<decimal>(type: "decimal(9,4)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ZoneName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
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

            migrationBuilder.AddForeignKey(
                name: "FK_MonthlyApplications_ApplicationZones_ZoneId",
                table: "MonthlyApplications",
                column: "ZoneId",
                principalTable: "ApplicationZones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

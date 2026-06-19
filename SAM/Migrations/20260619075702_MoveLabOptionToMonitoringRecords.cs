using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class MoveLabOptionToMonitoringRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LabOptionId",
                table: "WWChars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LabOptionId",
                table: "GWMonits",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE w
SET w.LabOptionId = f.DefaultLabOptionId
FROM WWChars w
INNER JOIN Facilities f ON f.Id = w.FacilityId
WHERE w.LabOptionId IS NULL
  AND f.DefaultLabOptionId IS NOT NULL
  AND w.IsDeleted = 0
  AND f.IsDeleted = 0;

UPDATE g
SET g.LabOptionId = f.DefaultLabOptionId
FROM GWMonits g
INNER JOIN Facilities f ON f.Id = g.FacilityId
WHERE g.LabOptionId IS NULL
  AND f.DefaultLabOptionId IS NOT NULL
  AND g.IsDeleted = 0
  AND f.IsDeleted = 0;
");

            migrationBuilder.CreateIndex(
                name: "IX_WWChars_LabOptionId",
                table: "WWChars",
                column: "LabOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_GWMonits_LabOptionId",
                table: "GWMonits",
                column: "LabOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_GWMonits_CompanyLabOptions_LabOptionId",
                table: "GWMonits",
                column: "LabOptionId",
                principalTable: "CompanyLabOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_WWChars_CompanyLabOptions_LabOptionId",
                table: "WWChars",
                column: "LabOptionId",
                principalTable: "CompanyLabOptions",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.DropForeignKey(
                name: "FK_Facilities_CompanyLabOptions_DefaultLabOptionId",
                table: "Facilities");

            migrationBuilder.DropIndex(
                name: "IX_Facilities_DefaultLabOptionId",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "DefaultLabOptionId",
                table: "Facilities");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultLabOptionId",
                table: "Facilities",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facilities_DefaultLabOptionId",
                table: "Facilities",
                column: "DefaultLabOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Facilities_CompanyLabOptions_DefaultLabOptionId",
                table: "Facilities",
                column: "DefaultLabOptionId",
                principalTable: "CompanyLabOptions",
                principalColumn: "Id");

            migrationBuilder.Sql(@"
UPDATE f
SET f.DefaultLabOptionId = (
    SELECT TOP 1 w.LabOptionId
    FROM WWChars w
    WHERE w.FacilityId = f.Id AND w.LabOptionId IS NOT NULL AND w.IsDeleted = 0
    ORDER BY w.Year DESC, w.Month DESC
)
FROM Facilities f
WHERE f.DefaultLabOptionId IS NULL AND f.IsDeleted = 0;
");

            migrationBuilder.DropForeignKey(
                name: "FK_WWChars_CompanyLabOptions_LabOptionId",
                table: "WWChars");

            migrationBuilder.DropForeignKey(
                name: "FK_GWMonits_CompanyLabOptions_LabOptionId",
                table: "GWMonits");

            migrationBuilder.DropIndex(
                name: "IX_WWChars_LabOptionId",
                table: "WWChars");

            migrationBuilder.DropIndex(
                name: "IX_GWMonits_LabOptionId",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "LabOptionId",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "LabOptionId",
                table: "GWMonits");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitTemplateNotesAndReportScopedPcsUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId_PcsParameterCatalogId",
                table: "FacilityPermitTemplateParameters");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FacilityPermitTemplateParameters",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId_PcsParameterCatalogId_ReportTypes",
                table: "FacilityPermitTemplateParameters",
                columns: new[] { "FacilityPermitId", "PcsParameterCatalogId", "ReportTypes" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId_PcsParameterCatalogId_ReportTypes",
                table: "FacilityPermitTemplateParameters");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FacilityPermitTemplateParameters");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId_PcsParameterCatalogId",
                table: "FacilityPermitTemplateParameters",
                columns: new[] { "FacilityPermitId", "PcsParameterCatalogId" },
                unique: true);
        }
    }
}

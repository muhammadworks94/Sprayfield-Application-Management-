using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddGwMonitVocReportFileMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VOCReportContentType",
                table: "GWMonits",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VOCReportFileName",
                table: "GWMonits",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VOCReportFileStoragePath",
                table: "GWMonits",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VOCReportContentType",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "VOCReportFileName",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "VOCReportFileStoragePath",
                table: "GWMonits");
        }
    }
}

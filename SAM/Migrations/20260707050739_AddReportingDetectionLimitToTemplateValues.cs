using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddReportingDetectionLimitToTemplateValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReportingDetectionLimit",
                table: "WWCharTemplateValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReportingDetectionLimit",
                table: "GWMonitTemplateValues",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsReportingDetectionLimit",
                table: "WWCharTemplateValues");

            migrationBuilder.DropColumn(
                name: "IsReportingDetectionLimit",
                table: "GWMonitTemplateValues");
        }
    }
}

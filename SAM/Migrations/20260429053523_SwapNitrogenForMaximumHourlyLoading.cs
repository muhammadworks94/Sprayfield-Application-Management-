using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class SwapNitrogenForMaximumHourlyLoading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MaximumHourlyLoadingInchesPerAcre",
                table: "MonthlyApplications",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.DropColumn(
                name: "NitrogenMgL",
                table: "MonthlyApplications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NitrogenMgL",
                table: "MonthlyApplications",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.DropColumn(
                name: "MaximumHourlyLoadingInchesPerAcre",
                table: "MonthlyApplications");
        }
    }
}

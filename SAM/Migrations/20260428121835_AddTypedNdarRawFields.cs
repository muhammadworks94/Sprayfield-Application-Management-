using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddTypedNdarRawFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FiveDayUpsetFt",
                table: "OperatorLogs",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecipitationIn",
                table: "OperatorLogs",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StorageFt",
                table: "OperatorLogs",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TemperatureF",
                table: "OperatorLogs",
                type: "decimal(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TimeIrrigatedMinutes",
                table: "MonthlyApplications",
                type: "decimal(10,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiveDayUpsetFt",
                table: "OperatorLogs");

            migrationBuilder.DropColumn(
                name: "PrecipitationIn",
                table: "OperatorLogs");

            migrationBuilder.DropColumn(
                name: "StorageFt",
                table: "OperatorLogs");

            migrationBuilder.DropColumn(
                name: "TemperatureF",
                table: "OperatorLogs");

            migrationBuilder.DropColumn(
                name: "TimeIrrigatedMinutes",
                table: "MonthlyApplications");
        }
    }
}

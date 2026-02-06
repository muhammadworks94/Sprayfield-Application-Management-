using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class addNewFieldsToMonitoringWell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "MonitoringWells",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "MonitoringWells",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");

            migrationBuilder.AddColumn<decimal>(
                name: "DepthToScreenFeet",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiameterInches",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HighScreenDepthFeet",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LowScreenDepthFeet",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfWellsToBeSampled",
                table: "MonitoringWells",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TopOfCasingElevationMsl",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TreatmentSystemLocation",
                table: "MonitoringWells",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WellDepthFeet",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WellPermitNumber",
                table: "MonitoringWells",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DepthToScreenFeet",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "DiameterInches",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "HighScreenDepthFeet",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "LowScreenDepthFeet",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "NumberOfWellsToBeSampled",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "TopOfCasingElevationMsl",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "TreatmentSystemLocation",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "WellDepthFeet",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "WellPermitNumber",
                table: "MonitoringWells");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "MonitoringWells",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "MonitoringWells",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);
        }
    }
}

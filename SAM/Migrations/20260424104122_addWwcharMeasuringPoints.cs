using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class addWwcharMeasuringPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FlowMeasuringPoint",
                table: "WWChars",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParameterMonitoringPoint",
                table: "WWChars",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlowMeasuringPoint",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "ParameterMonitoringPoint",
                table: "WWChars");
        }
    }
}

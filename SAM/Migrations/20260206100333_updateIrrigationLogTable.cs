using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class updateIrrigationLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Operator",
                table: "Irrigates");

            migrationBuilder.DropColumn(
                name: "WindDirection",
                table: "Irrigates");

            migrationBuilder.RenameColumn(
                name: "WindSpeed",
                table: "Irrigates",
                newName: "TemperatureF");

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Irrigates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrecipitationIn",
                table: "Irrigates",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Irrigates");

            migrationBuilder.DropColumn(
                name: "PrecipitationIn",
                table: "Irrigates");

            migrationBuilder.RenameColumn(
                name: "TemperatureF",
                table: "Irrigates",
                newName: "WindSpeed");

            migrationBuilder.AddColumn<string>(
                name: "Operator",
                table: "Irrigates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "WindDirection",
                table: "Irrigates",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }
    }
}

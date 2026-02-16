using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class PanMissingParametersAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "NO2N",
                table: "WWChars",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MineralizationRatePercent",
                table: "Facilities",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VolatilizationRatePercent",
                table: "Facilities",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NO2N",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "MineralizationRatePercent",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "VolatilizationRatePercent",
                table: "Facilities");
        }
    }
}

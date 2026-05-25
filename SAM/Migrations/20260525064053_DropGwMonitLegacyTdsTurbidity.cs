using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class DropGwMonitLegacyTdsTurbidity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TDS",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "Turbidity",
                table: "GWMonits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TDS",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Turbidity",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}

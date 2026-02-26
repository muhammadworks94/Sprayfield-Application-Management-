using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class removePanFromCropAndAddTknnNo3nToWWChar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PanFactor",
                table: "Crops");

            migrationBuilder.AddColumn<decimal>(
                name: "NO3N",
                table: "WWChars",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TKNN",
                table: "WWChars",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NO3N",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "TKNN",
                table: "WWChars");

            migrationBuilder.AddColumn<decimal>(
                name: "PanFactor",
                table: "Crops",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}

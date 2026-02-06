using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class updateWasteWaterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalColiformDaily",
                table: "WWChars",
                newName: "TNDaily");

            migrationBuilder.RenameColumn(
                name: "TDSDaily",
                table: "WWChars",
                newName: "SARDaily");

            migrationBuilder.AddColumn<string>(
                name: "CaDaily",
                table: "WWChars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MgDaily",
                table: "WWChars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NaDaily",
                table: "WWChars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaDaily",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "MgDaily",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "NaDaily",
                table: "WWChars");

            migrationBuilder.RenameColumn(
                name: "TNDaily",
                table: "WWChars",
                newName: "TotalColiformDaily");

            migrationBuilder.RenameColumn(
                name: "SARDaily",
                table: "WWChars",
                newName: "TDSDaily");
        }
    }
}

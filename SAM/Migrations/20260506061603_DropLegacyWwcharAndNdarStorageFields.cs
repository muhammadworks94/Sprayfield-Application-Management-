using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyWwcharAndNdarStorageFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LagoonFreeboard",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "ORCOnSite",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "StorageDaily",
                table: "NDAR1s");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LagoonFreeboard",
                table: "WWChars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ORCOnSite",
                table: "WWChars",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StorageDaily",
                table: "NDAR1s",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}

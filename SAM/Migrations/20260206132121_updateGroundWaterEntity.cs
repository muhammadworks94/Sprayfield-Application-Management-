using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class updateGroundWaterEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalPhosphorus",
                table: "GWMonits",
                newName: "TOC");

            migrationBuilder.RenameColumn(
                name: "COD",
                table: "GWMonits",
                newName: "Magnesium");

            migrationBuilder.RenameColumn(
                name: "BOD5",
                table: "GWMonits",
                newName: "GallonsPumped");

            migrationBuilder.AddColumn<string>(
                name: "Appearance",
                table: "GWMonits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Calcium",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MetalSamplesFieldAcidified",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MetalsSamplesCollectedUnfiltered",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Odor",
                table: "GWMonits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VOCMethodNumber",
                table: "GWMonits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "VOCReportAttached",
                table: "GWMonits",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Appearance",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "Calcium",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "MetalSamplesFieldAcidified",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "MetalsSamplesCollectedUnfiltered",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "Odor",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "VOCMethodNumber",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "VOCReportAttached",
                table: "GWMonits");

            migrationBuilder.RenameColumn(
                name: "TOC",
                table: "GWMonits",
                newName: "TotalPhosphorus");

            migrationBuilder.RenameColumn(
                name: "Magnesium",
                table: "GWMonits",
                newName: "COD");

            migrationBuilder.RenameColumn(
                name: "GallonsPumped",
                table: "GWMonits",
                newName: "BOD5");
        }
    }
}

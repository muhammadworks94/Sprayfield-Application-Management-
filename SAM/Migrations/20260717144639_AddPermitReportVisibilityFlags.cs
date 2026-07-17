using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitReportVisibilityFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowGw59Report",
                table: "FacilityPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowNdar1Report",
                table: "FacilityPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowNdmlrReport",
                table: "FacilityPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowNdmrReport",
                table: "FacilityPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowGw59Report",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "ShowNdar1Report",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "ShowNdmlrReport",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "ShowNdmrReport",
                table: "FacilityPermits");
        }
    }
}

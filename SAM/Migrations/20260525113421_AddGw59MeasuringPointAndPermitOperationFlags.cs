using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddGw59MeasuringPointAndPermitOperationFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MeasuringPointAboveLandSurface",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GwOperationLagoon",
                table: "FacilityPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "GwOperationSprayField",
                table: "FacilityPermits",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MeasuringPointAboveLandSurface",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GwOperationLagoon",
                table: "FacilityPermits");

            migrationBuilder.DropColumn(
                name: "GwOperationSprayField",
                table: "FacilityPermits");
        }
    }
}

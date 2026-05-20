using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddNdmlrSelectedMonthRollingWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NDMLRs_FacilityId_Year",
                table: "NDMLRs");

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "NDMLRs",
                type: "int",
                nullable: false,
                defaultValue: 12);

            migrationBuilder.Sql("UPDATE NDMLRs SET [Month] = 12 WHERE [Month] = 0;");

            migrationBuilder.CreateIndex(
                name: "IX_NDMLRs_FacilityId_Year_Month",
                table: "NDMLRs",
                columns: new[] { "FacilityId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NDMLRs_FacilityId_Year_Month",
                table: "NDMLRs");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "NDMLRs");

            migrationBuilder.CreateIndex(
                name: "IX_NDMLRs_FacilityId_Year",
                table: "NDMLRs",
                columns: new[] { "FacilityId", "Year" },
                unique: true);
        }
    }
}

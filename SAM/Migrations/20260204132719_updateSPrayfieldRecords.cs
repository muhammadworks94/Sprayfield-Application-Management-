using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class updateSPrayfieldRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AnnualRateInches",
                table: "Sprayfields",
                newName: "WeeklyRateInches");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WeeklyRateInches",
                table: "Sprayfields",
                newName: "AnnualRateInches");
        }
    }
}

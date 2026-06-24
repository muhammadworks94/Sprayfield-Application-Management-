using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddSprayfieldActualHourlyRateInches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualHourlyRateInches",
                table: "Sprayfields",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE s
                SET ActualHourlyRateInches = 0.23
                FROM Sprayfields s
                INNER JOIN Facilities f ON s.FacilityId = f.Id
                WHERE f.Name LIKE '%Edenton%' AND s.IsDeleted = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualHourlyRateInches",
                table: "Sprayfields");
        }
    }
}

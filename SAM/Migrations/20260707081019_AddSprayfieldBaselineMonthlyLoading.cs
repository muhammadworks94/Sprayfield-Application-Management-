using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddSprayfieldBaselineMonthlyLoading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SprayfieldBaselineMonthlyLoadings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SprayfieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    LoadingInches = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SprayfieldBaselineMonthlyLoadings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SprayfieldBaselineMonthlyLoadings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SprayfieldBaselineMonthlyLoadings_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SprayfieldBaselineMonthlyLoadings_Sprayfields_SprayfieldId",
                        column: x => x.SprayfieldId,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SprayfieldBaselineMonthlyLoadings_CompanyId",
                table: "SprayfieldBaselineMonthlyLoadings",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_SprayfieldBaselineMonthlyLoadings_FacilityId",
                table: "SprayfieldBaselineMonthlyLoadings",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_SprayfieldBaselineMonthlyLoadings_FacilityId_SprayfieldId_Year_Month",
                table: "SprayfieldBaselineMonthlyLoadings",
                columns: new[] { "FacilityId", "SprayfieldId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SprayfieldBaselineMonthlyLoadings_SprayfieldId",
                table: "SprayfieldBaselineMonthlyLoadings",
                column: "SprayfieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SprayfieldBaselineMonthlyLoadings");
        }
    }
}

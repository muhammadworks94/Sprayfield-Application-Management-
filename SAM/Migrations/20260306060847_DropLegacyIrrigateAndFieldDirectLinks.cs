using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyIrrigateAndFieldDirectLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Irrigates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Legacy data cannot be restored. This only recreates schema.
            migrationBuilder.CreateTable(
                name: "Irrigates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SprayfieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationRateInches = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    FlowRateGpm = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IrrigationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PrecipitationIn = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    TemperatureF = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalVolumeGallons = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WeatherConditions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Irrigates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Irrigates_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Irrigates_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Irrigates_Sprayfields_SprayfieldId",
                        column: x => x.SprayfieldId,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Irrigates_CompanyId",
                table: "Irrigates",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Irrigates_FacilityId",
                table: "Irrigates",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Irrigates_IrrigationDate",
                table: "Irrigates",
                column: "IrrigationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Irrigates_SprayfieldId",
                table: "Irrigates",
                column: "SprayfieldId");
        }
    }
}




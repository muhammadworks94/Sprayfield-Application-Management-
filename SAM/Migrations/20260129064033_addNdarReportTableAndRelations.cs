using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class addNdarReportTableAndRelations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AnnualRateInches",
                table: "Sprayfields",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRateInches",
                table: "Sprayfields",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "County",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NDAR1s",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    DidIrrigationOccur = table.Column<bool>(type: "bit", nullable: false),
                    WeatherCodeDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TemperatureDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrecipitationDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StorageDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FiveDayUpsetDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Field1VolumeAppliedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field1TimeIrrigatedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field1DailyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field1MaxHourlyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field1MonthlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field1MaxHourlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field1TwelveMonthFloatingTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field2Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Field2VolumeAppliedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field2TimeIrrigatedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field2DailyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field2MaxHourlyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field2MonthlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field2MaxHourlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field2TwelveMonthFloatingTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field3Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Field3VolumeAppliedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field3TimeIrrigatedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field3DailyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field3MaxHourlyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field3MonthlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field3MaxHourlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field3TwelveMonthFloatingTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field4Id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Field4VolumeAppliedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field4TimeIrrigatedDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field4DailyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field4MaxHourlyLoadingDaily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Field4MonthlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field4MaxHourlyLoading = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Field4TwelveMonthFloatingTotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDAR1s", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NDAR1s_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NDAR1s_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDAR1s_Sprayfields_Field1Id",
                        column: x => x.Field1Id,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDAR1s_Sprayfields_Field2Id",
                        column: x => x.Field2Id,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDAR1s_Sprayfields_Field3Id",
                        column: x => x.Field3Id,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NDAR1s_Sprayfields_Field4Id",
                        column: x => x.Field4Id,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_CompanyId",
                table: "NDAR1s",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_FacilityId",
                table: "NDAR1s",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_FacilityId_Month_Year",
                table: "NDAR1s",
                columns: new[] { "FacilityId", "Month", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_Field1Id",
                table: "NDAR1s",
                column: "Field1Id");

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_Field2Id",
                table: "NDAR1s",
                column: "Field2Id");

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_Field3Id",
                table: "NDAR1s",
                column: "Field3Id");

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1s_Field4Id",
                table: "NDAR1s",
                column: "Field4Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NDAR1s");

            migrationBuilder.DropColumn(
                name: "AnnualRateInches",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "HourlyRateInches",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "County",
                table: "Facilities");
        }
    }
}

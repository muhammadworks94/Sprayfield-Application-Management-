using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddNdar1DynamicFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NDAR1Fields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NDAR1Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SprayfieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FieldOrder = table.Column<int>(type: "int", nullable: false),
                    MonthlyLoading = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    MaxHourlyLoading = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    TwelveMonthFloatingTotal = table.Column<decimal>(type: "decimal(18,6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDAR1Fields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NDAR1Fields_NDAR1s_NDAR1Id",
                        column: x => x.NDAR1Id,
                        principalTable: "NDAR1s",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NDAR1Fields_Sprayfields_SprayfieldId",
                        column: x => x.SprayfieldId,
                        principalTable: "Sprayfields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NDAR1FieldDailies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NDAR1FieldId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNo = table.Column<int>(type: "int", nullable: false),
                    VolumeApplied = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    TimeIrrigated = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    DailyLoading = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    MaxHourlyLoading = table.Column<decimal>(type: "decimal(18,6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NDAR1FieldDailies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NDAR1FieldDailies_NDAR1Fields_NDAR1FieldId",
                        column: x => x.NDAR1FieldId,
                        principalTable: "NDAR1Fields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1FieldDailies_NDAR1FieldId_DayNo",
                table: "NDAR1FieldDailies",
                columns: new[] { "NDAR1FieldId", "DayNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1Fields_NDAR1Id_FieldOrder",
                table: "NDAR1Fields",
                columns: new[] { "NDAR1Id", "FieldOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1Fields_NDAR1Id_SprayfieldId",
                table: "NDAR1Fields",
                columns: new[] { "NDAR1Id", "SprayfieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NDAR1Fields_SprayfieldId",
                table: "NDAR1Fields",
                column: "SprayfieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NDAR1FieldDailies");

            migrationBuilder.DropTable(
                name: "NDAR1Fields");
        }
    }
}

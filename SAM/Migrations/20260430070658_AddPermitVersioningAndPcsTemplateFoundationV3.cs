using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddPermitVersioningAndPcsTemplateFoundationV3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "FacilityPermitId",
                table: "WWChars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FacilityPermits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermitNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PermitVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EffectiveStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PermitPdfFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: true),
                    PermitPdfStoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityPermits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityPermits_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacilityPermits_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PcsParameterCatalogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PcsCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    UserFriendlyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OfficialParameterName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AcceptedUnits = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PcsParameterCatalogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacilityPermitTemplateParameters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityPermitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PcsParameterCatalogId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParameterDisplayOverride = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UnitsOverride = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MonthlyAverageLimit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MonthlyGeometricMeanLimit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DailyMinimumLimit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    DailyMaximumLimit = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    SampleType = table.Column<int>(type: "int", nullable: false),
                    MeasurementFrequency = table.Column<int>(type: "int", nullable: false),
                    ScheduledMonthsCsv = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ReportTypes = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityPermitTemplateParameters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityPermitTemplateParameters_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacilityPermitTemplateParameters_FacilityPermits_FacilityPermitId",
                        column: x => x.FacilityPermitId,
                        principalTable: "FacilityPermits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacilityPermitTemplateParameters_PcsParameterCatalogs_PcsParameterCatalogId",
                        column: x => x.PcsParameterCatalogId,
                        principalTable: "PcsParameterCatalogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GWMonitTemplateValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GWMonitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityPermitTemplateParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GWMonitTemplateValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GWMonitTemplateValues_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GWMonitTemplateValues_FacilityPermitTemplateParameters_FacilityPermitTemplateParameterId",
                        column: x => x.FacilityPermitTemplateParameterId,
                        principalTable: "FacilityPermitTemplateParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GWMonitTemplateValues_GWMonits_GWMonitId",
                        column: x => x.GWMonitId,
                        principalTable: "GWMonits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WWCharTemplateValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WWCharId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityPermitTemplateParameterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayNo = table.Column<int>(type: "int", nullable: false),
                    NumericValue = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TextValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WWCharTemplateValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WWCharTemplateValues_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WWCharTemplateValues_FacilityPermitTemplateParameters_FacilityPermitTemplateParameterId",
                        column: x => x.FacilityPermitTemplateParameterId,
                        principalTable: "FacilityPermitTemplateParameters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WWCharTemplateValues_WWChars_WWCharId",
                        column: x => x.WWCharId,
                        principalTable: "WWChars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WWChars_FacilityPermitId",
                table: "WWChars",
                column: "FacilityPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermits_CompanyId",
                table: "FacilityPermits",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermits_FacilityId",
                table: "FacilityPermits",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermits_FacilityId_EffectiveStartDate_EffectiveEndDate",
                table: "FacilityPermits",
                columns: new[] { "FacilityId", "EffectiveStartDate", "EffectiveEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermits_FacilityId_PermitNumber_PermitVersion",
                table: "FacilityPermits",
                columns: new[] { "FacilityId", "PermitNumber", "PermitVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_CompanyId",
                table: "FacilityPermitTemplateParameters",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId",
                table: "FacilityPermitTemplateParameters",
                column: "FacilityPermitId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId_PcsParameterCatalogId",
                table: "FacilityPermitTemplateParameters",
                columns: new[] { "FacilityPermitId", "PcsParameterCatalogId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_FacilityPermitId_SortOrder",
                table: "FacilityPermitTemplateParameters",
                columns: new[] { "FacilityPermitId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityPermitTemplateParameters_PcsParameterCatalogId",
                table: "FacilityPermitTemplateParameters",
                column: "PcsParameterCatalogId");

            migrationBuilder.CreateIndex(
                name: "IX_GWMonitTemplateValues_CompanyId",
                table: "GWMonitTemplateValues",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_GWMonitTemplateValues_FacilityPermitTemplateParameterId",
                table: "GWMonitTemplateValues",
                column: "FacilityPermitTemplateParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_GWMonitTemplateValues_GWMonitId",
                table: "GWMonitTemplateValues",
                column: "GWMonitId");

            migrationBuilder.CreateIndex(
                name: "IX_GWMonitTemplateValues_GWMonitId_FacilityPermitTemplateParameterId",
                table: "GWMonitTemplateValues",
                columns: new[] { "GWMonitId", "FacilityPermitTemplateParameterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PcsParameterCatalogs_PcsCode",
                table: "PcsParameterCatalogs",
                column: "PcsCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTemplateValues_CompanyId",
                table: "WWCharTemplateValues",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTemplateValues_FacilityPermitTemplateParameterId",
                table: "WWCharTemplateValues",
                column: "FacilityPermitTemplateParameterId");

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTemplateValues_WWCharId",
                table: "WWCharTemplateValues",
                column: "WWCharId");

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTemplateValues_WWCharId_FacilityPermitTemplateParameterId_DayNo",
                table: "WWCharTemplateValues",
                columns: new[] { "WWCharId", "FacilityPermitTemplateParameterId", "DayNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WWChars_FacilityPermits_FacilityPermitId",
                table: "WWChars",
                column: "FacilityPermitId",
                principalTable: "FacilityPermits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WWChars_FacilityPermits_FacilityPermitId",
                table: "WWChars");

            migrationBuilder.DropTable(
                name: "GWMonitTemplateValues");

            migrationBuilder.DropTable(
                name: "WWCharTemplateValues");

            migrationBuilder.DropTable(
                name: "FacilityPermitTemplateParameters");

            migrationBuilder.DropTable(
                name: "FacilityPermits");

            migrationBuilder.DropTable(
                name: "PcsParameterCatalogs");

            migrationBuilder.DropIndex(
                name: "IX_WWChars_FacilityPermitId",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "FacilityPermitId",
                table: "WWChars");
        }
    }
}

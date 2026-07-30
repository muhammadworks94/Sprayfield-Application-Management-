using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddFacilityOrcAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FacilityOrcAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OperatorNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatorGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    OperatorPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityOrcAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityOrcAssignments_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacilityOrcAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityOrcAssignments_FacilityId",
                table: "FacilityOrcAssignments",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_FacilityOrcAssignments_FacilityId_EndDate",
                table: "FacilityOrcAssignments",
                columns: new[] { "FacilityId", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityOrcAssignments_UserId",
                table: "FacilityOrcAssignments",
                column: "UserId");

            // Backfill open ORC assignments from existing Facility ORC free-text fields.
            migrationBuilder.Sql(@"
                INSERT INTO FacilityOrcAssignments
                (
                    Id, FacilityId, UserId, StartDate, EndDate,
                    DisplayName, OperatorNumber, OperatorGrade, OperatorPhone,
                    CreatedDate, UpdatedDate, CreatedBy, IsDeleted
                )
                SELECT
                    NEWID(),
                    f.Id,
                    NULL,
                    CAST(COALESCE(f.CreatedDate, SYSUTCDATETIME()) AS date),
                    NULL,
                    LTRIM(RTRIM(f.OrcName)),
                    NULLIF(LTRIM(RTRIM(f.OperatorNumber)), ''),
                    NULLIF(LTRIM(RTRIM(f.OperatorGrade)), ''),
                    NULLIF(LTRIM(RTRIM(f.OperatorPhone)), ''),
                    SYSUTCDATETIME(),
                    NULL,
                    'migration-backfill',
                    0
                FROM Facilities f
                WHERE f.IsDeleted = 0
                  AND f.OrcName IS NOT NULL
                  AND LTRIM(RTRIM(f.OrcName)) <> ''
                  AND NOT EXISTS (
                      SELECT 1
                      FROM FacilityOrcAssignments a
                      WHERE a.FacilityId = f.Id AND a.EndDate IS NULL AND a.IsDeleted = 0
                  );
            ");

            // Link Edenton open assignments to Anthony Jordan SAM user when present.
            migrationBuilder.Sql(@"
                UPDATE a
                SET a.UserId = u.Id,
                    a.DisplayName = CASE
                        WHEN NULLIF(LTRIM(RTRIM(u.FullName)), '') IS NOT NULL THEN LTRIM(RTRIM(u.FullName))
                        ELSE a.DisplayName
                    END,
                    a.UpdatedDate = SYSUTCDATETIME()
                FROM FacilityOrcAssignments a
                INNER JOIN Facilities f ON f.Id = a.FacilityId
                INNER JOIN Users u ON u.CompanyId = f.CompanyId
                WHERE a.EndDate IS NULL
                  AND a.IsDeleted = 0
                  AND f.IsDeleted = 0
                  AND f.Name LIKE '%Edenton%'
                  AND (
                        u.FullName LIKE '%Anthony Jordan%'
                     OR u.Email LIKE '%anthony.jordan%'
                  );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacilityOrcAssignments");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddNdarEditLocksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NdarEditLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    LockedByDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LockToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NdarEditLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NdarEditLocks_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NdarEditLocks_Facilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "Facilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NdarEditLocks_Users_LockedByUserId",
                        column: x => x.LockedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NdarEditLocks_CompanyId",
                table: "NdarEditLocks",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_NdarEditLocks_ExpiresAtUtc",
                table: "NdarEditLocks",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_NdarEditLocks_FacilityId_EditDate",
                table: "NdarEditLocks",
                columns: new[] { "FacilityId", "EditDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NdarEditLocks_LockedByUserId",
                table: "NdarEditLocks",
                column: "LockedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NdarEditLocks");
        }
    }
}

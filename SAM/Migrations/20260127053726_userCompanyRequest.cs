using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class userCompanyRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelfSignup",
                table: "UserRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CompanyRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TaxId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FaxNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LicenseNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RequesterFullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequesterEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedCompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyRequests_Companies_CreatedCompanyId",
                        column: x => x.CreatedCompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRequests_IsSelfSignup",
                table: "UserRequests",
                column: "IsSelfSignup");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRequests_CreatedCompanyId",
                table: "CompanyRequests",
                column: "CreatedCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRequests_RequesterEmail",
                table: "CompanyRequests",
                column: "RequesterEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyRequests_Status",
                table: "CompanyRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyRequests");

            migrationBuilder.DropIndex(
                name: "IX_UserRequests_IsSelfSignup",
                table: "UserRequests");

            migrationBuilder.DropColumn(
                name: "IsSelfSignup",
                table: "UserRequests");
        }
    }
}

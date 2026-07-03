using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TemplateKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TemplateDisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    InitiatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    InitiatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    InitiatedByDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_SentAtUtc",
                table: "EmailLogs",
                column: "SentAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_Status_SentAtUtc",
                table: "EmailLogs",
                columns: new[] { "Status", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_TemplateKey_SentAtUtc",
                table: "EmailLogs",
                columns: new[] { "TemplateKey", "SentAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogs_ToEmail_SentAtUtc",
                table: "EmailLogs",
                columns: new[] { "ToEmail", "SentAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogs");
        }
    }
}

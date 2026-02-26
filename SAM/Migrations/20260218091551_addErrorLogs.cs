using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class addErrorLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    QueryString = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    InnerExceptionType = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InnerExceptionMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ActorUserId_OccurredAtUtc",
                table: "ErrorLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_CompanyId_OccurredAtUtc",
                table: "ErrorLogs",
                columns: new[] { "CompanyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_ExceptionType_OccurredAtUtc",
                table: "ErrorLogs",
                columns: new[] { "ExceptionType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_OccurredAtUtc",
                table: "ErrorLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorLogs_StatusCode_OccurredAtUtc",
                table: "ErrorLogs",
                columns: new[] { "StatusCode", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLogs");
        }
    }
}

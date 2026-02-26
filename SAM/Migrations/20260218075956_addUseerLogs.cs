using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class addUseerLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ActorDisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Path = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_ActivityType_OccurredAtUtc",
                table: "UserActivityLogs",
                columns: new[] { "ActivityType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_ActorUserId_OccurredAtUtc",
                table: "UserActivityLogs",
                columns: new[] { "ActorUserId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_CompanyId_OccurredAtUtc",
                table: "UserActivityLogs",
                columns: new[] { "CompanyId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_OccurredAtUtc",
                table: "UserActivityLogs",
                column: "OccurredAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserActivityLogs");
        }
    }
}

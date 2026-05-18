using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddWwCharTestResultAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WWCharTestResultAttachments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WWCharId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TestDate = table.Column<DateTime>(type: "date", nullable: false),
                    FileStoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WWCharTestResultAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WWCharTestResultAttachments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_WWCharTestResultAttachments_WWChars_WWCharId",
                        column: x => x.WWCharId,
                        principalTable: "WWChars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTestResultAttachments_CompanyId",
                table: "WWCharTestResultAttachments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTestResultAttachments_TestDate",
                table: "WWCharTestResultAttachments",
                column: "TestDate");

            migrationBuilder.CreateIndex(
                name: "IX_WWCharTestResultAttachments_WWCharId",
                table: "WWCharTestResultAttachments",
                column: "WWCharId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WWCharTestResultAttachments");
        }
    }
}

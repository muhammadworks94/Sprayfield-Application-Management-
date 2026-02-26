using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class adddSMTPSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SmtpSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "SMTP_GLOBAL"),
                    Host = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordEncrypted = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FromEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EnableSsl = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SmtpSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SmtpSettings_ScopeKey",
                table: "SmtpSettings",
                column: "ScopeKey",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SmtpSettings");
        }
    }
}

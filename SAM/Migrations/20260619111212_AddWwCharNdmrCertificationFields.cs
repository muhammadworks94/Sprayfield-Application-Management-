using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddWwCharNdmrCertificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SamplingPerson1",
                table: "WWChars",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SamplingPerson2",
                table: "WWChars",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "SecondaryLabOptionId",
                table: "WWChars",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WWChars_SecondaryLabOptionId",
                table: "WWChars",
                column: "SecondaryLabOptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WWChars_CompanyLabOptions_SecondaryLabOptionId",
                table: "WWChars",
                column: "SecondaryLabOptionId",
                principalTable: "CompanyLabOptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WWChars_CompanyLabOptions_SecondaryLabOptionId",
                table: "WWChars");

            migrationBuilder.DropIndex(
                name: "IX_WWChars_SecondaryLabOptionId",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "SamplingPerson1",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "SamplingPerson2",
                table: "WWChars");

            migrationBuilder.DropColumn(
                name: "SecondaryLabOptionId",
                table: "WWChars");
        }
    }
}

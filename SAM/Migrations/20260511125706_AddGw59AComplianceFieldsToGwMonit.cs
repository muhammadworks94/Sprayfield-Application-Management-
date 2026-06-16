using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddGw59AComplianceFieldsToGwMonit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "GW59ADueDate",
                table: "GWMonits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion1Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GW59AQuestion2Details",
                table: "GWMonits",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion2Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion3Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GW59AQuestion4Details",
                table: "GWMonits",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion4Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GW59AQuestion5Details",
                table: "GWMonits",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion5Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion6Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GW59AQuestion7Details",
                table: "GWMonits",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GW59AQuestion7Response",
                table: "GWMonits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GW59ASignedDate",
                table: "GWMonits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GW59ASignerName",
                table: "GWMonits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GW59ASignerTitle",
                table: "GWMonits",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GW59ADueDate",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion1Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion2Details",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion2Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion3Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion4Details",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion4Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion5Details",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion5Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion6Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion7Details",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59AQuestion7Response",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59ASignedDate",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59ASignerName",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "GW59ASignerTitle",
                table: "GWMonits");
        }
    }
}

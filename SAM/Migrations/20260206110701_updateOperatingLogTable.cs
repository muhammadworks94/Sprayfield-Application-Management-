using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class updateOperatingLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Shift",
                table: "OperatorLogs");

            migrationBuilder.DropColumn(
                name: "SystemStatus",
                table: "OperatorLogs");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ArrivalTime",
                table: "OperatorLogs",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<decimal>(
                name: "TimeOnSiteHours",
                table: "OperatorLogs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ArrivalTime",
                table: "OperatorLogs");

            migrationBuilder.DropColumn(
                name: "TimeOnSiteHours",
                table: "OperatorLogs");

            migrationBuilder.AddColumn<int>(
                name: "Shift",
                table: "OperatorLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SystemStatus",
                table: "OperatorLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class uddNewFieldsToFacility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CertifiedLaboratory1Name",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CertifiedLaboratory2Name",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ChangeInOrc",
                table: "Facilities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FacilityPhone",
                table: "Facilities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabCertificationNumber1",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LabCertificationNumber2",
                table: "Facilities",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperatorGrade",
                table: "Facilities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OperatorNumber",
                table: "Facilities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OrcName",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PermitExpirationDate",
                table: "Facilities",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermitPhone",
                table: "Facilities",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PermittedMinimumFreeboardFeet",
                table: "Facilities",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonsCollectingSamples",
                table: "Facilities",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TotalNumberOfSprayfields",
                table: "Facilities",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CertifiedLaboratory1Name",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "CertifiedLaboratory2Name",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "ChangeInOrc",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "FacilityPhone",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "LabCertificationNumber1",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "LabCertificationNumber2",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "OperatorGrade",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "OperatorNumber",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "OrcName",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PermitExpirationDate",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PermitPhone",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PermittedMinimumFreeboardFeet",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "PersonsCollectingSamples",
                table: "Facilities");

            migrationBuilder.DropColumn(
                name: "TotalNumberOfSprayfields",
                table: "Facilities");
        }
    }
}

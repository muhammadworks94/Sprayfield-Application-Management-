using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacySprayfieldZoneFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sprayfields_Crops_CropId",
                table: "Sprayfields");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprayfields_Nozzles_NozzleId",
                table: "Sprayfields");

            migrationBuilder.DropForeignKey(
                name: "FK_Sprayfields_Soils_SoilId",
                table: "Sprayfields");

            migrationBuilder.DropIndex(
                name: "IX_Sprayfields_CropId",
                table: "Sprayfields");

            migrationBuilder.DropIndex(
                name: "IX_Sprayfields_NozzleId",
                table: "Sprayfields");

            migrationBuilder.DropIndex(
                name: "IX_Sprayfields_SoilId",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "CropId",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "NozzleId",
                table: "Sprayfields");

            migrationBuilder.DropColumn(
                name: "SoilId",
                table: "Sprayfields");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CropId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "NozzleId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "SoilId",
                table: "Sprayfields",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Sprayfields_CropId",
                table: "Sprayfields",
                column: "CropId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprayfields_NozzleId",
                table: "Sprayfields",
                column: "NozzleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprayfields_SoilId",
                table: "Sprayfields",
                column: "SoilId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sprayfields_Crops_CropId",
                table: "Sprayfields",
                column: "CropId",
                principalTable: "Crops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprayfields_Nozzles_NozzleId",
                table: "Sprayfields",
                column: "NozzleId",
                principalTable: "Nozzles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Sprayfields_Soils_SoilId",
                table: "Sprayfields",
                column: "SoilId",
                principalTable: "Soils",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

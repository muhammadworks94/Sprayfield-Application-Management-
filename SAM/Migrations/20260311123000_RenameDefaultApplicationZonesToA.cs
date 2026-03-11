using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    public partial class RenameDefaultApplicationZonesToA : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ApplicationZones SET ZoneName = 'A' WHERE ZoneName = 'Default';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE ApplicationZones SET ZoneName = 'Default' WHERE ZoneName = 'A';");
        }
    }
}

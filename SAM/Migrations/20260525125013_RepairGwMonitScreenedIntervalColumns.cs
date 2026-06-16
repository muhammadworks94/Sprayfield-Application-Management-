using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class RepairGwMonitScreenedIntervalColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.GWMonits', 'ScreenedIntervalFromFeet') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[GWMonits] ADD [ScreenedIntervalFromFeet] decimal(18,2) NULL;
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.GWMonits', 'ScreenedIntervalToFeet') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[GWMonits] ADD [ScreenedIntervalToFeet] decimal(18,2) NULL;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.GWMonits', 'ScreenedIntervalFromFeet') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[GWMonits] DROP COLUMN [ScreenedIntervalFromFeet];
                END
                """);

            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.GWMonits', 'ScreenedIntervalToFeet') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[GWMonits] DROP COLUMN [ScreenedIntervalToFeet];
                END
                """);
        }
    }
}

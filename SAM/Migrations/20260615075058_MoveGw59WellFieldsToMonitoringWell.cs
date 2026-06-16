using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class MoveGw59WellFieldsToMonitoringWell : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MeasuringPointAboveLandSurface",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RelativeMpElevation",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScreenedIntervalFromFeet",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScreenedIntervalToFeet",
                table: "MonitoringWells",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.Sql(
                """
                ;WITH LatestGw AS (
                    SELECT
                        g.MonitoringWellId,
                        g.ScreenedIntervalFromFeet,
                        g.ScreenedIntervalToFeet,
                        g.MeasuringPointAboveLandSurface,
                        g.RelativeMpElevation,
                        ROW_NUMBER() OVER (
                            PARTITION BY g.MonitoringWellId
                            ORDER BY g.SampleDate DESC, g.CreatedDate DESC
                        ) AS rn
                    FROM GWMonits g
                    WHERE g.IsDeleted = 0
                      AND (
                          g.ScreenedIntervalFromFeet IS NOT NULL
                          OR g.ScreenedIntervalToFeet IS NOT NULL
                          OR g.MeasuringPointAboveLandSurface IS NOT NULL
                          OR g.RelativeMpElevation IS NOT NULL
                      )
                )
                UPDATE mw
                SET
                    mw.ScreenedIntervalFromFeet = lg.ScreenedIntervalFromFeet,
                    mw.ScreenedIntervalToFeet = lg.ScreenedIntervalToFeet,
                    mw.MeasuringPointAboveLandSurface = lg.MeasuringPointAboveLandSurface,
                    mw.RelativeMpElevation = lg.RelativeMpElevation
                FROM MonitoringWells mw
                INNER JOIN LatestGw lg ON lg.MonitoringWellId = mw.Id AND lg.rn = 1;
                """);

            migrationBuilder.DropColumn(
                name: "MeasuringPointAboveLandSurface",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "RelativeMpElevation",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "ScreenedIntervalFromFeet",
                table: "GWMonits");

            migrationBuilder.DropColumn(
                name: "ScreenedIntervalToFeet",
                table: "GWMonits");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MeasuringPointAboveLandSurface",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RelativeMpElevation",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScreenedIntervalFromFeet",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ScreenedIntervalToFeet",
                table: "GWMonits",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE g
                SET
                    g.ScreenedIntervalFromFeet = mw.ScreenedIntervalFromFeet,
                    g.ScreenedIntervalToFeet = mw.ScreenedIntervalToFeet,
                    g.MeasuringPointAboveLandSurface = mw.MeasuringPointAboveLandSurface,
                    g.RelativeMpElevation = mw.RelativeMpElevation
                FROM GWMonits g
                INNER JOIN MonitoringWells mw ON mw.Id = g.MonitoringWellId
                WHERE g.IsDeleted = 0;
                """);

            migrationBuilder.DropColumn(
                name: "MeasuringPointAboveLandSurface",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "RelativeMpElevation",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "ScreenedIntervalFromFeet",
                table: "MonitoringWells");

            migrationBuilder.DropColumn(
                name: "ScreenedIntervalToFeet",
                table: "MonitoringWells");
        }
    }
}

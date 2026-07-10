using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class BackfillWaterDepthFromLegacyFreeboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Set Edenton lagoon berm height when not configured.
            migrationBuilder.Sql(@"
                UPDATE Facilities
                SET LagoonBermHeightFeet = 11.0
                WHERE Name LIKE '%Edenton%'
                  AND LagoonBermHeightFeet IS NULL;
            ");

            // Backfill water depth from legacy storage lagoon freeboard (StorageFt).
            // WaterDepthFt = LagoonBermHeightFeet - StorageFt when berm height is configured.
            migrationBuilder.Sql(@"
                UPDATE ol
                SET ol.WaterDepthFt = ROUND(f.LagoonBermHeightFeet - ol.StorageFt, 2)
                FROM OperatorLogs ol
                INNER JOIN Facilities f ON f.Id = ol.FacilityId
                WHERE ol.WaterDepthFt IS NULL
                  AND ol.StorageFt IS NOT NULL
                  AND f.LagoonBermHeightFeet IS NOT NULL
                  AND (f.LagoonBermHeightFeet - ol.StorageFt) >= 0;
            ");

            // Verification (not run): SELECT COUNT(*) FROM OperatorLogs ol
            // INNER JOIN Facilities f ON f.Id = ol.FacilityId
            // WHERE ol.WaterDepthFt IS NULL AND ol.StorageFt IS NOT NULL AND f.LagoonBermHeightFeet IS NOT NULL;
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ol
                SET ol.WaterDepthFt = NULL
                FROM OperatorLogs ol
                INNER JOIN Facilities f ON f.Id = ol.FacilityId
                WHERE ol.WaterDepthFt IS NOT NULL
                  AND ol.StorageFt IS NOT NULL
                  AND f.LagoonBermHeightFeet IS NOT NULL
                  AND ol.WaterDepthFt = ROUND(f.LagoonBermHeightFeet - ol.StorageFt, 2);
            ");

            migrationBuilder.Sql(@"
                UPDATE Facilities
                SET LagoonBermHeightFeet = NULL
                WHERE Name LIKE '%Edenton%'
                  AND LagoonBermHeightFeet = 11.0;
            ");
        }
    }
}

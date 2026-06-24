using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class BackfillMonthlyApplicationActualHourlyRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ma
                SET
                    MaximumHourlyLoadingInchesPerAcre = s.ActualHourlyRateInches,
                    VolumeGallons = (s.ActualHourlyRateInches * ma.TimeIrrigatedMinutes / 60.0)
                        * COALESCE(s.AcresTotal, s.SizeAcres) * 27154,
                    Comments = CASE
                        WHEN NULLIF(LTRIM(RTRIM(ma.Comments)), '') IS NULL
                            THEN CONCAT(
                                N'Maximum hourly rate updated from ',
                                FORMAT(ma.MaximumHourlyLoadingInchesPerAcre, '0.00'),
                                N' to ',
                                FORMAT(s.ActualHourlyRateInches, '0.00'),
                                N' in/hr per sprayfield Actual Hourly Rate requirement; volume recalculated accordingly.')
                        ELSE CONCAT(
                                ma.Comments,
                                N' | Maximum hourly rate updated from ',
                                FORMAT(ma.MaximumHourlyLoadingInchesPerAcre, '0.00'),
                                N' to ',
                                FORMAT(s.ActualHourlyRateInches, '0.00'),
                                N' in/hr per sprayfield Actual Hourly Rate requirement; volume recalculated accordingly.')
                    END
                FROM MonthlyApplications ma
                INNER JOIN Sprayfields s ON ma.SprayfieldId = s.Id
                WHERE s.ActualHourlyRateInches IS NOT NULL
                  AND ma.TimeIrrigatedMinutes > 0
                  AND ma.MaximumHourlyLoadingInchesPerAcre <> s.ActualHourlyRateInches
                  AND s.IsDeleted = 0
                  AND COALESCE(s.AcresTotal, s.SizeAcres) > 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}

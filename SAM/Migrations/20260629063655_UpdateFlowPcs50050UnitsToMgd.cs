using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFlowPcs50050UnitsToMgd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE PcsParameterCatalogs
                SET AcceptedUnits = N'MGD'
                WHERE PcsCode = N'50050'
                  AND AcceptedUnits = N'GPD';

                UPDATE fptp
                SET UnitsOverride = N'MGD'
                FROM FacilityPermitTemplateParameters fptp
                INNER JOIN PcsParameterCatalogs pcs ON fptp.PcsParameterCatalogId = pcs.Id
                WHERE pcs.PcsCode = N'50050'
                  AND fptp.UnitsOverride = N'GPD';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE PcsParameterCatalogs
                SET AcceptedUnits = N'GPD'
                WHERE PcsCode = N'50050'
                  AND AcceptedUnits = N'MGD';

                UPDATE fptp
                SET UnitsOverride = N'GPD'
                FROM FacilityPermitTemplateParameters fptp
                INNER JOIN PcsParameterCatalogs pcs ON fptp.PcsParameterCatalogId = pcs.Id
                WHERE pcs.PcsCode = N'50050'
                  AND fptp.UnitsOverride = N'MGD';
            ");
        }
    }
}

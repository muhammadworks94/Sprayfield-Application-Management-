using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SAM.Migrations
{
    /// <inheritdoc />
    public partial class AddOperatorLogOrcOnSiteCanonical : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ORCOnSite",
                table: "OperatorLogs",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID('tempdb..#WwCharDaily') IS NOT NULL DROP TABLE #WwCharDaily;
                IF OBJECT_ID('tempdb..#NdarDaily') IS NOT NULL DROP TABLE #NdarDaily;

                CREATE TABLE #WwCharDaily
                (
                    CompanyId uniqueidentifier NOT NULL,
                    FacilityId uniqueidentifier NOT NULL,
                    LogDate date NOT NULL,
                    ORCOnSite int NULL,
                    StorageFt decimal(10,2) NULL
                );

                CREATE TABLE #NdarDaily
                (
                    CompanyId uniqueidentifier NOT NULL,
                    FacilityId uniqueidentifier NOT NULL,
                    LogDate date NOT NULL,
                    StorageFt decimal(10,2) NULL
                );

                INSERT INTO #WwCharDaily (CompanyId, FacilityId, LogDate, ORCOnSite, StorageFt)
                SELECT
                    w.CompanyId,
                    w.FacilityId,
                    TRY_CONVERT(date, CONCAT(w.[Year], '-', RIGHT(CONCAT('0', w.[Month]), 2), '-', RIGHT(CONCAT('0', d.[key] + 1), 2))) AS LogDate,
                    CASE JSON_VALUE(w.ORCOnSite, CONCAT('$[', d.[key], ']')) COLLATE DATABASE_DEFAULT
                        WHEN 'Y' THEN 0
                        WHEN 'N' THEN 1
                        ELSE NULL
                    END AS ORCOnSite,
                    TRY_CAST(JSON_VALUE(w.LagoonFreeboard, CONCAT('$[', d.[key], ']')) AS decimal(10,2)) AS StorageFt
                FROM WWChars w
                CROSS APPLY OPENJSON(w.LagoonFreeboard) d
                WHERE d.[key] BETWEEN 0 AND 30
                  AND TRY_CONVERT(date, CONCAT(w.[Year], '-', RIGHT(CONCAT('0', w.[Month]), 2), '-', RIGHT(CONCAT('0', d.[key] + 1), 2))) IS NOT NULL;

                INSERT INTO #NdarDaily (CompanyId, FacilityId, LogDate, StorageFt)
                SELECT
                    n.CompanyId,
                    n.FacilityId,
                    TRY_CONVERT(date, CONCAT(n.[Year], '-', RIGHT(CONCAT('0', n.[Month]), 2), '-', RIGHT(CONCAT('0', d.[key] + 1), 2))) AS LogDate,
                    TRY_CAST(d.[value] AS decimal(10,2)) AS StorageFt
                FROM NDAR1s n
                CROSS APPLY OPENJSON(n.StorageDaily) d
                WHERE d.[key] BETWEEN 0 AND 30
                  AND TRY_CONVERT(date, CONCAT(n.[Year], '-', RIGHT(CONCAT('0', n.[Month]), 2), '-', RIGHT(CONCAT('0', d.[key] + 1), 2))) IS NOT NULL;

                UPDATE o
                SET
                    o.ORCOnSite = COALESCE(o.ORCOnSite, w.ORCOnSite),
                    o.StorageFt = COALESCE(o.StorageFt, w.StorageFt)
                FROM OperatorLogs o
                INNER JOIN #WwCharDaily w
                    ON o.FacilityId = w.FacilityId AND CAST(o.LogDate AS date) = CAST(w.LogDate AS date)
                WHERE w.ORCOnSite IS NOT NULL OR w.StorageFt IS NOT NULL;

                UPDATE o
                SET
                    o.StorageFt = COALESCE(o.StorageFt, n.StorageFt)
                FROM OperatorLogs o
                INNER JOIN #NdarDaily n
                    ON o.FacilityId = n.FacilityId AND CAST(o.LogDate AS date) = CAST(n.LogDate AS date)
                WHERE n.StorageFt IS NOT NULL;

                INSERT INTO OperatorLogs
                (
                    Id, CompanyId, FacilityId, LogDate, OperatorName, WeatherConditions,
                    TemperatureF, PrecipitationIn, ORCOnSite, StorageFt, FiveDayUpsetFt,
                    ArrivalTime, TimeOnSiteHours, MaintenancePerformed, EquipmentInspected,
                    IssuesNoted, CorrectiveActions, NextShiftNotes, IsDeleted, CreatedDate, CreatedBy
                )
                SELECT
                    NEWID(),
                    w.CompanyId,
                    w.FacilityId,
                    CAST(w.LogDate AS datetime2),
                    'System',
                    '',
                    NULL, NULL, w.ORCOnSite, w.StorageFt, NULL,
                    CAST('00:00:00' AS time),
                    0,
                    '',
                    '',
                    '',
                    '',
                    '',
                    0,
                    SYSUTCDATETIME(),
                    'migration'
                FROM #WwCharDaily w
                WHERE (w.ORCOnSite IS NOT NULL OR w.StorageFt IS NOT NULL)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM OperatorLogs o
                      WHERE o.FacilityId = w.FacilityId AND CAST(o.LogDate AS date) = CAST(w.LogDate AS date)
                  );

                INSERT INTO OperatorLogs
                (
                    Id, CompanyId, FacilityId, LogDate, OperatorName, WeatherConditions,
                    TemperatureF, PrecipitationIn, ORCOnSite, StorageFt, FiveDayUpsetFt,
                    ArrivalTime, TimeOnSiteHours, MaintenancePerformed, EquipmentInspected,
                    IssuesNoted, CorrectiveActions, NextShiftNotes, IsDeleted, CreatedDate, CreatedBy
                )
                SELECT
                    NEWID(),
                    n.CompanyId,
                    n.FacilityId,
                    CAST(n.LogDate AS datetime2),
                    'System',
                    '',
                    NULL, NULL, NULL, n.StorageFt, NULL,
                    CAST('00:00:00' AS time),
                    0,
                    '',
                    '',
                    '',
                    '',
                    '',
                    0,
                    SYSUTCDATETIME(),
                    'migration'
                FROM #NdarDaily n
                WHERE n.StorageFt IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM OperatorLogs o
                      WHERE o.FacilityId = n.FacilityId AND CAST(o.LogDate AS date) = CAST(n.LogDate AS date)
                  );

                DROP TABLE #WwCharDaily;
                DROP TABLE #NdarDaily;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ORCOnSite",
                table: "OperatorLogs");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <summary>
    /// RuleConfigs: LifecycleStage (string) → OperationalMode (int). Idempotent for databases that already applied Phase 36 outside EF history.
    /// </summary>
    public partial class Phase37_OperationalMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.RuleConfigs', 'LifecycleStage') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[RuleConfigs] DROP COLUMN [LifecycleStage];
                END
                IF COL_LENGTH('dbo.RuleConfigs', 'OperationalMode') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[RuleConfigs] ADD [OperationalMode] int NOT NULL DEFAULT 0;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('dbo.RuleConfigs', 'OperationalMode') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[RuleConfigs] DROP COLUMN [OperationalMode];
                END
                IF COL_LENGTH('dbo.RuleConfigs', 'LifecycleStage') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[RuleConfigs] ADD [LifecycleStage] nvarchar(max) NOT NULL DEFAULT (N'');
                END
                """);
        }
    }
}

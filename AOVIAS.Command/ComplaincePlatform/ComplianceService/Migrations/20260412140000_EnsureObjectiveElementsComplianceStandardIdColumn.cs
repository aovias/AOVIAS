using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <summary>
/// Idempotent fix for databases where migration Phase36_ComplianceStandardRename did not run
/// (e.g. missing from __EFMigrationsHistory) but the model expects
/// <c>ObjectiveElements.ComplianceStandardId</c> and <c>ComplianceStandards</c>.
/// Safe to apply when Phase36 already applied (all IF blocks no-op).
/// </summary>
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260412140000_EnsureObjectiveElementsComplianceStandardIdColumn")]
public class EnsureObjectiveElementsComplianceStandardIdColumn : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Duplicate of Phase36_ComplianceStandardRename — keep in sync if schema rules change.
        migrationBuilder.Sql(
            """
            IF COL_LENGTH('dbo.ObjectiveElements', 'StandardId') IS NOT NULL
               AND COL_LENGTH('dbo.ObjectiveElements', 'ComplianceStandardId') IS NULL
            BEGIN
                EXEC sp_rename 'dbo.ObjectiveElements.StandardId', 'ComplianceStandardId', 'COLUMN';
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[IX_ObjectiveElements_StandardId_Code]', N'INDEX') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[IX_ObjectiveElements_ComplianceStandardId_Code]', N'INDEX') IS NULL
            BEGIN
                EXEC sp_rename
                    N'[dbo].[ObjectiveElements].[IX_ObjectiveElements_StandardId_Code]',
                    N'IX_ObjectiveElements_ComplianceStandardId_Code',
                    N'INDEX';
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[FK_ObjectiveElements_Standards_StandardId]', N'F') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[ObjectiveElements] DROP CONSTRAINT [FK_ObjectiveElements_Standards_StandardId];
            END

            IF OBJECT_ID(N'[dbo].[FK_ObjectiveElements_ComplianceStandards_ComplianceStandardId]', N'F') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[ObjectiveElements] DROP CONSTRAINT [FK_ObjectiveElements_ComplianceStandards_ComplianceStandardId];
            END

            IF OBJECT_ID(N'[dbo].[FK_ObjectiveElements_Standards_ComplianceStandardId]', N'F') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[ObjectiveElements] DROP CONSTRAINT [FK_ObjectiveElements_Standards_ComplianceStandardId];
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Standards]', N'U') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[ComplianceStandards]', N'U') IS NULL
            BEGIN
                EXEC sp_rename 'dbo.Standards', 'ComplianceStandards';
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[PK_Standards]', N'PK') IS NOT NULL
            BEGIN
                EXEC sp_rename N'[dbo].[PK_Standards]', N'PK_ComplianceStandards', N'OBJECT';
            END
            """);

        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[FK_ObjectiveElements_ComplianceStandards_ComplianceStandardId]', N'F') IS NULL
               AND COL_LENGTH('dbo.ObjectiveElements', 'ComplianceStandardId') IS NOT NULL
               AND OBJECT_ID(N'[dbo].[ComplianceStandards]', N'U') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[ObjectiveElements] WITH CHECK
                ADD CONSTRAINT [FK_ObjectiveElements_ComplianceStandards_ComplianceStandardId]
                FOREIGN KEY([ComplianceStandardId]) REFERENCES [dbo].[ComplianceStandards] ([Id]) ON DELETE NO ACTION;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Non-reversible idempotent migration; use Phase36 Down if full rollback is required.
    }
}

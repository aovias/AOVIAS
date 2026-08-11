using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <summary>Adds persisted <c>OperationalMode</c> to compliance journeys (spec v2 session / PATCH).</summary>
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260411120000_AddComplianceJourneyOperationalMode")]
public class AddComplianceJourneyOperationalMode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH('dbo.ComplianceJourneys', 'OperationalMode') IS NULL
            BEGIN
                ALTER TABLE [dbo].[ComplianceJourneys] ADD [OperationalMode] int NOT NULL DEFAULT 0;
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH('dbo.ComplianceJourneys', 'OperationalMode') IS NOT NULL
            BEGIN
                ALTER TABLE [dbo].[ComplianceJourneys] DROP COLUMN [OperationalMode];
            END
            """);
    }
}

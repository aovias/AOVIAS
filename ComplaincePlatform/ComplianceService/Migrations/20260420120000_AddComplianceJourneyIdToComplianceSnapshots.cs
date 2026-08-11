using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <summary>
/// Adds ComplianceJourneyId to ComplianceSnapshots for journey-centric read model alignment.
/// </summary>
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260420120000_AddComplianceJourneyIdToComplianceSnapshots")]
public class AddComplianceJourneyIdToComplianceSnapshots : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "ComplianceJourneyId",
            table: "ComplianceSnapshots",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceSnapshots_ComplianceJourneyId",
            table: "ComplianceSnapshots",
            column: "ComplianceJourneyId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ComplianceSnapshots_ComplianceJourneyId",
            table: "ComplianceSnapshots");

        migrationBuilder.DropColumn(
            name: "ComplianceJourneyId",
            table: "ComplianceSnapshots");
    }
}

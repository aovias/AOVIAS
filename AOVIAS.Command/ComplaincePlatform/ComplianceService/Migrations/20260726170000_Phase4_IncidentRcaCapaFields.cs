using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <summary>Phase 4 Sprint 3 — persist RCA corrective/preventive + category for deficiency CAPA promotion.</summary>
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726170000_Phase4_IncidentRcaCapaFields")]
public partial class Phase4_IncidentRcaCapaFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CorrectiveActionSummary",
            table: "IncidentRcas",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PreventiveActionSummary",
            table: "IncidentRcas",
            type: "nvarchar(4000)",
            maxLength: 4000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RootCauseCategory",
            table: "IncidentRcas",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "CorrectiveActionSummary", table: "IncidentRcas");
        migrationBuilder.DropColumn(name: "PreventiveActionSummary", table: "IncidentRcas");
        migrationBuilder.DropColumn(name: "RootCauseCategory", table: "IncidentRcas");
    }
}

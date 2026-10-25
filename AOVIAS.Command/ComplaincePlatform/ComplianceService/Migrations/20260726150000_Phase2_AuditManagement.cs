using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726150000_Phase2_AuditManagement")]
public partial class Phase2_AuditManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ChapterCode",
            table: "AuditObservations",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OeCode",
            table: "AuditObservations",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditObservations_ChapterCode_OeCode",
            table: "AuditObservations",
            columns: new[] { "ChapterCode", "OeCode" });

        migrationBuilder.AddColumn<Guid>(
            name: "ComplianceProgramId",
            table: "AuditSessions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ScheduledAuditId",
            table: "AuditSessions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_AuditSessions_ComplianceProgramId",
            table: "AuditSessions",
            column: "ComplianceProgramId");

        migrationBuilder.CreateIndex(
            name: "IX_AuditSessions_ScheduledAuditId",
            table: "AuditSessions",
            column: "ScheduledAuditId");

        migrationBuilder.AddColumn<int>(
            name: "AuditType",
            table: "ScheduledAudits",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProgramName",
            table: "ScheduledAudits",
            type: "nvarchar(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ProgramMonth",
            table: "ScheduledAudits",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ProgramYear",
            table: "ScheduledAudits",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ScheduledAudits_OrganizationId_ProgramYear_ProgramMonth",
            table: "ScheduledAudits",
            columns: new[] { "OrganizationId", "ProgramYear", "ProgramMonth" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ScheduledAudits_OrganizationId_ProgramYear_ProgramMonth",
            table: "ScheduledAudits");

        migrationBuilder.DropColumn(name: "AuditType", table: "ScheduledAudits");
        migrationBuilder.DropColumn(name: "ProgramName", table: "ScheduledAudits");
        migrationBuilder.DropColumn(name: "ProgramMonth", table: "ScheduledAudits");
        migrationBuilder.DropColumn(name: "ProgramYear", table: "ScheduledAudits");

        migrationBuilder.DropIndex(name: "IX_AuditSessions_ComplianceProgramId", table: "AuditSessions");
        migrationBuilder.DropIndex(name: "IX_AuditSessions_ScheduledAuditId", table: "AuditSessions");
        migrationBuilder.DropColumn(name: "ComplianceProgramId", table: "AuditSessions");
        migrationBuilder.DropColumn(name: "ScheduledAuditId", table: "AuditSessions");

        migrationBuilder.DropIndex(name: "IX_AuditObservations_ChapterCode_OeCode", table: "AuditObservations");
        migrationBuilder.DropColumn(name: "ChapterCode", table: "AuditObservations");
        migrationBuilder.DropColumn(name: "OeCode", table: "AuditObservations");
    }
}

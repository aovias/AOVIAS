using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726140000_Phase2_CapaLifecycle")]
public partial class Phase2_CapaLifecycle : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CapaNumber",
            table: "ComplianceDeficiencies",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "OwnerAcceptedAt",
            table: "ComplianceDeficiencies",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OwnerAcceptedBy",
            table: "ComplianceDeficiencies",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "OwnerAcceptanceRejected",
            table: "ComplianceDeficiencies",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "OwnerRejectedAt",
            table: "ComplianceDeficiencies",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OwnerRejectedBy",
            table: "ComplianceDeficiencies",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OwnershipRejectionReason",
            table: "ComplianceDeficiencies",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LinkedSourceType",
            table: "ComplianceDeficiencies",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        // Keep existing UniqueOpenPerObligationType filter ([Status] <> 6/7).
        // SQL Server filtered indexes disallow OR/ISNULL, so OriginType exclusion for AuditFinding
        // cannot be expressed here; uniqueness for audit-origin CAPAs is enforced in application code.

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceDeficiencies_OrganizationId_CapaNumber",
            table: "ComplianceDeficiencies",
            columns: new[] { "OrganizationId", "CapaNumber" },
            unique: true,
            filter: "[CapaNumber] IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ComplianceDeficiencies_OrganizationId_CapaNumber",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "CapaNumber",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "OwnerAcceptedAt",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "OwnerAcceptedBy",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "OwnerAcceptanceRejected",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "OwnerRejectedAt",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "OwnerRejectedBy",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "OwnershipRejectionReason",
            table: "ComplianceDeficiencies");

        migrationBuilder.DropColumn(
            name: "LinkedSourceType",
            table: "ComplianceDeficiencies");
    }
}

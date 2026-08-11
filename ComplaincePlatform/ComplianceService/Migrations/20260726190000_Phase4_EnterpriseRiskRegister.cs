using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726190000_Phase4_EnterpriseRiskRegister")]
public partial class Phase4_EnterpriseRiskRegister : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Remap legacy statuses before expanding the table:
        // Open(1) stays Identified(1); Mitigated(2)→Mitigating(3); Closed(3)→Closed(5).
        migrationBuilder.Sql("""
            UPDATE [Risks] SET [Status] = 105 WHERE [Status] = 3;
            UPDATE [Risks] SET [Status] = 3 WHERE [Status] = 2;
            UPDATE [Risks] SET [Status] = 5 WHERE [Status] = 105;
            """);

        migrationBuilder.DropCheckConstraint(
            name: "CK_Risk_ReferenceRequired",
            table: "Risks");

        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "Risks",
            type: "uniqueidentifier",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

        // Best-effort: seed TenantId from HospitalId when org linkage is unavailable in this DB.
        migrationBuilder.Sql("""
            UPDATE [Risks] SET [TenantId] = [HospitalId] WHERE [TenantId] = '00000000-0000-0000-0000-000000000000';
            """);

        migrationBuilder.AddColumn<string>(
            name: "Category",
            table: "Risks",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "OwnerUserId",
            table: "Risks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "OwnerName",
            table: "Risks",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "MitigationPlan",
            table: "Risks",
            type: "nvarchar(max)",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ResidualLikelihood",
            table: "Risks",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "ResidualImpact",
            table: "Risks",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ReviewDueDate",
            table: "Risks",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DepartmentId",
            table: "Risks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "IncidentId",
            table: "Risks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DeficiencyId",
            table: "Risks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "LastUpdatedAt",
            table: "Risks",
            type: "datetime2",
            nullable: false,
            defaultValueSql: "GETUTCDATE()");

        migrationBuilder.CreateIndex(
            name: "IX_Risks_TenantId",
            table: "Risks",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_Risks_ReviewDueDate",
            table: "Risks",
            column: "ReviewDueDate");

        migrationBuilder.CreateIndex(
            name: "IX_Risks_OwnerUserId",
            table: "Risks",
            column: "OwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Risks_DepartmentId",
            table: "Risks",
            column: "DepartmentId");

        migrationBuilder.CreateIndex(
            name: "IX_Risks_IncidentId",
            table: "Risks",
            column: "IncidentId");

        migrationBuilder.CreateIndex(
            name: "IX_Risks_DeficiencyId",
            table: "Risks",
            column: "DeficiencyId");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Risk_ResidualLikelihoodRange",
            table: "Risks",
            sql: "[ResidualLikelihood] IS NULL OR ([ResidualLikelihood] >= 1 AND [ResidualLikelihood] <= 5)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Risk_ResidualImpactRange",
            table: "Risks",
            sql: "[ResidualImpact] IS NULL OR ([ResidualImpact] >= 1 AND [ResidualImpact] <= 5)");

        migrationBuilder.AddForeignKey(
            name: "FK_Risks_Incidents_IncidentId",
            table: "Risks",
            column: "IncidentId",
            principalTable: "Incidents",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);

        migrationBuilder.AddForeignKey(
            name: "FK_Risks_ComplianceDeficiencies_DeficiencyId",
            table: "Risks",
            column: "DeficiencyId",
            principalTable: "ComplianceDeficiencies",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Risks_Incidents_IncidentId",
            table: "Risks");

        migrationBuilder.DropForeignKey(
            name: "FK_Risks_ComplianceDeficiencies_DeficiencyId",
            table: "Risks");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Risk_ResidualLikelihoodRange",
            table: "Risks");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Risk_ResidualImpactRange",
            table: "Risks");

        migrationBuilder.DropIndex(name: "IX_Risks_TenantId", table: "Risks");
        migrationBuilder.DropIndex(name: "IX_Risks_ReviewDueDate", table: "Risks");
        migrationBuilder.DropIndex(name: "IX_Risks_OwnerUserId", table: "Risks");
        migrationBuilder.DropIndex(name: "IX_Risks_DepartmentId", table: "Risks");
        migrationBuilder.DropIndex(name: "IX_Risks_IncidentId", table: "Risks");
        migrationBuilder.DropIndex(name: "IX_Risks_DeficiencyId", table: "Risks");

        migrationBuilder.DropColumn(name: "TenantId", table: "Risks");
        migrationBuilder.DropColumn(name: "Category", table: "Risks");
        migrationBuilder.DropColumn(name: "OwnerUserId", table: "Risks");
        migrationBuilder.DropColumn(name: "OwnerName", table: "Risks");
        migrationBuilder.DropColumn(name: "MitigationPlan", table: "Risks");
        migrationBuilder.DropColumn(name: "ResidualLikelihood", table: "Risks");
        migrationBuilder.DropColumn(name: "ResidualImpact", table: "Risks");
        migrationBuilder.DropColumn(name: "ReviewDueDate", table: "Risks");
        migrationBuilder.DropColumn(name: "DepartmentId", table: "Risks");
        migrationBuilder.DropColumn(name: "IncidentId", table: "Risks");
        migrationBuilder.DropColumn(name: "DeficiencyId", table: "Risks");
        migrationBuilder.DropColumn(name: "LastUpdatedAt", table: "Risks");

        migrationBuilder.Sql("""
            UPDATE [Risks] SET [Status] = 103 WHERE [Status] = 5;
            UPDATE [Risks] SET [Status] = 2 WHERE [Status] = 3;
            UPDATE [Risks] SET [Status] = 3 WHERE [Status] = 103;
            UPDATE [Risks] SET [Status] = 1 WHERE [Status] IN (1, 2, 4);
            """);

        migrationBuilder.AddCheckConstraint(
            name: "CK_Risk_ReferenceRequired",
            table: "Risks",
            sql: "([ObjectiveElementId] IS NOT NULL) OR ([TaskId] IS NOT NULL) OR ([FindingId] IS NOT NULL)");
    }
}

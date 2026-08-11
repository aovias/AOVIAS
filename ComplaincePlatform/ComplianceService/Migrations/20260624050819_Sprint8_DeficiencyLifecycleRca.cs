using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint8_DeficiencyLifecycleRca : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComplianceDeficiencies_UniqueOpenPerObligationType",
                table: "ComplianceDeficiencies");

            migrationBuilder.AddColumn<string>(
                name: "ActionPlan",
                table: "ComplianceDeficiencies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ActualResolutionDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssessedRiskLevel",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveActionSummary",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "ComplianceDeficiencies",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectivenessReviewStatus",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedOutcome",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinalRootCause",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactDescription",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreventiveActionSummary",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProblemStatement",
                table: "ComplianceDeficiencies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RemediationTargetCompletion",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskImpact",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskLikelihood",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskScore",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RootCauseSummary",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuccessCriteria",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetResolutionDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Why1",
                table: "ComplianceDeficiencies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Why2",
                table: "ComplianceDeficiencies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Why3",
                table: "ComplianceDeficiencies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Why4",
                table: "ComplianceDeficiencies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Why5",
                table: "ComplianceDeficiencies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeficiencyAuditTrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeficiencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DetailJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeficiencyAuditTrails", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDeficiencies_UniqueOpenPerObligationType",
                table: "ComplianceDeficiencies",
                columns: new[] { "OrganizationId", "ComplianceObligationId", "DeficiencyType" },
                filter: "[Status] <> 6 AND [Status] <> 7");

            migrationBuilder.CreateIndex(
                name: "IX_DeficiencyAuditTrails_DeficiencyId_OccurredAt",
                table: "DeficiencyAuditTrails",
                columns: new[] { "DeficiencyId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeficiencyAuditTrails");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceDeficiencies_UniqueOpenPerObligationType",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "ActionPlan",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "ActualResolutionDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "AssessedRiskLevel",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "AssignedDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionSummary",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewStatus",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "ExpectedOutcome",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "FinalRootCause",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "ImpactDescription",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PreventiveActionSummary",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "ProblemStatement",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RemediationTargetCompletion",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RiskImpact",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RiskLikelihood",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RootCauseSummary",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "SuccessCriteria",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "TargetResolutionDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Why1",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Why2",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Why3",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Why4",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "Why5",
                table: "ComplianceDeficiencies");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDeficiencies_UniqueOpenPerObligationType",
                table: "ComplianceDeficiencies",
                columns: new[] { "OrganizationId", "ComplianceObligationId", "DeficiencyType" },
                filter: "[Status] <> 6");
        }
    }
}

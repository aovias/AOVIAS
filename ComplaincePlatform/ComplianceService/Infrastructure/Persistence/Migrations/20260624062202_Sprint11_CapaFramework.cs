using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Sprint11_CapaFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectiveActionCompletedDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrectiveActionOwner",
                table: "ComplianceDeficiencies",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectiveActionStartDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorrectiveActionStatus",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CorrectiveActionTargetDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectivenessReviewDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EffectivenessReviewDaysOffset",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectivenessReviewNotes",
                table: "ComplianceDeficiencies",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectivenessReviewOutcome",
                table: "ComplianceDeficiencies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectivenessReviewScheduledDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EffectivenessReviewer",
                table: "ComplianceDeficiencies",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRecurringIssue",
                table: "ComplianceDeficiencies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreventiveActionCompletedDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreventiveActionOwner",
                table: "ComplianceDeficiencies",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PreventiveActionStatus",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PreventiveActionSuccessCriteria",
                table: "ComplianceDeficiencies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PreventiveActionTargetDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecurrenceDetectedAt",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecurrenceReason",
                table: "ComplianceDeficiencies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDeficiencies_IsRecurringIssue",
                table: "ComplianceDeficiencies",
                column: "IsRecurringIssue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComplianceDeficiencies_IsRecurringIssue",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionCompletedDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionOwner",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionStartDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionStatus",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CorrectiveActionTargetDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewDaysOffset",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewNotes",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewOutcome",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewScheduledDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "EffectivenessReviewer",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "IsRecurringIssue",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PreventiveActionCompletedDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PreventiveActionOwner",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PreventiveActionStatus",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PreventiveActionSuccessCriteria",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PreventiveActionTargetDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RecurrenceDetectedAt",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "RecurrenceReason",
                table: "ComplianceDeficiencies");
        }
    }
}

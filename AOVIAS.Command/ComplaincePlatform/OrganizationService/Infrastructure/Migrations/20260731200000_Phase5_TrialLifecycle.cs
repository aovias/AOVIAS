using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrganizationService.Infrastructure.Persistence;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20260731200000_Phase5_TrialLifecycle")]
public partial class Phase5_TrialLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "TrialLifecycleNotificationLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LicenseKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Milestone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Recipient = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_TrialLifecycleNotificationLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_TrialLifecycleNotificationLogs_LicenseKeyId_Milestone",
            table: "TrialLifecycleNotificationLogs",
            columns: new[] { "LicenseKeyId", "Milestone" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_TrialLifecycleNotificationLogs_OrganizationId_SentAtUtc",
            table: "TrialLifecycleNotificationLogs",
            columns: new[] { "OrganizationId", "SentAtUtc" });

        migrationBuilder.CreateTable(
            name: "TrialLifecycleRuns",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Trigger = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                FinishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ProcessedCount = table.Column<int>(type: "int", nullable: false),
                TransitionCount = table.Column<int>(type: "int", nullable: false),
                ReminderCount = table.Column<int>(type: "int", nullable: false),
                FailureCount = table.Column<int>(type: "int", nullable: false),
                FailedLicenseKeyIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ErrorSummary = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_TrialLifecycleRuns", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_TrialLifecycleRuns_StartedAtUtc",
            table: "TrialLifecycleRuns",
            column: "StartedAtUtc");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TrialLifecycleNotificationLogs");
        migrationBuilder.DropTable(name: "TrialLifecycleRuns");
    }
}

using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrganizationService.Infrastructure.Persistence;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20260731180000_Phase3_OnboardingTelemetry")]
public partial class Phase3_OnboardingTelemetry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OnboardingTelemetryEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Stage = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_OnboardingTelemetryEvents", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_OnboardingTelemetryEvents_OrganizationId_OccurredAtUtc",
            table: "OnboardingTelemetryEvents",
            columns: new[] { "OrganizationId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_OnboardingTelemetryEvents_EventType_OccurredAtUtc",
            table: "OnboardingTelemetryEvents",
            columns: new[] { "EventType", "OccurredAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OnboardingTelemetryEvents");
    }
}

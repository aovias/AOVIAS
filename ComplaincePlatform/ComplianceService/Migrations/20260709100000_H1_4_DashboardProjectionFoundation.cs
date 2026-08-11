using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// INF-01: these two attributes were missing, which made the migration
    /// invisible to EF Core — never applied, never reported as pending. A fresh
    /// database therefore never gained <c>DashboardProjections</c>. The dev
    /// database has the table only because it was created out-of-band.
    /// </remarks>
    [DbContext(typeof(ComplianceDbContext))]
    [Migration("20260709100000_H1_4_DashboardProjectionFoundation")]
    public partial class H1_4_DashboardProjectionFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardProjections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectionSchemaVersion = table.Column<int>(type: "int", nullable: false),
                    DataVersion = table.Column<long>(type: "bigint", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    IsCorrupted = table.Column<bool>(type: "bit", nullable: false),
                    CorruptionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OverallComplianceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AuditConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ActiveJourneysCount = table.Column<int>(type: "int", nullable: false),
                    TotalJourneysCount = table.Column<int>(type: "int", nullable: false),
                    PendingTasksCount = table.Column<int>(type: "int", nullable: false),
                    OverdueTasksCount = table.Column<int>(type: "int", nullable: false),
                    OpenAlertsCount = table.Column<int>(type: "int", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScoreCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastEventProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastProcessedOutboxMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardProjections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardProjections_IsComplete_IsCorrupted",
                table: "DashboardProjections",
                columns: new[] { "IsComplete", "IsCorrupted" });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardProjections_LastUpdatedAt",
                table: "DashboardProjections",
                column: "LastUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardProjections_OrganizationId_HospitalId_FrameworkId",
                table: "DashboardProjections",
                columns: new[] { "OrganizationId", "HospitalId", "FrameworkId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardProjections");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint12B1_AnalyticsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AnalyticsDailySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ComplianceMaturityIndex = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OpenDeficiencies = table.Column<int>(type: "int", nullable: false),
                    CriticalDeficiencies = table.Column<int>(type: "int", nullable: false),
                    CapaOpen = table.Column<int>(type: "int", nullable: false),
                    CapaEffective = table.Column<int>(type: "int", nullable: false),
                    CapaIneffective = table.Column<int>(type: "int", nullable: false),
                    EscalationsOpen = table.Column<int>(type: "int", nullable: false),
                    SlaBreached = table.Column<int>(type: "int", nullable: false),
                    StageUnlockedCount = table.Column<int>(type: "int", nullable: false),
                    RecurringIssues = table.Column<int>(type: "int", nullable: false),
                    EvidencePendingVerification = table.Column<int>(type: "int", nullable: false),
                    TaskBacklog = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsDailySnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailySnapshots_OrganizationId_HospitalId_FrameworkId_SnapshotDate",
                table: "AnalyticsDailySnapshots",
                columns: new[] { "OrganizationId", "HospitalId", "FrameworkId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailySnapshots_SnapshotDate",
                table: "AnalyticsDailySnapshots",
                column: "SnapshotDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsDailySnapshots");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGapAnalysisTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GapAnalysisReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OverallPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MeetsOverallPercent = table.Column<bool>(type: "bit", nullable: false),
                    CoreRulePassed = table.Column<bool>(type: "bit", nullable: false),
                    ChapterAveragesPassed = table.Column<bool>(type: "bit", nullable: false),
                    StandardAveragesPassed = table.Column<bool>(type: "bit", nullable: false),
                    CommitmentLevelPassed = table.Column<bool>(type: "bit", nullable: false),
                    CriticalCount = table.Column<int>(type: "int", nullable: false),
                    MajorCount = table.Column<int>(type: "int", nullable: false),
                    MinorCount = table.Column<int>(type: "int", nullable: false),
                    CompliantCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapAnalysisReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GapAnalysisReports_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GapAnalysisItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReportId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeCode = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    NodeTitle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StandardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GapAnalysisItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GapAnalysisItems_ComplianceNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GapAnalysisItems_GapAnalysisReports_ReportId",
                        column: x => x.ReportId,
                        principalTable: "GapAnalysisReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisItems_NodeId",
                table: "GapAnalysisItems",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisItems_ReportId_NodeId",
                table: "GapAnalysisItems",
                columns: new[] { "ReportId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisReports_AssessmentId",
                table: "GapAnalysisReports",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisReports_FacilityId",
                table: "GapAnalysisReports",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_GapAnalysisReports_FrameworkId",
                table: "GapAnalysisReports",
                column: "FrameworkId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GapAnalysisItems");

            migrationBuilder.DropTable(
                name: "GapAnalysisReports");
        }
    }
}

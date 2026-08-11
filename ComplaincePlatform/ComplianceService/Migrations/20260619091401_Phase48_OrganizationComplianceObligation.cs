using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase48_OrganizationComplianceObligation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationComplianceObligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MaturityLevel = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    RiskCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", nullable: false, defaultValue: 1.0m),
                    ImplementationStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    AssessmentStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    JourneyStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    EvidenceStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    VerificationStatus = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ReadinessScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    CompletionPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    LastAssessmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEvidenceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastVerificationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSnapshotRefreshDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationComplianceObligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationComplianceObligations_ComplianceFrameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "ComplianceFrameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrganizationComplianceObligations_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceObligations_FrameworkId",
                table: "OrganizationComplianceObligations",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceObligations_ObjectiveElementId",
                table: "OrganizationComplianceObligations",
                column: "ObjectiveElementId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceObligations_Org_Framework_Maturity",
                table: "OrganizationComplianceObligations",
                columns: new[] { "OrganizationId", "FrameworkId", "MaturityLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceObligations_Org_ImplementationStatus",
                table: "OrganizationComplianceObligations",
                columns: new[] { "OrganizationId", "ImplementationStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceObligations_Tenant_Hospital_Framework",
                table: "OrganizationComplianceObligations",
                columns: new[] { "TenantId", "HospitalId", "FrameworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationComplianceObligations_TenantId",
                table: "OrganizationComplianceObligations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_OrganizationComplianceObligations_OrgOE",
                table: "OrganizationComplianceObligations",
                columns: new[] { "OrganizationId", "ObjectiveElementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationComplianceObligations");
        }
    }
}

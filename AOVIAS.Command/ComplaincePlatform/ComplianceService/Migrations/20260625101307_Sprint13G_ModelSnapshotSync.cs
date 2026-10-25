using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint13G_ModelSnapshotSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftType = table.Column<int>(type: "int", nullable: false),
                    SourceType = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Persona = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false, defaultValue: ""),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", nullable: false, defaultValue: 1.0m),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiDrafts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotActionProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DraftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposalType = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true, defaultValue: ""),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: true, defaultValue: "{}"),
                    ProposedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Copilot"),
                    ProposedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResultEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExecutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotActionProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotActivityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Persona = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotActivityEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotEffectivenessSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecommendationsGenerated = table.Column<int>(type: "int", nullable: false),
                    RecommendationsViewed = table.Column<int>(type: "int", nullable: false),
                    RecommendationsAccepted = table.Column<int>(type: "int", nullable: false),
                    RecommendationsDismissed = table.Column<int>(type: "int", nullable: false),
                    DraftsGenerated = table.Column<int>(type: "int", nullable: false),
                    DraftsRegenerated = table.Column<int>(type: "int", nullable: false),
                    ProposalsCreated = table.Column<int>(type: "int", nullable: false),
                    ProposalsApproved = table.Column<int>(type: "int", nullable: false),
                    ProposalsRejected = table.Column<int>(type: "int", nullable: false),
                    ProposalsExecuted = table.Column<int>(type: "int", nullable: false),
                    PersonaSwitches = table.Column<int>(type: "int", nullable: false),
                    ActiveUsers = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotEffectivenessSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotProposalAuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActorEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotProposalAuditEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotRecommendations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecommendationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Severity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    Persona = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 1.0m),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false, defaultValue: ""),
                    Rationale = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ExpectedOutcome = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RiskIfIgnored = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SourceReferences = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    SourceEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeepLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RationaleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImpactJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcceptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotRecommendations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_CreatedUtc",
                table: "AiDrafts",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_DraftType",
                table: "AiDrafts",
                column: "DraftType");

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_GroupId",
                table: "AiDrafts",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_OrganizationId",
                table: "AiDrafts",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_OrgId_GroupId",
                table: "AiDrafts",
                columns: new[] { "OrganizationId", "GroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_Persona",
                table: "AiDrafts",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_RecommendationId",
                table: "AiDrafts",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_AiDrafts_Status",
                table: "AiDrafts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_ExpiresAtUtc",
                table: "CopilotActionProposals",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_OrganizationId",
                table: "CopilotActionProposals",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_OrgId_Status",
                table: "CopilotActionProposals",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_ProposalType",
                table: "CopilotActionProposals",
                column: "ProposalType");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_RecommendationId",
                table: "CopilotActionProposals",
                column: "RecommendationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_Status",
                table: "CopilotActionProposals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActivityEvents_EventType",
                table: "CopilotActivityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActivityEvents_OccurredAtUtc",
                table: "CopilotActivityEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActivityEvents_OrganizationId",
                table: "CopilotActivityEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotEffectivenessSnapshots_OrganizationId",
                table: "CopilotEffectivenessSnapshots",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotEffectivenessSnapshots_OrganizationId_SnapshotDate",
                table: "CopilotEffectivenessSnapshots",
                columns: new[] { "OrganizationId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CopilotProposalAuditEvents_EventType",
                table: "CopilotProposalAuditEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotProposalAuditEvents_OccurredAtUtc",
                table: "CopilotProposalAuditEvents",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotProposalAuditEvents_OrganizationId",
                table: "CopilotProposalAuditEvents",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotProposalAuditEvents_ProposalId",
                table: "CopilotProposalAuditEvents",
                column: "ProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_Category",
                table: "CopilotRecommendations",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_GeneratedAtUtc",
                table: "CopilotRecommendations",
                column: "GeneratedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_OrganizationId",
                table: "CopilotRecommendations",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_OrgPersonaStatus",
                table: "CopilotRecommendations",
                columns: new[] { "OrganizationId", "Persona", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_Persona",
                table: "CopilotRecommendations",
                column: "Persona");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_Severity",
                table: "CopilotRecommendations",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotRecommendations_Status",
                table: "CopilotRecommendations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiDrafts");

            migrationBuilder.DropTable(
                name: "CopilotActionProposals");

            migrationBuilder.DropTable(
                name: "CopilotActivityEvents");

            migrationBuilder.DropTable(
                name: "CopilotEffectivenessSnapshots");

            migrationBuilder.DropTable(
                name: "CopilotProposalAuditEvents");

            migrationBuilder.DropTable(
                name: "CopilotRecommendations");
        }
    }
}

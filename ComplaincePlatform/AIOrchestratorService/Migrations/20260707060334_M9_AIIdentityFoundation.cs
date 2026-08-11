using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrchestratorService.Migrations
{
    /// <inheritdoc />
    public partial class M9_AIIdentityFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIApprovalPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalType = table.Column<int>(type: "int", nullable: false),
                    GovernanceApprovalChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConfidenceThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RiskLevelThreshold = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIApprovalPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIApprovalRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIAssistantProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CopilotPersonaKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAssistantProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIAutomationPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    BusinessHoursOnly = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceThreshold = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAutomationPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIConversationContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FrameworkCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetentionDays = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversationContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIConversationMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferencedDocumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIConversationMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIExecutionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PromptVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UseCase = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelProvider = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    LatencyMs = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ToolsUsedJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DocumentsAccessedJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    ApprovalStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIExecutionAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIExecutionContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionAuditId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ModuleCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIExecutionContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIIdentities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlatformId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GovernanceOwnerUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LifecycleStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIIdentities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIIdentityScopeAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsibilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIIdentityScopeAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIProfilePermissionGrants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIProfilePermissionGrants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIPromptProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LayerType = table.Column<int>(type: "int", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FrameworkCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentVersion = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIPromptProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIPromptVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PromptProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Locale = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SystemPrompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RolePrompt = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIPromptVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIUsageStatistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModuleCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ExecutionCount = table.Column<int>(type: "int", nullable: false),
                    TotalInputTokens = table.Column<long>(type: "bigint", nullable: false),
                    TotalOutputTokens = table.Column<long>(type: "bigint", nullable: false),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AvgLatencyMs = table.Column<int>(type: "int", nullable: false),
                    AcceptedCount = table.Column<int>(type: "int", nullable: false),
                    RejectedCount = table.Column<int>(type: "int", nullable: false),
                    HumanOverrideCount = table.Column<int>(type: "int", nullable: false),
                    AutomationRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIUsageStatistics", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIApprovalRequests_Status",
                table: "AIApprovalRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AIAssistantProfiles_Code",
                table: "AIAssistantProfiles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIConversationMessages_ConversationId_Sequence",
                table: "AIConversationMessages",
                columns: new[] { "ConversationId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_AIExecutionAudits_ProfileId_OccurredAtUtc",
                table: "AIExecutionAudits",
                columns: new[] { "ProfileId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AIExecutionAudits_TraceId",
                table: "AIExecutionAudits",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_AIIdentities_PlatformId",
                table: "AIIdentities",
                column: "PlatformId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIOutboxMessages_ProcessedOn",
                table: "AIOutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_AIPromptProfiles_Code",
                table: "AIPromptProfiles",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_AIPromptVersions_PromptProfileId_Version",
                table: "AIPromptVersions",
                columns: new[] { "PromptProfileId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIApprovalPolicies");

            migrationBuilder.DropTable(
                name: "AIApprovalRequests");

            migrationBuilder.DropTable(
                name: "AIAssistantProfiles");

            migrationBuilder.DropTable(
                name: "AIAutomationPolicies");

            migrationBuilder.DropTable(
                name: "AIConversationContexts");

            migrationBuilder.DropTable(
                name: "AIConversationMessages");

            migrationBuilder.DropTable(
                name: "AIExecutionAudits");

            migrationBuilder.DropTable(
                name: "AIExecutionContexts");

            migrationBuilder.DropTable(
                name: "AIIdentities");

            migrationBuilder.DropTable(
                name: "AIIdentityScopeAssignments");

            migrationBuilder.DropTable(
                name: "AIOutboxMessages");

            migrationBuilder.DropTable(
                name: "AIProfilePermissionGrants");

            migrationBuilder.DropTable(
                name: "AIPromptProfiles");

            migrationBuilder.DropTable(
                name: "AIPromptVersions");

            migrationBuilder.DropTable(
                name: "AIUsageStatistics");
        }
    }
}

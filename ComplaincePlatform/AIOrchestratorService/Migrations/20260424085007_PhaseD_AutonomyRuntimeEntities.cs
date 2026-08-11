using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrchestratorService.Migrations
{
    /// <inheritdoc />
    public partial class PhaseD_AutonomyRuntimeEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowTemplateId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WorkflowTemplateVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ContextVersionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStepId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CompletedStepIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PendingStepIdsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FailureType = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AgentExecutionSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DependsOnJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsParallelizable = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    InputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApprovalRequestedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureType = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentExecutionSteps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UseCase = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptTemplateId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptTemplateVersion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ContextVersionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompositeConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValidationOutcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DecisionPath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SourceReferencesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionLogs_ExecutionId_OccurredAtUtc",
                table: "AgentExecutionLogs",
                columns: new[] { "ExecutionId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutions_TenantId_Status_UpdatedAtUtc",
                table: "AgentExecutions",
                columns: new[] { "TenantId", "Status", "UpdatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionSteps_ExecutionId_Sequence",
                table: "AgentExecutionSteps",
                columns: new[] { "ExecutionId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentExecutionSteps_ExecutionId_StepId",
                table: "AgentExecutionSteps",
                columns: new[] { "ExecutionId", "StepId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIAuditEntries_HospitalId_OccurredAtUtc",
                table: "AIAuditEntries",
                columns: new[] { "HospitalId", "OccurredAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentExecutionLogs");

            migrationBuilder.DropTable(
                name: "AgentExecutions");

            migrationBuilder.DropTable(
                name: "AgentExecutionSteps");

            migrationBuilder.DropTable(
                name: "AIAuditEntries");
        }
    }
}

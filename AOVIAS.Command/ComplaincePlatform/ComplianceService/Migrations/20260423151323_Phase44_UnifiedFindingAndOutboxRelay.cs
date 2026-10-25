using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase44_UnifiedFindingAndOutboxRelay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplianceAuditFindings_ComplianceJourneys_ComplianceJourneyId",
                table: "ComplianceAuditFindings");

            migrationBuilder.DropForeignKey(
                name: "FK_ComplianceAuditFindings_ObjectiveElements_ObjectiveElementId",
                table: "ComplianceAuditFindings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Risk_ObjectiveElementOrTaskRequired",
                table: "Risks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComplianceAuditFindings",
                table: "ComplianceAuditFindings");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceAuditFindings_ComplianceJourneyId_IsResolved",
                table: "ComplianceAuditFindings");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceAuditFindings_DueDate",
                table: "ComplianceAuditFindings");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceAuditFindings_IsResolved",
                table: "ComplianceAuditFindings");

            migrationBuilder.RenameTable(
                name: "ComplianceAuditFindings",
                newName: "Findings");

            migrationBuilder.RenameColumn(
                name: "ProcessedOn",
                table: "OutboxMessages",
                newName: "RelayProcessedOn");

            migrationBuilder.RenameIndex(
                name: "IX_Outbox_ProcessedOn",
                table: "OutboxMessages",
                newName: "IX_Outbox_RelayProcessedOn");

            migrationBuilder.RenameIndex(
                name: "IX_ComplianceAuditFindings_ObjectiveElementId",
                table: "Findings",
                newName: "IX_Findings_ObjectiveElementId");

            migrationBuilder.RenameIndex(
                name: "IX_ComplianceAuditFindings_ComplianceJourneyId",
                table: "Findings",
                newName: "IX_Findings_ComplianceJourneyId");

            migrationBuilder.AddColumn<Guid>(
                name: "FindingId",
                table: "Risks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProjectionProcessedOn",
                table: "OutboxMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Findings",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<Guid>(
                name: "ObjectiveElementId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<bool>(
                name: "IsResolved",
                table: "Findings",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "Findings",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Findings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AlterColumn<Guid>(
                name: "ComplianceJourneyId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<int>(
                name: "AssignedDepartment",
                table: "Findings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssignedToUserId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClauseReference",
                table: "Findings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ComplianceStatus",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfidenceScore",
                table: "Findings",
                type: "decimal(5,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByType",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateRaised",
                table: "Findings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Department",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<int>(
                name: "EvidenceCount",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FindingKind",
                table: "Findings",
                type: "nvarchar(34)",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "Findings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdatedAt",
                table: "Findings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "LastUpdatedBy",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RaisedBy",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Findings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RiskLevel",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "Severity",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Findings",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "TaskId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Findings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Findings",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
UPDATE Findings
SET
    Scope = 1,
    ComplianceStatus = 3,
    CreatedByType = 1,
    Severity = CASE
        WHEN Type = 1 THEN 2
        WHEN Type = 2 THEN 1
        ELSE 1
    END,
    Status = CASE WHEN IsResolved = 1 THEN 4 ELSE 1 END,
    Department = 4,
    RaisedBy = 1,
    DateRaised = COALESCE(CreatedAt, GETUTCDATE()),
    LastUpdatedAt = GETUTCDATE(),
    TenantId = COALESCE(TenantId, '00000000-0000-0000-0000-000000000000'),
    FindingKind = 'ComplianceAuditFinding',
    Title = CASE WHEN LEN(LTRIM(RTRIM(Description))) = 0 THEN 'Audit finding' ELSE LEFT(Description, 500) END,
    ClauseReference = 'UNMAPPED'
WHERE FindingKind = '' OR FindingKind IS NULL;
");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Findings",
                table: "Findings",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "FindingAuditTrails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PreviousStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewStateJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingAuditTrails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingAuditTrails_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FindingEvidences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FindingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FindingEvidences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FindingEvidences_Evidences_EvidenceId",
                        column: x => x.EvidenceId,
                        principalTable: "Evidences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FindingEvidences_Findings_FindingId",
                        column: x => x.FindingId,
                        principalTable: "Findings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_FindingId",
                table: "Risks",
                column: "FindingId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Risk_ReferenceRequired",
                table: "Risks",
                sql: "([ObjectiveElementId] IS NOT NULL) OR ([TaskId] IS NOT NULL) OR ([FindingId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_Outbox_ProjectionProcessedOn",
                table: "OutboxMessages",
                column: "ProjectionProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Scope_TenantId",
                table: "Findings",
                columns: new[] { "Scope", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Findings_Status",
                table: "Findings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Findings_TaskId",
                table: "Findings",
                column: "TaskId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Finding_AuditScopeLinkage",
                table: "Findings",
                sql: "([Scope] <> 1) OR ([ComplianceJourneyId] IS NOT NULL AND [ObjectiveElementId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Finding_ConfidenceRange",
                table: "Findings",
                sql: "([ConfidenceScore] IS NULL) OR ([ConfidenceScore] >= 0 AND [ConfidenceScore] <= 1)");

            migrationBuilder.CreateIndex(
                name: "IX_FindingAuditTrails_FindingId_OccurredAt",
                table: "FindingAuditTrails",
                columns: new[] { "FindingId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FindingEvidences_EvidenceId",
                table: "FindingEvidences",
                column: "EvidenceId");

            migrationBuilder.CreateIndex(
                name: "IX_FindingEvidences_FindingId_EvidenceId",
                table: "FindingEvidences",
                columns: new[] { "FindingId", "EvidenceId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Findings_ComplianceJourneys_ComplianceJourneyId",
                table: "Findings",
                column: "ComplianceJourneyId",
                principalTable: "ComplianceJourneys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Findings_ComplianceTasks_TaskId",
                table: "Findings",
                column: "TaskId",
                principalTable: "ComplianceTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Findings_ObjectiveElements_ObjectiveElementId",
                table: "Findings",
                column: "ObjectiveElementId",
                principalTable: "ObjectiveElements",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Findings_FindingId",
                table: "Risks",
                column: "FindingId",
                principalTable: "Findings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Findings_ComplianceJourneys_ComplianceJourneyId",
                table: "Findings");

            migrationBuilder.DropForeignKey(
                name: "FK_Findings_ComplianceTasks_TaskId",
                table: "Findings");

            migrationBuilder.DropForeignKey(
                name: "FK_Findings_ObjectiveElements_ObjectiveElementId",
                table: "Findings");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Findings_FindingId",
                table: "Risks");

            migrationBuilder.DropTable(
                name: "FindingAuditTrails");

            migrationBuilder.DropTable(
                name: "FindingEvidences");

            migrationBuilder.DropIndex(
                name: "IX_Risks_FindingId",
                table: "Risks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Risk_ReferenceRequired",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Outbox_ProjectionProcessedOn",
                table: "OutboxMessages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Findings",
                table: "Findings");

            migrationBuilder.DropIndex(
                name: "IX_Findings_Scope_TenantId",
                table: "Findings");

            migrationBuilder.DropIndex(
                name: "IX_Findings_Status",
                table: "Findings");

            migrationBuilder.DropIndex(
                name: "IX_Findings_TaskId",
                table: "Findings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Finding_AuditScopeLinkage",
                table: "Findings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Finding_ConfidenceRange",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "FindingId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ProjectionProcessedOn",
                table: "OutboxMessages");

            migrationBuilder.DropColumn(
                name: "AssignedDepartment",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ClauseReference",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ComplianceStatus",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "CreatedByType",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "DateRaised",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "EvidenceCount",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "FindingKind",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "IsMandatory",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "RaisedBy",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "RiskLevel",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "TaskId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Findings");

            migrationBuilder.RenameTable(
                name: "Findings",
                newName: "ComplianceAuditFindings");

            migrationBuilder.RenameColumn(
                name: "RelayProcessedOn",
                table: "OutboxMessages",
                newName: "ProcessedOn");

            migrationBuilder.RenameIndex(
                name: "IX_Outbox_RelayProcessedOn",
                table: "OutboxMessages",
                newName: "IX_Outbox_ProcessedOn");

            migrationBuilder.RenameIndex(
                name: "IX_Findings_ObjectiveElementId",
                table: "ComplianceAuditFindings",
                newName: "IX_ComplianceAuditFindings_ObjectiveElementId");

            migrationBuilder.RenameIndex(
                name: "IX_Findings_ComplianceJourneyId",
                table: "ComplianceAuditFindings",
                newName: "IX_ComplianceAuditFindings_ComplianceJourneyId");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "ComplianceAuditFindings",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ObjectiveElementId",
                table: "ComplianceAuditFindings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsResolved",
                table: "ComplianceAuditFindings",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DueDate",
                table: "ComplianceAuditFindings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "ComplianceAuditFindings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<Guid>(
                name: "ComplianceJourneyId",
                table: "ComplianceAuditFindings",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComplianceAuditFindings",
                table: "ComplianceAuditFindings",
                column: "Id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Risk_ObjectiveElementOrTaskRequired",
                table: "Risks",
                sql: "([ObjectiveElementId] IS NOT NULL AND [TaskId] IS NULL) OR ([ObjectiveElementId] IS NULL AND [TaskId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_ComplianceJourneyId_IsResolved",
                table: "ComplianceAuditFindings",
                columns: new[] { "ComplianceJourneyId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_DueDate",
                table: "ComplianceAuditFindings",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAuditFindings_IsResolved",
                table: "ComplianceAuditFindings",
                column: "IsResolved");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplianceAuditFindings_ComplianceJourneys_ComplianceJourneyId",
                table: "ComplianceAuditFindings",
                column: "ComplianceJourneyId",
                principalTable: "ComplianceJourneys",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComplianceAuditFindings_ObjectiveElements_ObjectiveElementId",
                table: "ComplianceAuditFindings",
                column: "ObjectiveElementId",
                principalTable: "ObjectiveElements",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

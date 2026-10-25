using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726151000_Phase2_TaskEngine")]
public partial class Phase2_TaskEngine : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Category",
            table: "ComplianceTasks",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<int>(
            name: "TaskKind",
            table: "ComplianceTasks",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<Guid>(
            name: "BlockedByTaskId",
            table: "ComplianceTasks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RecurringDefinitionId",
            table: "ComplianceTasks",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "OverdueEscalatedAt",
            table: "ComplianceTasks",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTasks_Category",
            table: "ComplianceTasks",
            column: "Category");

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTasks_TaskKind",
            table: "ComplianceTasks",
            column: "TaskKind");

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTasks_BlockedByTaskId",
            table: "ComplianceTasks",
            column: "BlockedByTaskId");

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTasks_RecurringDefinitionId",
            table: "ComplianceTasks",
            column: "RecurringDefinitionId");

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTasks_HospitalId_DueDate_Status",
            table: "ComplianceTasks",
            columns: new[] { "HospitalId", "DueDate", "Status" });

        migrationBuilder.CreateTable(
            name: "ComplianceTaskOwnershipHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PreviousAssigneeUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                NewAssigneeUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ComplianceTaskOwnershipHistories", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTaskOwnershipHistories_TaskId",
            table: "ComplianceTaskOwnershipHistories",
            column: "TaskId");

        migrationBuilder.CreateIndex(
            name: "IX_ComplianceTaskOwnershipHistories_HospitalId_TaskId_ChangedAt",
            table: "ComplianceTaskOwnershipHistories",
            columns: new[] { "HospitalId", "TaskId", "ChangedAt" });

        migrationBuilder.CreateTable(
            name: "RecurringTaskDefinitions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ComplianceStandardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DepartmentName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                Category = table.Column<int>(type: "int", nullable: false),
                TaskKind = table.Column<int>(type: "int", nullable: false),
                Criticality = table.Column<int>(type: "int", nullable: false),
                Priority = table.Column<int>(type: "int", nullable: false),
                Frequency = table.Column<int>(type: "int", nullable: false),
                Interval = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                NextOccurrenceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastMaterializedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                LastMaterializedTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                DueDaysOffset = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecurringTaskDefinitions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RecurringTaskDefinitions_HospitalId_IsActive_NextOccurrenceDate",
            table: "RecurringTaskDefinitions",
            columns: new[] { "HospitalId", "IsActive", "NextOccurrenceDate" });

        migrationBuilder.CreateIndex(
            name: "IX_RecurringTaskDefinitions_ObjectiveElementId",
            table: "RecurringTaskDefinitions",
            column: "ObjectiveElementId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ComplianceTaskOwnershipHistories");
        migrationBuilder.DropTable(name: "RecurringTaskDefinitions");

        migrationBuilder.DropIndex(name: "IX_ComplianceTasks_Category", table: "ComplianceTasks");
        migrationBuilder.DropIndex(name: "IX_ComplianceTasks_TaskKind", table: "ComplianceTasks");
        migrationBuilder.DropIndex(name: "IX_ComplianceTasks_BlockedByTaskId", table: "ComplianceTasks");
        migrationBuilder.DropIndex(name: "IX_ComplianceTasks_RecurringDefinitionId", table: "ComplianceTasks");
        migrationBuilder.DropIndex(name: "IX_ComplianceTasks_HospitalId_DueDate_Status", table: "ComplianceTasks");

        migrationBuilder.DropColumn(name: "Category", table: "ComplianceTasks");
        migrationBuilder.DropColumn(name: "TaskKind", table: "ComplianceTasks");
        migrationBuilder.DropColumn(name: "BlockedByTaskId", table: "ComplianceTasks");
        migrationBuilder.DropColumn(name: "RecurringDefinitionId", table: "ComplianceTasks");
        migrationBuilder.DropColumn(name: "OverdueEscalatedAt", table: "ComplianceTasks");
    }
}

using System;
using GovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GovernanceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(GovernanceDbContext))]
[Migration("20260726200000_Phase4_CommitteeManagement")]
public partial class Phase4_CommitteeManagement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "RelayProcessedOn",
            table: "OutboxMessages",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "AgendaTemplateCode",
            table: "CommitteeMeetings",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "MinutesApproved",
            table: "CommitteeMeetings",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<DateTime>(
            name: "MinutesApprovedAt",
            table: "CommitteeMeetings",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "MinutesApprovedByUserId",
            table: "CommitteeMeetings",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "DeficiencyId",
            table: "CommitteeDecisions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "RiskId",
            table: "CommitteeDecisions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TaskId",
            table: "CommitteeDecisions",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeDecisions_DeficiencyId",
            table: "CommitteeDecisions",
            column: "DeficiencyId");

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeDecisions_RiskId",
            table: "CommitteeDecisions",
            column: "RiskId");

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeDecisions_TaskId",
            table: "CommitteeDecisions",
            column: "TaskId");

        migrationBuilder.CreateTable(
            name: "CommitteeActionItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CommitteeMeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                OwnerAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                Status = table.Column<int>(type: "int", nullable: false),
                TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                DecisionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommitteeActionItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_CommitteeActionItems_CommitteeMeetings_CommitteeMeetingId",
                    column: x => x.CommitteeMeetingId,
                    principalTable: "CommitteeMeetings",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeActionItems_CommitteeMeetingId",
            table: "CommitteeActionItems",
            column: "CommitteeMeetingId");

        migrationBuilder.CreateIndex(
            name: "IX_CommitteeActionItems_TaskId",
            table: "CommitteeActionItems",
            column: "TaskId");

        migrationBuilder.CreateTable(
            name: "AgendaTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                SectionsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsManagementReview = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgendaTemplates", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AgendaTemplates_Code",
            table: "AgendaTemplates",
            column: "Code",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CommitteeActionItems");
        migrationBuilder.DropTable(name: "AgendaTemplates");

        migrationBuilder.DropIndex(name: "IX_CommitteeDecisions_DeficiencyId", table: "CommitteeDecisions");
        migrationBuilder.DropIndex(name: "IX_CommitteeDecisions_RiskId", table: "CommitteeDecisions");
        migrationBuilder.DropIndex(name: "IX_CommitteeDecisions_TaskId", table: "CommitteeDecisions");

        migrationBuilder.DropColumn(name: "AgendaTemplateCode", table: "CommitteeMeetings");
        migrationBuilder.DropColumn(name: "MinutesApproved", table: "CommitteeMeetings");
        migrationBuilder.DropColumn(name: "MinutesApprovedAt", table: "CommitteeMeetings");
        migrationBuilder.DropColumn(name: "MinutesApprovedByUserId", table: "CommitteeMeetings");
        migrationBuilder.DropColumn(name: "DeficiencyId", table: "CommitteeDecisions");
        migrationBuilder.DropColumn(name: "RiskId", table: "CommitteeDecisions");
        migrationBuilder.DropColumn(name: "TaskId", table: "CommitteeDecisions");
        migrationBuilder.DropColumn(name: "RelayProcessedOn", table: "OutboxMessages");
    }
}

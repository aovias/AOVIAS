using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GovernanceService.Migrations
{
    /// <inheritdoc />
    public partial class _20260706_M6_5_GovernanceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalChains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChainCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChainName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalChains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Committees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CommitteeName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CommitteeTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Charter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChairpersonAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SecretaryAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TermStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TermEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Committees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationChains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChainCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChainName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EscalationTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationChains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceAssignments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssigneeType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignmentMode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DelegatedFromAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssignedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceAssignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GovernanceTimeline",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GovernedResourceType = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GovernedResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernanceTimeline", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LookupCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Prefix = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSequences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Responsibilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResponsibilityCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ResponsibilityTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GovernedResourceType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GovernedResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequiresPrimaryOwner = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Responsibilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    AssigneeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConditionExpression = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FallbackStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApprovalSteps_ApprovalChains_ApprovalChainId",
                        column: x => x.ApprovalChainId,
                        principalTable: "ApprovalChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMeetings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MeetingTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Agenda = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMeetings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMeetings_Committees_CommitteeId",
                        column: x => x.CommitteeId,
                        principalTable: "Committees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeRoleValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TermStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TermEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VotingRights = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReplacedByMembershipId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeMemberships_Committees_CommitteeId",
                        column: x => x.CommitteeId,
                        principalTable: "Committees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EscalationLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationChainId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LevelOrder = table.Column<int>(type: "int", nullable: false),
                    TargetAssigneeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetAssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DelayHours = table.Column<int>(type: "int", nullable: true),
                    ConditionExpression = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationLevels_EscalationChains_EscalationChainId",
                        column: x => x.EscalationChainId,
                        principalTable: "EscalationChains",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LookupValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LookupValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LookupValues_LookupCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "LookupCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitteeDecisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommitteeMeetingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecisionTypeValueId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GovernedResourceType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GovernedResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitteeDecisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommitteeDecisions_CommitteeMeetings_CommitteeMeetingId",
                        column: x => x.CommitteeMeetingId,
                        principalTable: "CommitteeMeetings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalChains_ChainCode",
                table: "ApprovalChains",
                column: "ChainCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApprovalChainId",
                table: "ApprovalSteps",
                column: "ApprovalChainId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeDecisions_CommitteeMeetingId",
                table: "CommitteeDecisions",
                column: "CommitteeMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMeetings_CommitteeId",
                table: "CommitteeMeetings",
                column: "CommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitteeMemberships_CommitteeId",
                table: "CommitteeMemberships",
                column: "CommitteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Committees_CommitteeCode",
                table: "Committees",
                column: "CommitteeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationChains_ChainCode",
                table: "EscalationChains",
                column: "ChainCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationLevels_EscalationChainId",
                table: "EscalationLevels",
                column: "EscalationChainId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceAssignments_ResponsibilityId_AssigneeType_AssigneeId",
                table: "GovernanceAssignments",
                columns: new[] { "ResponsibilityId", "AssigneeType", "AssigneeId" });

            migrationBuilder.CreateIndex(
                name: "IX_GovernanceTimeline_GovernedResourceType_GovernedResourceId_OccurredAt",
                table: "GovernanceTimeline",
                columns: new[] { "GovernedResourceType", "GovernedResourceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupValues_CategoryId_Code",
                table: "LookupValues",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn",
                table: "OutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_Responsibilities_ResponsibilityCode",
                table: "Responsibilities",
                column: "ResponsibilityCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalSteps");

            migrationBuilder.DropTable(
                name: "CommitteeDecisions");

            migrationBuilder.DropTable(
                name: "CommitteeMemberships");

            migrationBuilder.DropTable(
                name: "EscalationLevels");

            migrationBuilder.DropTable(
                name: "GovernanceAssignments");

            migrationBuilder.DropTable(
                name: "GovernanceAuditLogs");

            migrationBuilder.DropTable(
                name: "GovernanceTimeline");

            migrationBuilder.DropTable(
                name: "LookupValues");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "PlatformSequences");

            migrationBuilder.DropTable(
                name: "Responsibilities");

            migrationBuilder.DropTable(
                name: "ApprovalChains");

            migrationBuilder.DropTable(
                name: "CommitteeMeetings");

            migrationBuilder.DropTable(
                name: "EscalationChains");

            migrationBuilder.DropTable(
                name: "LookupCategories");

            migrationBuilder.DropTable(
                name: "Committees");
        }
    }
}

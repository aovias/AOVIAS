using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialKnowledgeSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Frameworks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Frameworks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assessments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assessments_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceNodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NodeType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ComplianceNodes_ComplianceNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ComplianceNodes_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FrameworkClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FrameworkClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FrameworkClassifications_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoringConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FullyCompliantScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PartialScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NonCompliantScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AggregationMethod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScaleType = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoringConfigs_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContextNodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Prompt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Response = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelUsed = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIInteractions_ComplianceNodes_ContextNodeId",
                        column: x => x.ContextNodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIInteractions_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HasActionPlan = table.Column<bool>(type: "bit", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentItems_Assessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "Assessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssessmentItems_ComplianceNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WhyItMatters = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HowToComply = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceRequired = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AuditChecklist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpactLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeItems_ComplianceNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequirementDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Intent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MeasurableElements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    ApplicableDepartments = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiresDocumentation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementDetails_ComplianceNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequirementScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ClassificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequirementScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequirementScores_ComplianceNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequirementScores_FrameworkClassifications_ClassificationId",
                        column: x => x.ClassificationId,
                        principalTable: "FrameworkClassifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DecisionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AIInteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProblemStatement = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestedAction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalDecision = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecisionLogs_AIInteractions_AIInteractionId",
                        column: x => x.AIInteractionId,
                        principalTable: "AIInteractions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecisionLogs_ComplianceNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DecisionLogs_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIInteractions_ContextNodeId",
                table: "AIInteractions",
                column: "ContextNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AIInteractions_FrameworkId",
                table: "AIInteractions",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentItems_AssessmentId_NodeId",
                table: "AssessmentItems",
                columns: new[] { "AssessmentId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentItems_NodeId",
                table: "AssessmentItems",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_FacilityId",
                table: "Assessments",
                column: "FacilityId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_FrameworkId",
                table: "Assessments",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceNodes_FrameworkId",
                table: "ComplianceNodes",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceNodes_FrameworkId_ParentId",
                table: "ComplianceNodes",
                columns: new[] { "FrameworkId", "ParentId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceNodes_FrameworkId_Sequence",
                table: "ComplianceNodes",
                columns: new[] { "FrameworkId", "Sequence" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceNodes_ParentId",
                table: "ComplianceNodes",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionLogs_AIInteractionId",
                table: "DecisionLogs",
                column: "AIInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionLogs_FrameworkId",
                table: "DecisionLogs",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_DecisionLogs_NodeId",
                table: "DecisionLogs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FrameworkClassifications_FrameworkId",
                table: "FrameworkClassifications",
                column: "FrameworkId");

            migrationBuilder.CreateIndex(
                name: "IX_Frameworks_Code",
                table: "Frameworks",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_NodeId",
                table: "KnowledgeItems",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementDetails_NodeId",
                table: "RequirementDetails",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequirementScores_ClassificationId",
                table: "RequirementScores",
                column: "ClassificationId");

            migrationBuilder.CreateIndex(
                name: "IX_RequirementScores_NodeId",
                table: "RequirementScores",
                column: "NodeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoringConfigs_FrameworkId",
                table: "ScoringConfigs",
                column: "FrameworkId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentItems");

            migrationBuilder.DropTable(
                name: "DecisionLogs");

            migrationBuilder.DropTable(
                name: "KnowledgeItems");

            migrationBuilder.DropTable(
                name: "RequirementDetails");

            migrationBuilder.DropTable(
                name: "RequirementScores");

            migrationBuilder.DropTable(
                name: "ScoringConfigs");

            migrationBuilder.DropTable(
                name: "Assessments");

            migrationBuilder.DropTable(
                name: "AIInteractions");

            migrationBuilder.DropTable(
                name: "FrameworkClassifications");

            migrationBuilder.DropTable(
                name: "ComplianceNodes");

            migrationBuilder.DropTable(
                name: "Frameworks");
        }
    }
}

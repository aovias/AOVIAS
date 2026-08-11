using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWizardQuestionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WizardQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChapterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StandardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionTitle = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HelpText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssessmentGuidance = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpectedEvidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    WeightBand = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    RiskCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WizardQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WizardQuestions_ComplianceNodes_ChapterId",
                        column: x => x.ChapterId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WizardQuestions_ComplianceNodes_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WizardQuestions_ComplianceNodes_StandardId",
                        column: x => x.StandardId,
                        principalTable: "ComplianceNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WizardQuestions_Frameworks_FrameworkId",
                        column: x => x.FrameworkId,
                        principalTable: "Frameworks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WizardQuestions_Category_IsActive",
                table: "WizardQuestions",
                columns: new[] { "Category", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WizardQuestions_ChapterId_DisplayOrder",
                table: "WizardQuestions",
                columns: new[] { "ChapterId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_WizardQuestions_FrameworkId_IsActive",
                table: "WizardQuestions",
                columns: new[] { "FrameworkId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WizardQuestions_ObjectiveElementId_Version",
                table: "WizardQuestions",
                columns: new[] { "ObjectiveElementId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WizardQuestions_StandardId",
                table: "WizardQuestions",
                column: "StandardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WizardQuestions");
        }
    }
}

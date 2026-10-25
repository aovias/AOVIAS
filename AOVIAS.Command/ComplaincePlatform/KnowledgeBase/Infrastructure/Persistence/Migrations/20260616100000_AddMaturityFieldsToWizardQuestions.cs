using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KnowledgeBase.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaturityFieldsToWizardQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssessmentMaturityLevel",
                table: "WizardQuestions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AssessmentPhase",
                table: "WizardQuestions",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsMandatory",
                table: "WizardQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsCritical",
                table: "WizardQuestions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "UnlockThreshold",
                table: "WizardQuestions",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayPriority",
                table: "WizardQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AssessmentJourneyOrder",
                table: "WizardQuestions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_WizardQuestions_FrameworkId_MaturityLevel_IsActive",
                table: "WizardQuestions",
                columns: new[] { "FrameworkId", "AssessmentMaturityLevel", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WizardQuestions_FrameworkId_MaturityLevel_IsActive",
                table: "WizardQuestions");

            migrationBuilder.DropColumn(name: "AssessmentJourneyOrder", table: "WizardQuestions");
            migrationBuilder.DropColumn(name: "DisplayPriority", table: "WizardQuestions");
            migrationBuilder.DropColumn(name: "UnlockThreshold", table: "WizardQuestions");
            migrationBuilder.DropColumn(name: "IsCritical", table: "WizardQuestions");
            migrationBuilder.DropColumn(name: "IsMandatory", table: "WizardQuestions");
            migrationBuilder.DropColumn(name: "AssessmentPhase", table: "WizardQuestions");
            migrationBuilder.DropColumn(name: "AssessmentMaturityLevel", table: "WizardQuestions");
        }
    }
}

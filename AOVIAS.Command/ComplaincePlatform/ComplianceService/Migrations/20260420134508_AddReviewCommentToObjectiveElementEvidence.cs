using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class AddReviewCommentToObjectiveElementEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReviewComment",
                table: "ObjectiveElementEvidences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [ObjectiveElementEvidences]
                SET [ReviewComment] = [Remarks]
                WHERE [VerificationStatus] = 2
                  AND [ReviewComment] IS NULL
                  AND [Remarks] IS NOT NULL
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReviewComment",
                table: "ObjectiveElementEvidences");
        }
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_ComplianceTaskSourceDeficiencyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_HospitalId_ObjectiveElementId",
                table: "ComplianceTasks");

            migrationBuilder.AddColumn<Guid>(
                name: "SourceDeficiencyId",
                table: "ComplianceTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_HospitalId_ObjectiveElementId",
                table: "ComplianceTasks",
                columns: new[] { "HospitalId", "ObjectiveElementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_SourceDeficiencyId",
                table: "ComplianceTasks",
                column: "SourceDeficiencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_SourceDeficiencyId",
                table: "ComplianceTasks");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_HospitalId_ObjectiveElementId",
                table: "ComplianceTasks");

            migrationBuilder.DropColumn(
                name: "SourceDeficiencyId",
                table: "ComplianceTasks");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_HospitalId_ObjectiveElementId",
                table: "ComplianceTasks",
                columns: new[] { "HospitalId", "ObjectiveElementId" },
                unique: true);
        }
    }
}

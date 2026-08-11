using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase40_ComplianceTaskComplianceStandard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ComplianceTasks_Regulations_RegulationId",
                table: "ComplianceTasks");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceTasks_RegulationId",
                table: "ComplianceTasks");

            migrationBuilder.AddColumn<Guid>(
                name: "ComplianceStandardId",
                table: "ComplianceTasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE t
                SET ComplianceStandardId = oe.ComplianceStandardId
                FROM ComplianceTasks AS t
                INNER JOIN ObjectiveElements AS oe ON oe.Id = t.ObjectiveElementId
                """);

            migrationBuilder.DropColumn(
                name: "RegulationId",
                table: "ComplianceTasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ComplianceStandardId",
                table: "ComplianceTasks",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceTasks_ComplianceStandardId",
                table: "ComplianceTasks",
                column: "ComplianceStandardId");

            migrationBuilder.AddForeignKey(
                name: "FK_ComplianceTasks_ComplianceStandards_ComplianceStandardId",
                table: "ComplianceTasks",
                column: "ComplianceStandardId",
                principalTable: "ComplianceStandards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Reverting Phase40 would require recreating RegulationId values; use a backup instead.");
        }
    }
}

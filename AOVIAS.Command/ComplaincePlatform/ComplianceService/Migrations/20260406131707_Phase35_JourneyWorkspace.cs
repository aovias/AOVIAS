using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase35_JourneyWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "Evidences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ObjectiveElementObservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ComplianceJourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveElementObservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementObservations_ComplianceJourneys_ComplianceJourneyId",
                        column: x => x.ComplianceJourneyId,
                        principalTable: "ComplianceJourneys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementObservations_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementScores_JourneyId_ObjectiveElementId",
                table: "ObjectiveElementScores",
                columns: new[] { "JourneyId", "ObjectiveElementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementObservations_ComplianceJourneyId_ObjectiveElementId",
                table: "ObjectiveElementObservations",
                columns: new[] { "ComplianceJourneyId", "ObjectiveElementId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementObservations_ObjectiveElementId",
                table: "ObjectiveElementObservations",
                column: "ObjectiveElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObjectiveElementObservations");

            migrationBuilder.DropIndex(
                name: "IX_ObjectiveElementScores_JourneyId_ObjectiveElementId",
                table: "ObjectiveElementScores");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "Evidences");
        }
    }
}

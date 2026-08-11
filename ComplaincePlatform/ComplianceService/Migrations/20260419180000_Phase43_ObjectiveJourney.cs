using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

[DbContext(typeof(ComplianceDbContext))]
[Migration("20260419180000_Phase43_ObjectiveJourney")]
/// <inheritdoc />
public partial class Phase43_ObjectiveJourney : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ObjectiveJourneys",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ComplianceJourneyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                WizardPayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CurrentStepIndex = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ObjectiveJourneys", x => x.Id);
                table.ForeignKey(
                    name: "FK_ObjectiveJourneys_ComplianceJourneys_ComplianceJourneyId",
                    column: x => x.ComplianceJourneyId,
                    principalTable: "ComplianceJourneys",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ObjectiveJourneys_ObjectiveElements_ObjectiveElementId",
                    column: x => x.ObjectiveElementId,
                    principalTable: "ObjectiveElements",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ObjectiveJourneys_ComplianceJourneyId_ObjectiveElementId",
            table: "ObjectiveJourneys",
            columns: new[] { "ComplianceJourneyId", "ObjectiveElementId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ObjectiveJourneys_HospitalId",
            table: "ObjectiveJourneys",
            column: "HospitalId");

        migrationBuilder.CreateIndex(
            name: "IX_ObjectiveJourneys_TenantId",
            table: "ObjectiveJourneys",
            column: "TenantId");

        migrationBuilder.CreateIndex(
            name: "IX_ObjectiveJourneys_ObjectiveElementId",
            table: "ObjectiveJourneys",
            column: "ObjectiveElementId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ObjectiveJourneys");
    }
}

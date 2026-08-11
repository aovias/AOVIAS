using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726201000_Phase4_GovernanceTraceLinks")]
public partial class Phase4_GovernanceTraceLinks : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GovernanceTraceLinks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SourceEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                SourceEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SourceLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                TargetEntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                TargetEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TargetLabel = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RelationshipType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GovernanceTraceLinks", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GovernanceTraceLinks_OrganizationId_SourceEntityId",
            table: "GovernanceTraceLinks",
            columns: new[] { "OrganizationId", "SourceEntityId" });

        migrationBuilder.CreateIndex(
            name: "IX_GovernanceTraceLinks_OrganizationId_TargetEntityId",
            table: "GovernanceTraceLinks",
            columns: new[] { "OrganizationId", "TargetEntityId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GovernanceTraceLinks");
    }
}

using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIOrchestratorService.Migrations;

/// <inheritdoc />
[DbContext(typeof(AIOrchestratorDbContext))]
[Migration("20260726150000_Phase5_AIAuditModelVersion")]
public partial class Phase5_AIAuditModelVersion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ModelVersion",
            table: "AIAuditEntries",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ModelVersion",
            table: "AIAuditEntries");
    }
}

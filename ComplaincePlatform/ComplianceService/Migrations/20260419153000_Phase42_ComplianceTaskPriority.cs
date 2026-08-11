using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    [DbContext(typeof(ComplianceDbContext))]
    [Migration("20260419153000_Phase42_ComplianceTaskPriority")]
    /// <inheritdoc />
    public partial class Phase42_ComplianceTaskPriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ComplianceTasks",
                type: "int",
                nullable: false,
                defaultValue: 3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ComplianceTasks");
        }
    }
}

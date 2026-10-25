using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ComplianceDbContext))]
    [Migration("20260726120000_Phase1_FrameworkLifecycle")]
    public partial class Phase1_FrameworkLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ComplianceFrameworks",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "ComplianceFrameworks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RetiredAt",
                table: "ComplianceFrameworks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedAt",
                table: "ComplianceFrameworks",
                type: "datetime2",
                nullable: true);

            // Backfill from legacy IsActive: Active=2, Retired=3
            migrationBuilder.Sql(
                """
                UPDATE ComplianceFrameworks SET Status = 2 WHERE IsActive = 1;
                UPDATE ComplianceFrameworks SET Status = 3 WHERE IsActive = 0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "ComplianceFrameworks");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "ComplianceFrameworks");

            migrationBuilder.DropColumn(
                name: "RetiredAt",
                table: "ComplianceFrameworks");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ComplianceFrameworks");
        }
    }
}

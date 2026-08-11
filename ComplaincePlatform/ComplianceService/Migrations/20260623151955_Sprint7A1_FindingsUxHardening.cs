using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7A1_FindingsUxHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuditCycle",
                table: "Findings",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuditDate",
                table: "Findings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuditorName",
                table: "Findings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Impact",
                table: "Findings",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observation",
                table: "Findings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "Findings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Requirement",
                table: "Findings",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuditCycle",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "AuditDate",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "AuditorName",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Impact",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Observation",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "Findings");

            migrationBuilder.DropColumn(
                name: "Requirement",
                table: "Findings");
        }
    }
}

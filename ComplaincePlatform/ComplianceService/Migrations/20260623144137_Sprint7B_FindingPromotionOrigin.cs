using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint7B_FindingPromotionOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OriginId",
                table: "ComplianceDeficiencies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginReference",
                table: "ComplianceDeficiencies",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginType",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PromotedAt",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginId",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "OriginReference",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "OriginType",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "PromotedAt",
                table: "ComplianceDeficiencies");
        }
    }
}

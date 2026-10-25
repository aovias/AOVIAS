using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations;

/// <inheritdoc />
[DbContext(typeof(ComplianceDbContext))]
[Migration("20260726160000_Phase4_DocumentControlExpiry")]
public partial class Phase4_DocumentControlExpiry : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "ExpiryDate",
            table: "ControlledDocuments",
            type: "datetime2",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_ControlledDocuments_ExpiryDate",
            table: "ControlledDocuments",
            column: "ExpiryDate");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ControlledDocuments_ExpiryDate",
            table: "ControlledDocuments");

        migrationBuilder.DropColumn(
            name: "ExpiryDate",
            table: "ControlledDocuments");
    }
}

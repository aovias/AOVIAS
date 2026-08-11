using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GovernanceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_GovernanceEvolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttendanceJson",
                table: "CommitteeMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "CommitteeMeetings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Minutes",
                table: "CommitteeMeetings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "CommitteeMeetings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImplementationStatus",
                table: "CommitteeDecisions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerAssigneeId",
                table: "CommitteeDecisions",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttendanceJson",
                table: "CommitteeMeetings");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "CommitteeMeetings");

            migrationBuilder.DropColumn(
                name: "Minutes",
                table: "CommitteeMeetings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CommitteeMeetings");

            migrationBuilder.DropColumn(
                name: "ImplementationStatus",
                table: "CommitteeDecisions");

            migrationBuilder.DropColumn(
                name: "OwnerAssigneeId",
                table: "CommitteeDecisions");
        }
    }
}

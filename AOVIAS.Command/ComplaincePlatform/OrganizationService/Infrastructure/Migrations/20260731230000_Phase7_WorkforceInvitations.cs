using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrganizationService.Infrastructure.Persistence;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20260731230000_Phase7_WorkforceInvitations")]
public partial class Phase7_WorkforceInvitations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Designation",
            table: "OrganizationMemberInvitations",
            type: "nvarchar(200)",
            maxLength: 200,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "EmploymentType",
            table: "OrganizationMemberInvitations",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: false,
            defaultValue: "Permanent");

        migrationBuilder.AddColumn<Guid>(
            name: "ReportsToUserId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ManagerEmploymentId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "RequireSeat",
            table: "OrganizationMemberInvitations",
            type: "bit",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PositionId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "InvitedByUserId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "CancelledAt",
            table: "OrganizationMemberInvitations",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PipelineStep",
            table: "OrganizationMemberInvitations",
            type: "nvarchar(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "PipelineError",
            table: "OrganizationMemberInvitations",
            type: "nvarchar(1024)",
            maxLength: 1024,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "MembershipId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "EmploymentId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "PersonId",
            table: "OrganizationMemberInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "SeatAssigned",
            table: "OrganizationMemberInvitations",
            type: "bit",
            nullable: false,
            defaultValue: false);

        migrationBuilder.CreateIndex(
            name: "IX_OrganizationMemberInvitations_TokenHash",
            table: "OrganizationMemberInvitations",
            column: "TokenHash");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_OrganizationMemberInvitations_TokenHash",
            table: "OrganizationMemberInvitations");

        migrationBuilder.DropColumn(name: "Designation", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "EmploymentType", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "ReportsToUserId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "ManagerEmploymentId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "RequireSeat", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "PositionId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "InvitedByUserId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "CancelledAt", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "PipelineStep", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "PipelineError", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "MembershipId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "EmploymentId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "PersonId", table: "OrganizationMemberInvitations");
        migrationBuilder.DropColumn(name: "SeatAssigned", table: "OrganizationMemberInvitations");
    }
}

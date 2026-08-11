using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccessGovernanceService.Migrations
{
    /// <inheritdoc />
    public partial class Phase5_AccessGovernanceEvolution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EmploymentId",
                table: "RoleAssignments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FieldMask",
                table: "Permissions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Delegations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HospitalId",
                table: "Delegations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                table: "Delegations",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoleAssignments_UserId_IsRevoked_EffectiveFrom_EffectiveTo",
                table: "RoleAssignments",
                columns: new[] { "UserId", "IsRevoked", "EffectiveFrom", "EffectiveTo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoleAssignments_UserId_IsRevoked_EffectiveFrom_EffectiveTo",
                table: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "EmploymentId",
                table: "RoleAssignments");

            migrationBuilder.DropColumn(
                name: "FieldMask",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Delegations");

            migrationBuilder.DropColumn(
                name: "HospitalId",
                table: "Delegations");

            migrationBuilder.DropColumn(
                name: "OrganizationId",
                table: "Delegations");
        }
    }
}

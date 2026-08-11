using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class M6_2_OrganizationMemberPersonLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "Members",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "Members");
        }
    }
}

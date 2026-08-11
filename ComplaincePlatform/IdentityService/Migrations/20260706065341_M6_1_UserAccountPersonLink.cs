using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class M6_1_UserAccountPersonLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PersonId",
                table: "AspNetUsers",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PersonId",
                table: "AspNetUsers");
        }
    }
}

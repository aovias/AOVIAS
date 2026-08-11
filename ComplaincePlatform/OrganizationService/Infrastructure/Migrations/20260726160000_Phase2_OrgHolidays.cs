using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_OrgHolidays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrgHolidays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrgHolidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgHolidays_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgHolidays_OrganizationId_Date",
                table: "OrgHolidays",
                columns: new[] { "OrganizationId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "OrgHolidays");
        }
    }
}

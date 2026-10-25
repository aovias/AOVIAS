using System;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ComplianceDbContext))]
    [Migration("20260726120100_Phase1_OeOwnershipMatrix")]
    public partial class Phase1_OeOwnershipMatrix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ObjectiveElementOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FrameworkId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectiveElementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimaryOwnerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectiveElementOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementOwnerships_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObjectiveElementOwnerships_ObjectiveElements_ObjectiveElementId",
                        column: x => x.ObjectiveElementId,
                        principalTable: "ObjectiveElements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementOwnerships_DepartmentId",
                table: "ObjectiveElementOwnerships",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementOwnerships_HospitalId_FrameworkId",
                table: "ObjectiveElementOwnerships",
                columns: new[] { "HospitalId", "FrameworkId" });

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementOwnerships_HospitalId_ObjectiveElementId",
                table: "ObjectiveElementOwnerships",
                columns: new[] { "HospitalId", "ObjectiveElementId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectiveElementOwnerships_ObjectiveElementId",
                table: "ObjectiveElementOwnerships",
                column: "ObjectiveElementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ObjectiveElementOwnerships");
        }
    }
}

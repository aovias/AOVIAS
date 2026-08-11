using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint9B_AssignmentRouting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssignmentRoutingLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RuleApplied = table.Column<int>(type: "int", nullable: false),
                    RecommendedUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AppliedUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Confidence = table.Column<int>(type: "int", nullable: false),
                    RoutingReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    WasAutoAssigned = table.Column<bool>(type: "bit", nullable: false),
                    WasOverridden = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssignmentRoutingLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentLabelMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OeDepartmentLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NormalizedCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DefaultResponsibleRole = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentLabelMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DepartmentOwnerships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrimaryOwnerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BackupOwnerUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepartmentOwnerships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DepartmentOwnerships_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRoutingLogs_EntityType_EntityId",
                table: "AssignmentRoutingLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AssignmentRoutingLogs_OrganizationId_CreatedAt",
                table: "AssignmentRoutingLogs",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentLabelMappings_NormalizedCode",
                table: "DepartmentLabelMappings",
                column: "NormalizedCode");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentLabelMappings_OeDepartmentLabel",
                table: "DepartmentLabelMappings",
                column: "OeDepartmentLabel",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentOwnerships_DepartmentId",
                table: "DepartmentOwnerships",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DepartmentOwnerships_OrganizationId_HospitalId_DepartmentId_Status",
                table: "DepartmentOwnerships",
                columns: new[] { "OrganizationId", "HospitalId", "DepartmentId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssignmentRoutingLogs");

            migrationBuilder.DropTable(
                name: "DepartmentLabelMappings");

            migrationBuilder.DropTable(
                name: "DepartmentOwnerships");
        }
    }
}

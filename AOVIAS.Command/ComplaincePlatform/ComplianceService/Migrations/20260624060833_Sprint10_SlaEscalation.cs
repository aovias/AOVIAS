using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComplianceService.Migrations
{
    /// <inheritdoc />
    public partial class Sprint10_SlaEscalation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentEscalationLevel",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentEscalationState",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentSlaStatus",
                table: "ComplianceDeficiencies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaDueDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SlaPolicyId",
                table: "ComplianceDeficiencies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaStartDate",
                table: "ComplianceDeficiencies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeficiencyEscalationHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeficiencyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    TargetUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    TriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeficiencyEscalationHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EscalationLevels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EscalationPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetDesignation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TargetUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    TargetLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TriggerAtSlaPercent = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EscalationLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EscalationLevels_EscalationPolicies_EscalationPolicyId",
                        column: x => x.EscalationPolicyId,
                        principalTable: "EscalationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SlaPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HospitalId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    LifecycleStage = table.Column<int>(type: "int", nullable: true),
                    TargetResolutionHours = table.Column<int>(type: "int", nullable: false),
                    WarningThresholdPercent = table.Column<int>(type: "int", nullable: false),
                    EscalationPolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SlaPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SlaPolicies_EscalationPolicies_EscalationPolicyId",
                        column: x => x.EscalationPolicyId,
                        principalTable: "EscalationPolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceDeficiencies_CurrentSlaStatus",
                table: "ComplianceDeficiencies",
                column: "CurrentSlaStatus");

            migrationBuilder.CreateIndex(
                name: "IX_DeficiencyEscalationHistories_DeficiencyId_TriggeredAt",
                table: "DeficiencyEscalationHistories",
                columns: new[] { "DeficiencyId", "TriggeredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DeficiencyEscalationHistories_OrganizationId_State",
                table: "DeficiencyEscalationHistories",
                columns: new[] { "OrganizationId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_EscalationLevels_EscalationPolicyId_Level",
                table: "EscalationLevels",
                columns: new[] { "EscalationPolicyId", "Level" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EscalationPolicies_OrganizationId_HospitalId_Severity",
                table: "EscalationPolicies",
                columns: new[] { "OrganizationId", "HospitalId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_EscalationPolicyId",
                table: "SlaPolicies",
                column: "EscalationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_SlaPolicies_OrganizationId_Severity_Status",
                table: "SlaPolicies",
                columns: new[] { "OrganizationId", "Severity", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeficiencyEscalationHistories");

            migrationBuilder.DropTable(
                name: "EscalationLevels");

            migrationBuilder.DropTable(
                name: "SlaPolicies");

            migrationBuilder.DropTable(
                name: "EscalationPolicies");

            migrationBuilder.DropIndex(
                name: "IX_ComplianceDeficiencies_CurrentSlaStatus",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CurrentEscalationLevel",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CurrentEscalationState",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "CurrentSlaStatus",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "SlaDueDate",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "SlaPolicyId",
                table: "ComplianceDeficiencies");

            migrationBuilder.DropColumn(
                name: "SlaStartDate",
                table: "ComplianceDeficiencies");
        }
    }
}

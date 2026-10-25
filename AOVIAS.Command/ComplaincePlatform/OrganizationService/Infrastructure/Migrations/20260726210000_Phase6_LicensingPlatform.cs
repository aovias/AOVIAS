using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrganizationService.Infrastructure.Persistence;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20260726210000_Phase6_LicensingPlatform")]
public partial class Phase6_LicensingPlatform : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "LicenseKeys",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                KeyCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                LicenseType = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                MaxSeats = table.Column<int>(type: "int", nullable: false),
                UsedSeats = table.Column<int>(type: "int", nullable: false),
                GracePeriodDays = table.Column<int>(type: "int", nullable: false),
                IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                GraceEndsAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                EntitlementsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                CustomerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_LicenseKeys", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LicenseActivations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LicenseKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MachineFingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ActivationMode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                OfflinePayload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Signature = table.Column<string>(type: "nvarchar(max)", nullable: true),
                ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                DeactivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                ActivatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_LicenseActivations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LicenseSeatAssignments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LicenseKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                AssignedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReleasedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LicenseSeatAssignments", x => x.Id));

        migrationBuilder.CreateTable(
            name: "LicenseHeartbeats",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                LicenseKeyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MachineFingerprint = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ReceivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ProductVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                StatusSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                SeatUsage = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_LicenseHeartbeats", x => x.Id));

        migrationBuilder.CreateIndex(name: "IX_LicenseKeys_KeyCode", table: "LicenseKeys", column: "KeyCode", unique: true);
        migrationBuilder.CreateIndex(name: "IX_LicenseKeys_OrganizationId_Status", table: "LicenseKeys", columns: new[] { "OrganizationId", "Status" });
        migrationBuilder.CreateIndex(name: "IX_LicenseActivations_LicenseKeyId_IsActive", table: "LicenseActivations", columns: new[] { "LicenseKeyId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_LicenseSeatAssignments_LicenseKeyId_UserId_IsActive", table: "LicenseSeatAssignments", columns: new[] { "LicenseKeyId", "UserId", "IsActive" });
        migrationBuilder.CreateIndex(name: "IX_LicenseHeartbeats_OrganizationId_ReceivedAtUtc", table: "LicenseHeartbeats", columns: new[] { "OrganizationId", "ReceivedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "LicenseHeartbeats");
        migrationBuilder.DropTable(name: "LicenseSeatAssignments");
        migrationBuilder.DropTable(name: "LicenseActivations");
        migrationBuilder.DropTable(name: "LicenseKeys");
    }
}

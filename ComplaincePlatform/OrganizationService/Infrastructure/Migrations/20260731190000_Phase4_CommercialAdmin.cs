using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrganizationService.Infrastructure.Persistence;

#nullable disable

namespace OrganizationService.Infrastructure.Migrations;

[DbContext(typeof(OrganizationDbContext))]
[Migration("20260731190000_Phase4_CommercialAdmin")]
public partial class Phase4_CommercialAdmin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "ExtensionCount",
            table: "LicenseKeys",
            type: "int",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.CreateTable(
            name: "CommercialAdminAuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OperatorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                OperatorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                TargetType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                TargetId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                Action = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                PreviousJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                NewJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CommercialAdminAuditEvents", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_CommercialAdminAuditEvents_OrganizationId_OccurredAtUtc",
            table: "CommercialAdminAuditEvents",
            columns: new[] { "OrganizationId", "OccurredAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CommercialAdminAuditEvents_Action_OccurredAtUtc",
            table: "CommercialAdminAuditEvents",
            columns: new[] { "Action", "OccurredAtUtc" });

        migrationBuilder.CreateTable(
            name: "EmailDeliveryLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                TemplateKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                ToAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Error = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EmailDeliveryLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_EmailDeliveryLogs_OrganizationId_SentAtUtc",
            table: "EmailDeliveryLogs",
            columns: new[] { "OrganizationId", "SentAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_EmailDeliveryLogs_Status_SentAtUtc",
            table: "EmailDeliveryLogs",
            columns: new[] { "Status", "SentAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CommercialAdminAuditEvents");
        migrationBuilder.DropTable(name: "EmailDeliveryLogs");
        migrationBuilder.DropColumn(name: "ExtensionCount", table: "LicenseKeys");
    }
}

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationService.Migrations
{
    /// <inheritdoc />
    public partial class M12_1_SyncRunAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WebhookDeliveries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AttemptCount",
                table: "SynchronizationRuns",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "AttemptCount",
                table: "SynchronizationRuns");
        }
    }
}

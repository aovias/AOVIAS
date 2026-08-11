using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntegrationService.Migrations
{
    /// <inheritdoc />
    public partial class M12_IntegrationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectorCredentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CredentialType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedPayload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorCredentials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorHealth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    SuccessRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FailureCount = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorHealth", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileFormat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OutputContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordsExported = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FieldMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MappingProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceField = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetField = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransformationRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FileFormat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContentJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreviewJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RollbackCheckpointJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecordsTotal = table.Column<int>(type: "int", nullable: false),
                    RecordsImported = table.Column<int>(type: "int", nullable: false),
                    RecordsFailed = table.Column<int>(type: "int", nullable: false),
                    ErrorReportJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    ErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OccurredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntegrationConnectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LifecycleState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HealthStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    ConfigurationJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CredentialId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntegrationConnectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MappingProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceEntity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetEntity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MappingProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RelayProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SynchronizationJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    MappingProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SyncType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConflictPolicy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronizationJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SynchronizationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SynchronizationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecordsProcessed = table.Column<int>(type: "int", nullable: false),
                    RecordsFailed = table.Column<int>(type: "int", nullable: false),
                    ConflictsDetected = table.Column<int>(type: "int", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Watermark = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TraceId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronizationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SynchronizationSchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SynchronizationJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CronExpression = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeZone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LastRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SynchronizationSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransformationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RulesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransformationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WebhookSubscriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayloadHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeadLetter = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookDeliveries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebhookSubscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecretHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventFilterJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebhookSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorHealth_ConnectorId",
                table: "ConnectorHealth",
                column: "ConnectorId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldMappings_MappingProfileId",
                table: "FieldMappings",
                column: "MappingProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationAuditLogs_OrganizationId_OccurredAtUtc",
                table: "IntegrationAuditLogs",
                columns: new[] { "OrganizationId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IntegrationConnectors_OrganizationId_Name",
                table: "IntegrationConnectors",
                columns: new[] { "OrganizationId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationRuns_SynchronizationJobId",
                table: "SynchronizationRuns",
                column: "SynchronizationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_SynchronizationSchedules_SynchronizationJobId",
                table: "SynchronizationSchedules",
                column: "SynchronizationJobId");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookDeliveries_WebhookSubscriptionId",
                table: "WebhookDeliveries",
                column: "WebhookSubscriptionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectorCredentials");

            migrationBuilder.DropTable(
                name: "ConnectorHealth");

            migrationBuilder.DropTable(
                name: "ExportJobs");

            migrationBuilder.DropTable(
                name: "FieldMappings");

            migrationBuilder.DropTable(
                name: "ImportJobs");

            migrationBuilder.DropTable(
                name: "IntegrationAuditLogs");

            migrationBuilder.DropTable(
                name: "IntegrationConnectors");

            migrationBuilder.DropTable(
                name: "MappingProfiles");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "SynchronizationJobs");

            migrationBuilder.DropTable(
                name: "SynchronizationRuns");

            migrationBuilder.DropTable(
                name: "SynchronizationSchedules");

            migrationBuilder.DropTable(
                name: "TransformationRules");

            migrationBuilder.DropTable(
                name: "WebhookDeliveries");

            migrationBuilder.DropTable(
                name: "WebhookSubscriptions");
        }
    }
}

namespace IntegrationService.Application.Contracts;

public sealed class ConnectorDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
    public string LifecycleState { get; init; } = string.Empty;
    public string HealthStatus { get; init; } = string.Empty;
    public int Version { get; init; }
    public string ConfigurationJson { get; init; } = "{}";
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CreateConnectorRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = "{}";
    public CreateCredentialRequest? Credential { get; set; }
}

public sealed class UpdateConnectorRequest
{
    public string Name { get; set; } = string.Empty;
    public string ConfigurationJson { get; set; } = "{}";
    public string LifecycleState { get; set; } = string.Empty;
}

public sealed class CreateCredentialRequest
{
    public string Name { get; set; } = string.Empty;
    public string CredentialType { get; set; } = "ApiKey";
    public string Secret { get; set; } = string.Empty;
}

public sealed class SynchronizationJobDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid SourceConnectorId { get; init; }
    public Guid MappingProfileId { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string SyncType { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}

public sealed class CreateSyncJobRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SourceConnectorId { get; set; }
    public Guid MappingProfileId { get; set; }
    public string Direction { get; set; } = "OneWay";
    public string SyncType { get; set; } = "Incremental";
    public string? CronExpression { get; set; }
}

public sealed class SynchronizationRunDto
{
    public Guid Id { get; init; }
    public Guid JobId { get; init; }
    public string Status { get; init; } = string.Empty;
    public int RecordsProcessed { get; init; }
    public int RecordsFailed { get; init; }
    public int ConflictsDetected { get; init; }
    public long DurationMs { get; init; }
    public string? TraceId { get; init; }
    public DateTime StartedAtUtc { get; init; }
}

public sealed class MappingProfileDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SourceEntity { get; init; } = string.Empty;
    public string TargetEntity { get; init; } = string.Empty;
    public IReadOnlyList<FieldMappingDto> Fields { get; init; } = Array.Empty<FieldMappingDto>();
}

public sealed class FieldMappingDto
{
    public Guid Id { get; init; }
    public string SourceField { get; init; } = string.Empty;
    public string TargetField { get; init; } = string.Empty;
    public Guid? TransformationRuleId { get; init; }
}

public sealed class CreateMappingProfileRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceEntity { get; set; } = "ExternalEmployee";
    public string TargetEntity { get; set; } = "Employment";
    public List<FieldMappingInput> Fields { get; set; } = [];
}

public sealed class FieldMappingInput
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public Guid? TransformationRuleId { get; set; }
}

public sealed class WebhookSubscriptionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string? TargetUrl { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class CreateWebhookRequest
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = "Outbound";
    public string? TargetUrl { get; set; }
    public string? Secret { get; set; }
    public IReadOnlyList<string>? EventFilters { get; set; }
    public Guid? TriggerSyncJobId { get; set; }
}

public sealed class WebhookDeliveryDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int AttemptCount { get; init; }
    public DateTime? DeliveredAtUtc { get; init; }
    public string? LastError { get; init; }
}

public sealed class ImportJobDto
{
    public Guid Id { get; init; }
    public string FileFormat { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int RecordsTotal { get; init; }
    public int RecordsImported { get; init; }
    public int RecordsFailed { get; init; }
    public object? Preview { get; init; }
}

public sealed class CreateImportRequest
{
    public Guid OrganizationId { get; set; }
    public Guid? MappingProfileId { get; set; }
    public string FileFormat { get; set; } = "Csv";
    public string? FileName { get; set; }
    public string Content { get; set; } = string.Empty;
}

public sealed class CreateExportRequest
{
    public Guid OrganizationId { get; set; }
    public string FileFormat { get; set; } = "Csv";
    public string? FilterJson { get; set; }
}

public sealed class ExportJobDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public int RecordsExported { get; init; }
    public string? OutputContent { get; init; }
}

public sealed class ConnectorHealthDto
{
    public Guid ConnectorId { get; init; }
    public string ConnectorName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long LatencyMs { get; init; }
    public decimal SuccessRate { get; init; }
    public DateTime CheckedAtUtc { get; init; }
}

public sealed class IntegrationAuditDto
{
    public Guid Id { get; init; }
    public string Operation { get; init; } = string.Empty;
    public string Result { get; init; } = string.Empty;
    public int RecordsProcessed { get; init; }
    public long DurationMs { get; init; }
    public string? TraceId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
}

public sealed class ExternalRecord
{
    public string ExternalId { get; set; } = string.Empty;
    public Dictionary<string, string?> Fields { get; set; } = new();
    public DateTime? LastModifiedUtc { get; set; }
    public string RecordHash { get; set; } = string.Empty;
}

public sealed class RollbackCheckpointEntry
{
    public string ExternalId { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public Guid PlatformId { get; set; }
}

public sealed class MappedRecord
{
    public string TargetEntity { get; set; } = string.Empty;
    public Dictionary<string, string?> Fields { get; set; } = new();
}

public sealed class ConnectorValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
}

public sealed class ConnectorHealthResult
{
    public string Status { get; init; } = "Unknown";
    public long LatencyMs { get; init; }
    public string? Message { get; init; }
}

public sealed class ConnectorPullResult
{
    public IReadOnlyList<ExternalRecord> Records { get; init; } = Array.Empty<ExternalRecord>();
    public string? Watermark { get; init; }
}

public sealed class SyncContext
{
    public Guid OrganizationId { get; init; }
    public Guid RunId { get; init; }
    public string? Watermark { get; init; }
    public string TraceId { get; init; } = string.Empty;
}

public sealed class IntegrationEventEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
}

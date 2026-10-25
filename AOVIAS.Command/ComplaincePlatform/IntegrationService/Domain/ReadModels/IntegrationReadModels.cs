namespace IntegrationService.Domain.ReadModels;

public class IntegrationConnector
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string LifecycleState { get; set; } = "Draft";
    public string HealthStatus { get; set; } = "Unknown";
    public int Version { get; set; } = 1;
    public string ConfigurationJson { get; set; } = "{}";
    public Guid? CredentialId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class ConnectorCredential
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CredentialType { get; set; } = "ApiKey";
    public string EncryptedPayload { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

public class SynchronizationJob
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid SourceConnectorId { get; set; }
    public Guid? TargetConnectorId { get; set; }
    public Guid MappingProfileId { get; set; }
    public string Direction { get; set; } = "OneWay";
    public string SyncType { get; set; } = "Incremental";
    public string ConflictPolicy { get; set; } = "ExternalWins";
    public bool IsEnabled { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SynchronizationSchedule
{
    public Guid Id { get; set; }
    public Guid SynchronizationJobId { get; set; }
    public string CronExpression { get; set; } = "0 */6 * * *";
    public string TimeZone { get; set; } = "UTC";
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunUtc { get; set; }
    public DateTime? NextRunUtc { get; set; }
}

public class SynchronizationRun
{
    public Guid Id { get; set; }
    public Guid SynchronizationJobId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Status { get; set; } = "Pending";
    public int RecordsProcessed { get; set; }
    public int RecordsFailed { get; set; }
    public int ConflictsDetected { get; set; }
    public long DurationMs { get; set; }
    public string? ErrorSummary { get; set; }
    public string? Watermark { get; set; }
    public string? TraceId { get; set; }
    public int AttemptCount { get; set; } = 1;
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class MappingProfile
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SourceEntity { get; set; } = "ExternalEmployee";
    public string TargetEntity { get; set; } = "Employment";
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class FieldMapping
{
    public Guid Id { get; set; }
    public Guid MappingProfileId { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public Guid? TransformationRuleId { get; set; }
    public int SortOrder { get; set; }
}

public class TransformationRule
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleType { get; set; } = "String";
    public string RulesJson { get; set; } = "{}";
}

public class WebhookSubscription
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Direction { get; set; } = "Outbound";
    public string? TargetUrl { get; set; }
    public string SecretHash { get; set; } = string.Empty;
    public string EventFilterJson { get; set; } = "[]";
    public int MaxRetries { get; set; } = 5;
    public bool IsEnabled { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class WebhookDelivery
{
    public Guid Id { get; set; }
    public Guid WebhookSubscriptionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string Status { get; set; } = "Pending";
    public int AttemptCount { get; set; }
    public string? LastError { get; set; }
    public bool IsDeadLetter { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAtUtc { get; set; }
}

public class ImportJob
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? ConnectorId { get; set; }
    public Guid? MappingProfileId { get; set; }
    public string FileFormat { get; set; } = "Csv";
    public string Status { get; set; } = "Pending";
    public string? FileName { get; set; }
    public string? ContentJson { get; set; }
    public string? PreviewJson { get; set; }
    public string? RollbackCheckpointJson { get; set; }
    public int RecordsTotal { get; set; }
    public int RecordsImported { get; set; }
    public int RecordsFailed { get; set; }
    public string? ErrorReportJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class ExportJob
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string FileFormat { get; set; } = "Csv";
    public string Status { get; set; } = "Pending";
    public string? FilterJson { get; set; }
    public string? OutputContent { get; set; }
    public int RecordsExported { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}

public class ConnectorHealth
{
    public Guid Id { get; set; }
    public Guid ConnectorId { get; set; }
    public string Status { get; set; } = "Unknown";
    public long LatencyMs { get; set; }
    public decimal SuccessRate { get; set; }
    public int FailureCount { get; set; }
    public string? ErrorSummary { get; set; }
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
}

public class IntegrationAuditLog
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ConnectorId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public int RecordsProcessed { get; set; }
    public string? ErrorsJson { get; set; }
    public string? TraceId { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public DateTime? RelayProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

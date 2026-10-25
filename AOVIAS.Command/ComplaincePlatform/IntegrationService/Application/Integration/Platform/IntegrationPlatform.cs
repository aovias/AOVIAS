using IntegrationService.Application.Contracts;
using IntegrationService.Domain.ReadModels;

namespace IntegrationService.Application.Integration.Platform;

public interface IConnectorProvider
{
    string ProviderType { get; }
    Task<ConnectorValidationResult> ValidateConfigurationAsync(string configurationJson, CancellationToken ct = default);
    Task<ConnectorHealthResult> CheckHealthAsync(IntegrationConnector connector, CancellationToken ct = default);
    Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default);
}

public interface IConnectorRegistry
{
    IConnectorProvider Resolve(string providerType);
    IReadOnlyList<string> ListProviderTypes();
}

public interface IMappingEngine
{
    Task<MappedRecord> MapAsync(ExternalRecord source, MappingProfile profile, IReadOnlyList<FieldMapping> fields, IReadOnlyList<TransformationRule> rules, CancellationToken ct = default);
    Task<ConnectorValidationResult> ValidateProfileAsync(MappingProfile profile, IReadOnlyList<FieldMapping> fields, CancellationToken ct = default);
}

public interface ISynchronizationEngine
{
    Task<SynchronizationRunDto> RunAsync(Guid jobId, string traceId, Guid? userId, CancellationToken ct = default);
}

public interface IWebhookService
{
    Task<IReadOnlyList<WebhookSubscriptionDto>> ListAsync(Guid organizationId, CancellationToken ct = default);
    Task<WebhookSubscriptionDto> CreateAsync(CreateWebhookRequest request, CancellationToken ct = default);
    Task<bool> ProcessInboundAsync(Guid subscriptionId, string payload, string signature, string? timestampHeader, string? idempotencyKey, CancellationToken ct = default);
    Task DeliverOutboundAsync(string eventType, object payload, Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookDeliveryDto>> ListDeliveriesAsync(Guid subscriptionId, CancellationToken ct = default);
}

public interface IImportExportEngine
{
    Task<ImportJobDto> CreateImportAsync(CreateImportRequest request, CancellationToken ct = default);
    Task<ImportJobDto> PreviewImportAsync(Guid importId, CancellationToken ct = default);
    Task<ImportJobDto> ExecuteImportAsync(Guid importId, Guid? userId, CancellationToken ct = default);
    Task<ImportJobDto> RollbackImportAsync(Guid importId, Guid? userId, CancellationToken ct = default);
    Task<ExportJobDto> CreateExportAsync(CreateExportRequest request, CancellationToken ct = default);
    Task<ExportJobDto> ExecuteExportAsync(Guid exportId, CancellationToken ct = default);
}

public interface IIntegrationAuditService
{
    Task LogAsync(Guid organizationId, string operation, string result, Guid? userId, Guid? connectorId, string source, string target, int records, long durationMs, string? traceId, string? errorsJson = null, CancellationToken ct = default);
}

public interface IIntegrationOutboxWriter
{
    Task EnqueueAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default);
}

public interface ICredentialProtector
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}

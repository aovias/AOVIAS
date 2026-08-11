using System.Net.Http.Json;
using System.Text.Json;
using IntegrationService.Application.Contracts;
using Shared.Infrastructure.DTO;

namespace IntegrationService.Infrastructure.Clients;

public sealed class PlatformWriteResult
{
    public Guid EntityId { get; init; }
    public bool WasCreated { get; init; }
    public string TargetEntity { get; init; } = string.Empty;
    public string? ExternalId { get; init; }
}

public sealed class PlatformWriteException : Exception
{
    public PlatformWriteException(string message, int statusCode, string? responseBody = null, Exception? inner = null)
        : base(message, inner)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public int StatusCode { get; }
    public string? ResponseBody { get; }
}

public interface IPlatformWriteClient
{
    Task<PlatformWriteResult> UpsertAsync(Guid organizationId, MappedRecord record, string? externalId = null, CancellationToken ct = default);
    Task RollbackAsync(Guid organizationId, string targetEntity, Guid entityId, CancellationToken ct = default);
}

public sealed class PlatformWriteClient(IHttpClientFactory httpFactory, ILogger<PlatformWriteClient> logger) : IPlatformWriteClient
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<PlatformWriteResult> UpsertAsync(Guid organizationId, MappedRecord record, string? externalId = null, CancellationToken ct = default)
    {
        var (service, path) = ResolveUpsertRoute(record.TargetEntity);
        var client = httpFactory.CreateClient($"Integration.{service}");
        var body = new IntegrationUpsertRequest
        {
            OrganizationId = organizationId,
            ExternalId = externalId,
            Fields = new Dictionary<string, string?>(record.Fields)
        };

        using var response = await client.PutAsJsonAsync(path, body, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Platform write failed for {TargetEntity} status {StatusCode} body {Body}",
                record.TargetEntity, (int)response.StatusCode, responseBody);
            throw new PlatformWriteException(
                $"Platform write failed for {record.TargetEntity}",
                (int)response.StatusCode,
                responseBody);
        }

        var envelope = JsonSerializer.Deserialize<ApiEnvelope<IntegrationUpsertResponse>>(responseBody, JsonOpts)
            ?? throw new PlatformWriteException("Invalid platform write response", (int)response.StatusCode, responseBody);
        if (envelope.Data is null)
            throw new PlatformWriteException("Platform write returned empty data", (int)response.StatusCode, responseBody);

        return new PlatformWriteResult
        {
            EntityId = envelope.Data.EntityId,
            WasCreated = envelope.Data.WasCreated,
            TargetEntity = record.TargetEntity,
            ExternalId = externalId
        };
    }

    public async Task RollbackAsync(Guid organizationId, string targetEntity, Guid entityId, CancellationToken ct = default)
    {
        var (service, path) = ResolveRollbackRoute(targetEntity, entityId);
        var client = httpFactory.CreateClient($"Integration.{service}");
        var body = new IntegrationRollbackRequest { OrganizationId = organizationId, EntityId = entityId, TargetEntity = targetEntity };
        using var response = await client.PostAsJsonAsync(path, body, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("Platform rollback failed for {TargetEntity}/{EntityId} status {StatusCode}",
                targetEntity, entityId, (int)response.StatusCode);
            throw new PlatformWriteException(
                $"Platform rollback failed for {targetEntity}",
                (int)response.StatusCode,
                responseBody);
        }
    }

    private static (string Service, string Path) ResolveUpsertRoute(string targetEntity) => targetEntity switch
    {
        "Person" => ("PersonService", "internal/persons/upsert"),
        "Employment" => ("WorkforceService", "internal/employment/upsert"),
        "Organization.Department" or "Department" => ("OrganizationService", "internal/departments/upsert"),
        _ => throw new PlatformWriteException($"Unsupported target entity: {targetEntity}", 400)
    };

    private static (string Service, string Path) ResolveRollbackRoute(string targetEntity, Guid entityId) => targetEntity switch
    {
        "Person" => ("PersonService", $"internal/persons/{entityId}/integration-rollback"),
        "Employment" => ("WorkforceService", $"internal/employment/{entityId}/integration-rollback"),
        "Organization.Department" or "Department" => ("OrganizationService", $"internal/departments/{entityId}/integration-rollback"),
        _ => throw new PlatformWriteException($"Unsupported rollback target: {targetEntity}", 400)
    };

    private sealed class ApiEnvelope<T>
    {
        public T? Data { get; set; }
    }
}

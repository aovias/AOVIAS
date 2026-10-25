using System.Text.Json;
using Aovias.Configuration.Deployment;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Registration;

/// <summary>
/// Sprint M4.3 — the persisted record of this environment's own Command Center Fleet Registry
/// identity (<see cref="EnvironmentId"/>/<see cref="ApiKey"/>), written to
/// <c>{ConfigRoot}/environment-registration.json</c> using the same atomic tmp-then-move write
/// <see cref="!:Aovias.Runtime.Agent.Infrastructure.Persistence.RuntimeManifestStore"/> and
/// <see cref="!:Aovias.Runtime.Agent.Infrastructure.Reconciliation.FileReconciliationHistoryStore"/>
/// already established — no new persistence pattern.
///
/// This is the entire mechanism by which registration "survives Runtime Agent restart": on the
/// next startup, <c>EnvironmentRegistrationHostedService</c> reads this file before deciding
/// whether to register at all, instead of unconditionally registering (which is exactly what would
/// create a duplicate Fleet Registry entry every restart).
/// </summary>
public sealed record EnvironmentRegistrationRecord
{
    public required string EnvironmentId { get; init; }

    public required string ApiKey { get; init; }

    public DateTimeOffset RegisteredUtc { get; init; } = DateTimeOffset.UtcNow;
}

public sealed class EnvironmentRegistrationRecordStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDeploymentPathResolver pathResolver;
    private readonly ILogger<EnvironmentRegistrationRecordStore> logger;

    public EnvironmentRegistrationRecordStore(IDeploymentPathResolver pathResolver, ILogger<EnvironmentRegistrationRecordStore> logger)
    {
        this.pathResolver = pathResolver;
        this.logger = logger;
    }

    private string RecordPath => Path.Combine(pathResolver.ConfigRoot, "environment-registration.json");

    public async Task<EnvironmentRegistrationRecord?> TryLoadAsync(CancellationToken cancellationToken = default)
    {
        var path = RecordPath;
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<EnvironmentRegistrationRecord>(stream, SerializerOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read environment registration record from {Path}; will re-register.", path);
            return null;
        }
    }

    public async Task SaveAsync(EnvironmentRegistrationRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        var path = RecordPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(record, SerializerOptions), cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }
}

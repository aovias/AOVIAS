using System.Text.Json;
using Aovias.Configuration.Deployment;
using Aovias.Deployment.Contracts.Admission;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Infrastructure.Admission;

/// <summary>
/// Sprint M4.4 — persists the latest <see cref="EnvironmentAdmissionReport"/> to
/// <c>{ConfigRoot}/environment-admission.json</c>, using the exact atomic tmp-then-move write
/// convention <c>EnvironmentRegistrationRecordStore</c> (Sprint M4.3) and
/// <c>FileReconciliationHistoryStore</c> (Sprint M2.6) already established — no new persistence
/// pattern. This is the "Admission result persistence" scope item: the report survives a Runtime
/// Agent restart and is readable on disk without recomputation, even though recomputation itself is
/// cheap and deterministic (Sprint M4.4 rule: "Admission must be deterministic").
/// </summary>
public sealed class EnvironmentAdmissionStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDeploymentPathResolver pathResolver;
    private readonly ILogger<EnvironmentAdmissionStore> logger;

    public EnvironmentAdmissionStore(IDeploymentPathResolver pathResolver, ILogger<EnvironmentAdmissionStore> logger)
    {
        this.pathResolver = pathResolver;
        this.logger = logger;
    }

    private string RecordPath => Path.Combine(pathResolver.ConfigRoot, "environment-admission.json");

    public async Task<EnvironmentAdmissionReport?> TryLoadAsync(CancellationToken cancellationToken = default)
    {
        var path = RecordPath;
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<EnvironmentAdmissionReport>(stream, SerializerOptions, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read environment admission report from {Path}; will recompute.", path);
            return null;
        }
    }

    public async Task SaveAsync(EnvironmentAdmissionReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var path = RecordPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
        await File.WriteAllTextAsync(tempPath, JsonSerializer.Serialize(report, SerializerOptions), cancellationToken);
        File.Move(tempPath, path, overwrite: true);
    }
}

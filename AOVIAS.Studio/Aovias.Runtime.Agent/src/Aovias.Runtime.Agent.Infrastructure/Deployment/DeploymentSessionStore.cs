using System.Text.Json;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Persists in-flight deployments so Runtime Agent restart or power interruption can resume cleanly.
/// </summary>
public sealed class DeploymentSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly RuntimeAgentDeploymentPathResolver pathResolver;
    private readonly string sessionRoot;

    public DeploymentSessionStore(RuntimeAgentDeploymentPathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
        sessionRoot = pathResolver.ResolveSessionStoreRoot();
    }

    public void Save(PlatformDeploymentRecord record)
    {
        if (record.State is PlatformDeploymentState.Completed
            or PlatformDeploymentState.Failed
            or PlatformDeploymentState.Cancelled)
        {
            Delete(record.DeploymentId);
            return;
        }

        pathResolver.EnsureDirectory(sessionRoot);

        var snapshot = new DeploymentSessionSnapshot
        {
            DeploymentId = record.DeploymentId,
            State = record.State,
            Operator = record.Operator,
            Runtime = record.Runtime,
            Environment = record.Environment,
            FailureMessage = record.FailureMessage,
            PersistedUtc = DateTimeOffset.UtcNow,
            Recoverable = IsRecoverable(record.State)
        };

        var path = GetPath(record.DeploymentId);
        File.WriteAllText(path, JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    public IReadOnlyList<DeploymentSessionSnapshot> ListRecoverable()
    {
        if (!Directory.Exists(sessionRoot))
        {
            return [];
        }

        var snapshots = new List<DeploymentSessionSnapshot>();
        foreach (var file in Directory.EnumerateFiles(sessionRoot, "*.json"))
        {
            try
            {
                var snapshot = JsonSerializer.Deserialize<DeploymentSessionSnapshot>(File.ReadAllText(file), JsonOptions);
                if (snapshot?.Recoverable == true)
                {
                    snapshots.Add(snapshot);
                }
            }
            catch (JsonException)
            {
                // Ignore corrupt session files.
            }
        }

        return snapshots;
    }

    public void Delete(string deploymentId)
    {
        var path = GetPath(deploymentId);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static bool IsRecoverable(PlatformDeploymentState state)
        => state is PlatformDeploymentState.Provisioning
            or PlatformDeploymentState.Deploying
            or PlatformDeploymentState.Verifying
            or PlatformDeploymentState.Uploading
            or PlatformDeploymentState.Preparing;

    private string GetPath(string deploymentId)
        => Path.Combine(sessionRoot, $"{deploymentId}.json");
}

/// <summary>
/// Persisted deployment session metadata for recovery.
/// </summary>
public sealed class DeploymentSessionSnapshot
{
    public required string DeploymentId { get; init; }

    public PlatformDeploymentState State { get; init; }

    public required string Operator { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }

    public string? FailureMessage { get; init; }

    public DateTimeOffset PersistedUtc { get; init; }

    public bool Recoverable { get; init; }
}

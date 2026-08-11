using System.Text.Json;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;
using Aovias.Publishing.Core.Execution;

namespace Aovias.Publishing.Core.Verification;

/// <summary>
/// Verifies a freshly built Runtime Agent Docker image contains Sprint 9+ deployment APIs.
/// </summary>
public interface IRuntimeAgentPackageVerifier
{
    Task<RuntimeAgentPackageVerificationResult> VerifyImageAsync(
        string imageName,
        string imageTag,
        string expectedSourceRevision,
        CancellationToken cancellationToken = default);
}

public sealed class RuntimeAgentPackageVerificationResult
{
    public bool Passed { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];

    public string? BuildManifestJson { get; init; }

    public static RuntimeAgentPackageVerificationResult Success(string? manifestJson)
        => new() { Passed = true, BuildManifestJson = manifestJson };

    public static RuntimeAgentPackageVerificationResult Failure(params string[] errors)
        => new() { Passed = false, Errors = errors };
}

public sealed class RuntimeAgentPackageVerifier : IRuntimeAgentPackageVerifier
{
    private const int RequiredPathPolicyVersion = 2;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IDockerCliExecutor dockerCliExecutor;
    private readonly IProcessRunner processRunner;

    public RuntimeAgentPackageVerifier(IDockerCliExecutor dockerCliExecutor, IProcessRunner processRunner)
    {
        this.dockerCliExecutor = dockerCliExecutor;
        this.processRunner = processRunner;
    }

    public async Task<RuntimeAgentPackageVerificationResult> VerifyImageAsync(
        string imageName,
        string imageTag,
        string expectedSourceRevision,
        CancellationToken cancellationToken = default)
    {
        var imageRef = $"{imageName}:{imageTag}";
        var manifest = await ReadBuildManifestAsync(imageRef, cancellationToken);
        if (manifest is null)
        {
            return RuntimeAgentPackageVerificationResult.Failure(
                "Runtime Agent build manifest was not found at /app/aovias-build-manifest.json.");
        }

        var errors = new List<string>();
        if (!manifest.DeploymentApi)
        {
            errors.Add("Runtime Agent image does not advertise deploymentApi=true.");
        }

        if (!string.Equals(manifest.SourceRevision, expectedSourceRevision, StringComparison.Ordinal))
        {
            errors.Add(
                $"Runtime Agent image source revision mismatch. Expected '{expectedSourceRevision}' but image contains '{manifest.SourceRevision}'. " +
                "The Docker build likely reused stale cached layers.");
        }

        if (!string.Equals(manifest.Assembly, RuntimeAgentConventions.EntryPointAssembly, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Unexpected runtime agent assembly '{manifest.Assembly}'.");
        }

        if (manifest.PathPolicyVersion < RequiredPathPolicyVersion)
        {
            errors.Add(
                $"Runtime Agent image pathPolicyVersion={manifest.PathPolicyVersion} is stale. "
                + $"Expected >= {RequiredPathPolicyVersion} (configured runtime paths, not /usr/share).");
        }

        if (!manifest.SignalRDiscovery)
        {
            errors.Add("Runtime Agent image does not advertise signalRDiscovery=true.");
        }

        return errors.Count == 0
            ? RuntimeAgentPackageVerificationResult.Success(JsonSerializer.Serialize(manifest, JsonOptions))
            : RuntimeAgentPackageVerificationResult.Failure(errors.ToArray());
    }

    private async Task<RuntimeAgentBuildManifest?> ReadBuildManifestAsync(
        string imageRef,
        CancellationToken cancellationToken)
    {
        var inspect = await dockerCliExecutor.InspectAsync(cancellationToken);
        if (!inspect.IsSuccess)
        {
            return null;
        }

        var result = await processRunner.RunAsync(
            "docker",
            $"run --rm --entrypoint cat {Quote(imageRef)} /app/aovias-build-manifest.json",
            Environment.CurrentDirectory,
            120,
            cancellationToken);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return null;
        }

        return JsonSerializer.Deserialize<RuntimeAgentBuildManifest>(result.StandardOutput, JsonOptions);
    }

    private static string Quote(string value) => value.Contains(' ') ? $"\"{value}\"" : value;

    private sealed class RuntimeAgentBuildManifest
    {
        public string ServiceId { get; init; } = string.Empty;

        public string SourceRevision { get; init; } = string.Empty;

        public string BuildUtc { get; init; } = string.Empty;

        public bool DeploymentApi { get; init; }

        public int PathPolicyVersion { get; init; }

        public bool SignalRDiscovery { get; init; }

        public string Assembly { get; init; } = string.Empty;
    }
}

/// <summary>
/// Creates a unique source revision token for Runtime Agent Docker builds.
/// </summary>
[Obsolete("Use PublishingBuildIdentityService.EstablishPreDocker for deterministic source revisions.")]
public static class RuntimeAgentBuildRevision
{
    [Obsolete("Use PublishingBuildIdentityService.EstablishPreDocker for deterministic source revisions.")]
    public static string Create() => Guid.NewGuid().ToString("N");
}

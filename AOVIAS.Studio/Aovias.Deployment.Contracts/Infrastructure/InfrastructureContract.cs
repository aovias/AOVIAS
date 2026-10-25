namespace Aovias.Deployment.Contracts.Infrastructure;

using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Migration;
using Aovias.Deployment.Contracts.Provisioning;

/// <summary>
/// Unified infrastructure requirements for a deployable service.
///
/// Schema v2 (Sprint M2.2) adds an explicit version envelope — schema version, contract
/// identity, compatibility metadata, migration metadata, integrity metadata, publishing
/// engine version, source revision, and generated-UTC — around the same domain model schema
/// v1 (M2.0/M2.1) already established. <see cref="SchemaVersion"/> defaults to
/// <see cref="ContractSchemaVersions.V1"/> specifically so that deserializing a legacy file
/// that never had a <c>schemaVersion</c> key is correctly detected as v1, not silently treated
/// as current — Publishing always sets this field explicitly to
/// <see cref="ContractSchemaVersions.Current"/> when it writes a contract.
/// </summary>
public sealed class InfrastructureContract
{
    /// <summary>
    /// The wire schema version. Absent in every schema v1 (M2.0/M2.1) package, which is why the
    /// default is <see cref="ContractSchemaVersions.V1"/> rather than <see cref="ContractSchemaVersions.Current"/>.
    /// </summary>
    public int SchemaVersion { get; init; } = ContractSchemaVersions.V1;

    public string ServiceId { get; init; } = string.Empty;

    public string ContractVersion { get; init; } = "1.0";

    /// <summary>Which service and version this contract belongs to (Sprint M2.2 D1).</summary>
    public ContractIdentity Identity { get; init; } = new();

    /// <summary>The schema version range this contract instance is compatible with (Sprint M2.2 D1/D4).</summary>
    public ContractCompatibilityMetadata Compatibility { get; init; } = new();

    /// <summary>Non-null only when this instance was produced by a migrator, not authored natively (Sprint M2.2 D1/D3).</summary>
    public ContractMigrationMetadata? Migration { get; init; }

    /// <summary>A content hash of the domain payload, independent of package-level checksums (Sprint M2.2 D1).</summary>
    public ContractIntegrityMetadata Integrity { get; init; } = new();

    /// <summary>The Publishing engine build that compiled this contract. Empty for contracts migrated from schema v1.</summary>
    public string PublishingEngineVersion { get; init; } = string.Empty;

    /// <summary>The deterministic source revision of the publish run. Empty for contracts migrated from schema v1.</summary>
    public string SourceRevision { get; init; } = string.Empty;

    /// <summary>
    /// When Publishing generated this contract, taken from the publish run's fixed build
    /// identity — never a fresh clock read at serialization or read time (see Migration Policy
    /// for why that distinction matters). Default for contracts migrated from schema v1.
    /// </summary>
    public DateTimeOffset GeneratedUtc { get; init; }

    public FilesystemContract Filesystem { get; init; } = new();

    public CertificateContract Certificates { get; init; } = new();

    public SecretContract Secrets { get; init; } = new();

    public DatabaseContract Database { get; init; } = new();

    public DockerInfrastructureContract Docker { get; init; } = new();

    public NetworkContract Network { get; init; } = new();

    public VolumeContract Storage { get; init; } = new();

    public IReadOnlyList<ExternalProviderContract> Providers { get; init; } = [];

    public DependencyContract Dependencies { get; init; } = new();

    public HealthContract Health { get; init; } = new() { HealthUrl = "/health" };

    /// <summary>
    /// Provider-agnostic infrastructure requirements this service declares (Sprint M2.3 D1/D7).
    /// Publishing compiles these from the specification unchanged — it never resolves them
    /// against an environment's capabilities; that is the Deployment Planner's job, at
    /// deployment time, via <see cref="Capabilities.RequirementCapabilityMatcher"/>. Additive to
    /// schema v2: absent on every contract published before this sprint, and on every contract
    /// migrated from schema v1, which is why it defaults to an empty list rather than being
    /// <see langword="required"/>.
    /// </summary>
    public IReadOnlyList<Requirement> Requirements { get; init; } = [];
}

public sealed class DockerInfrastructureContract
{
    public bool RequireEngine { get; init; } = true;

    public bool RequireCompose { get; init; } = true;

    public string ImageReference { get; init; } = string.Empty;

    public string RestartPolicy { get; init; } = "unless-stopped";

    public bool CreateNetworkIfMissing { get; init; } = true;

    public IReadOnlyList<string> RequiredImages { get; init; } = [];
}

public sealed class ExternalProviderContract
{
    public required string Id { get; init; }

    public required string Provider { get; init; }

    public bool Required { get; init; }

    public string SecretName { get; init; } = string.Empty;

    public string ConfigurationKey { get; init; } = string.Empty;

    public string ValidationRule { get; init; } = "non-empty";
}

public enum InfrastructureProvisionState
{
    Pending,
    Provisioning,
    Validating,
    Ready,
    Skipped,
    Failed
}

public sealed class InfrastructureStageResult
{
    public required string Stage { get; init; }

    public InfrastructureProvisionState State { get; init; }

    public bool Passed { get; init; }

    public IReadOnlyList<string> Messages { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> RecoveryActions { get; init; } = [];

    public static InfrastructureStageResult Ready(string stage, params string[] messages)
        => new() { Stage = stage, State = InfrastructureProvisionState.Ready, Passed = true, Messages = messages };

    public static InfrastructureStageResult Skipped(string stage, params string[] messages)
        => new() { Stage = stage, State = InfrastructureProvisionState.Skipped, Passed = true, Messages = messages };

    public static InfrastructureStageResult Failed(string stage, IReadOnlyList<string> errors, IReadOnlyList<string>? recoveryActions = null)
        => new()
        {
            Stage = stage,
            State = InfrastructureProvisionState.Failed,
            Passed = false,
            Errors = errors,
            RecoveryActions = recoveryActions ?? []
        };
}

public sealed class InfrastructureReadinessReport
{
    public required string ServiceId { get; init; }

    public required string Version { get; init; }

    public bool DeploymentAllowed { get; init; }

    public IReadOnlyList<InfrastructureStageResult> Stages { get; init; } = [];

    public IReadOnlyList<string> Blockers { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public DateTimeOffset GeneratedUtc { get; init; } = DateTimeOffset.UtcNow;

    public static InfrastructureReadinessReport Create(
        ServiceDeploymentBundle bundle,
        IReadOnlyList<InfrastructureStageResult> stages,
        IReadOnlyList<string> warnings)
    {
        var blockers = stages
            .Where(stage => !stage.Passed)
            .SelectMany(stage => stage.Errors.Select(error => $"{stage.Stage}: {error}"))
            .ToList();

        return new InfrastructureReadinessReport
        {
            ServiceId = bundle.Manifest.ServiceId,
            Version = bundle.Manifest.Version,
            DeploymentAllowed = blockers.Count == 0,
            Stages = stages,
            Blockers = blockers,
            Warnings = warnings
        };
    }
}

public sealed class InfrastructureProvisioningContext
{
    public required ServiceDeploymentBundle Bundle { get; init; }

    public required InfrastructureContract Contract { get; init; }

    public required HostPathContext HostPaths { get; init; }

    public RuntimeCapabilities RuntimeCapabilities { get; init; } = new();

    /// <summary>
    /// The target environment's declared capabilities (Sprint M2.3 D8), used by
    /// <see cref="Provisioners.DependencyInfrastructureProvisioner"/> to evaluate
    /// <see cref="InfrastructureContract.Requirements"/> via <see cref="RequirementCapabilityMatcher"/>.
    /// Optional and empty by default: neither DRV nor Runtime Agent populate this today, so the
    /// provisioner falls back to <see cref="LegacyCapabilityBridge.FromRuntimeCapabilities"/> to
    /// derive an equivalent capability list from <see cref="RuntimeCapabilities"/> whenever this
    /// is left empty — existing callers require zero source changes to get requirement matching
    /// for free.
    /// </summary>
    public IReadOnlyList<Capability> AvailableCapabilities { get; init; } = [];

    public IInfrastructureExecutionContext? Execution { get; init; }

    public string? PackageDirectory { get; init; }

    public string? DockerNetworkName { get; init; }

    public IList<InfrastructureStageResult> StageResults { get; } = [];
}

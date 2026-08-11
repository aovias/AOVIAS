namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Registered deployment environment known to AOVIAS Command.
///
/// Sprint M2.7: this is the Fleet Registry entry type the Operations Command Center uses
/// directly — it was already shaped for that purpose since Sprint M2.0 (see the class remarks
/// above), so M2.7 extends it additively (<see cref="Labels"/>, <see cref="MaintenanceMode"/>,
/// <see cref="MaintenanceReason"/>, <see cref="LastHeartbeatUtc"/>) rather than introducing a
/// second, parallel "fleet entry" type. Changed from a plain <c>sealed class</c> to a
/// <c>sealed record</c> so the Command Center's Fleet Registry can use <c>with</c> expressions to
/// produce updated copies (e.g. after a heartbeat) — the same "record for <c>with</c>-expression
/// convenience" precedent <c>HealthContract</c> already established in this codebase. Every
/// existing <c>{ Prop = value }</c> object-initializer call site remains valid; only reference
/// equality (never relied upon for this DTO) changes to value equality.
/// </summary>
public sealed record RemoteEnvironment
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    public required string BaseUrl { get; init; }

    public required string RuntimeAgentEndpoint { get; init; }

    public EnvironmentAuthenticationMode AuthenticationMode { get; init; } = EnvironmentAuthenticationMode.ApiKey;

    public string? ApiKey { get; init; }

    public string? RegistrationToken { get; init; }

    public EnvironmentType EnvironmentType { get; init; }

    public string RuntimeVersion { get; init; } = string.Empty;

    public string PlatformVersion { get; init; } = string.Empty;

    public string HealthStatus { get; init; } = "Unknown";

    public DateTimeOffset? LastDeploymentUtc { get; init; }

    public DateTimeOffset? LastRollbackUtc { get; init; }

    public bool IsRegistered { get; init; }

    public DateTimeOffset? RegisteredUtc { get; init; }

    /// <summary>Free-form operator-assigned labels/tags (Sprint M2.7) — the basis for fleet-wide search/filter.</summary>
    public IReadOnlyDictionary<string, string> Labels { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether this environment is currently in maintenance mode (Sprint M2.7) — mirrors the value the Runtime Agent itself holds via its own <c>MaintenanceModeStore</c>; the Command Center's copy is a cache, not the source of truth (the Runtime Agent is).</summary>
    public bool MaintenanceMode { get; init; }

    public string? MaintenanceReason { get; init; }

    /// <summary>When the Command Center last successfully received a heartbeat from this environment (Sprint M2.7) — <see langword="null"/> means never, which the Fleet Status Service treats as offline.</summary>
    public DateTimeOffset? LastHeartbeatUtc { get; init; }

    /// <summary>Sprint M4.3: this environment's declared deployment capabilities — set at registration time from <see cref="FleetRegistrationRequest.Capabilities"/> and kept fresh afterward by <c>RuntimeAgentPollingService</c> copying <see cref="RuntimeHeartbeatDto.Capabilities"/> on every poll, the same way <see cref="RuntimeVersion"/> already is. No new sync mechanism — this rides the existing heartbeat loop.</summary>
    public IReadOnlyList<string> Capabilities { get; init; } = [];

    /// <summary>Sprint M4.4: this environment's Deployment Ready state, kept fresh by <c>RuntimeAgentPollingService</c> copying <see cref="RuntimeHeartbeatDto.DeploymentReady"/> on every poll — the same heartbeat loop <see cref="Capabilities"/> already rides, not a second sync mechanism.</summary>
    public bool DeploymentReady { get; init; } = true;

    /// <summary>Sprint M4.4: the number of blocking admission diagnostics behind <see cref="DeploymentReady"/>, synced the same way.</summary>
    public int AdmissionBlockerCount { get; init; }
}

/// <summary>
/// Runtime Agent registration request payload.
/// </summary>
public sealed class AgentRegistrationRequest
{
    public required string RegistrationToken { get; init; }

    public required string EnvironmentName { get; init; }

    public EnvironmentType EnvironmentType { get; init; }

    public required string RuntimeVersion { get; init; }

    public required string Hostname { get; init; }

    public required string PublicUrl { get; init; }
}

/// <summary>
/// Runtime Agent registration response payload.
/// </summary>
public sealed class AgentRegistrationResponse
{
    public required string EnvironmentId { get; init; }

    public required string ApiKey { get; init; }

    public required string RuntimeAgentEndpoint { get; init; }

    public DateTimeOffset RegisteredUtc { get; init; }
}

/// <summary>
/// Explicit deployment execution mode. Controls which packages the planner and
/// package resolver may include — infrastructure packages are never injected into
/// Application or Service deployments.
/// </summary>
public enum PlatformDeploymentMode
{
    /// <summary>Full platform: infrastructure (including Runtime Agent) plus all application services.</summary>
    Platform = 0,

    /// <summary>One or more application services. Runtime Agent package is never required.</summary>
    Application = 1,

    /// <summary>Exactly one application or infrastructure service as selected by the operator.</summary>
    Service = 2
}

/// <summary>
/// Request to deploy a platform, application set, or single service from a solution manifest.
/// </summary>
public sealed class PlatformDeploymentRequest
{
    public required string RequestedBy { get; init; }

    public required string Runtime { get; init; }

    public required string Environment { get; init; }

    public required PlatformDeploymentManifest Manifest { get; init; }

    public IReadOnlyDictionary<string, string> PackagePaths { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool OperatorConfirmed { get; init; } = true;

    public bool ApprovalGranted { get; init; } = true;

    /// <summary>
    /// When null/empty with <see cref="DeploymentMode"/> = Platform, every deployment-ready
    /// service is included. For Application/Service modes this is the operator selection
    /// (application dependencies may be expanded; Runtime Agent is never auto-injected).
    /// </summary>
    public IReadOnlyList<string>? SelectedServiceIds { get; init; }

    /// <summary>Execution mode. When omitted, inferred from <see cref="SelectedServiceIds"/>.</summary>
    public PlatformDeploymentMode? DeploymentMode { get; init; }
}

/// <summary>
/// Response when a platform deployment is accepted into the queue.
/// </summary>
public sealed class PlatformDeploymentResponse
{
    public required string DeploymentId { get; init; }

    public PlatformDeploymentState State { get; init; }

    public string Message { get; init; } = string.Empty;

    public IReadOnlyList<ServiceDeploymentStatus> Services { get; init; } = [];
}

/// <summary>
/// Per-service deployment progress within a platform deployment.
/// </summary>
public sealed class ServiceDeploymentStatus
{
    public required string ServiceId { get; init; }

    public required string Name { get; init; }

    public string Status { get; set; } = "Pending";

    public string? ReleaseId { get; set; }

    public string? WorkflowId { get; set; }

    public string? HealthStatus { get; set; }

    public DeploymentFailureCategory? FailureCategory { get; set; }

    public string? FailureMessage { get; set; }
}

/// <summary>
/// Live deployment status snapshot.
/// </summary>
public sealed class DeploymentStatusResponse
{
    public required string DeploymentId { get; init; }

    public PlatformDeploymentState State { get; init; }

    public int ProgressPercent { get; init; }

    public string CurrentPhase { get; init; } = string.Empty;

    public string? FailureMessage { get; init; }

    public DeploymentFailureCategory? FailureCategory { get; init; }

    public IReadOnlyList<ServiceDeploymentStatus> Services { get; init; } = [];

    public InfrastructureReadinessSnapshot Infrastructure { get; init; } = new();

    public PlatformVersionSnapshot Version { get; init; } = new();

    public DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }
}

/// <summary>
/// Infrastructure readiness gates for platform health verification.
/// </summary>
public sealed class InfrastructureReadinessSnapshot
{
    public string Infrastructure { get; set; } = "Pending";

    public string Contracts { get; set; } = "Pending";

    public string Containers { get; set; } = "Pending";

    public string Health { get; set; } = "Pending";

    public string Dependencies { get; set; } = "Pending";

    public string Runtime { get; set; } = "Pending";
}

/// <summary>
/// Installed vs target platform version snapshot.
/// </summary>
public sealed class PlatformVersionSnapshot
{
    public string InstalledPlatformVersion { get; set; } = string.Empty;

    public string TargetPlatformVersion { get; set; } = string.Empty;

    public string RuntimeVersion { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, string> PackageVersions { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> ContractVersions { get; init; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public bool HasDrift { get; set; }
}

/// <summary>
/// Rollback plan generated before deployment begins.
/// </summary>
public sealed class PlatformRollbackPlan
{
    public required string DeploymentId { get; init; }

    public IReadOnlyList<RollbackServiceSnapshot> Services { get; init; } = [];

    public DateTimeOffset GeneratedUtc { get; init; }
}

/// <summary>
/// Previous release snapshot used during rollback.
/// </summary>
public sealed class RollbackServiceSnapshot
{
    public required string ServiceId { get; init; }

    public string? PreviousReleaseId { get; init; }

    public string? PreviousImage { get; init; }

    public string? PreviousPackageVersion { get; init; }
}

/// <summary>
/// Deployment history record for audit and operator review.
/// </summary>
public sealed class PlatformDeploymentHistoryEntry
{
    public required string DeploymentId { get; init; }

    public required string PlatformVersion { get; init; }

    public required string Operator { get; init; }

    public required string Environment { get; init; }

    public TimeSpan Duration { get; init; }

    public IReadOnlyList<string> Packages { get; init; } = [];

    public string HealthStatus { get; init; } = "Unknown";

    public bool RolledBack { get; init; }

    public PlatformDeploymentState FinalState { get; init; }

    public DateTimeOffset StartedUtc { get; init; }

    public DateTimeOffset? CompletedUtc { get; init; }

    public DeploymentFailureCategory? FailureCategory { get; init; }
}

/// <summary>
/// Deployment report artifact returned after completion.
/// </summary>
public sealed class PlatformDeploymentReport
{
    public required string DeploymentId { get; init; }

    public DeploymentStatusResponse Status { get; init; } = null!;

    public PlatformRollbackPlan? RollbackPlan { get; init; }

    public IReadOnlyList<string> Logs { get; init; } = [];

    public IReadOnlyList<string> AuditTrail { get; init; } = [];

    public PlatformCertificationReport? Certification { get; init; }
}

/// <summary>
/// Environment health snapshot for dashboard display.
/// </summary>
public sealed class EnvironmentHealthSnapshot
{
    public required string EnvironmentId { get; init; }

    public required string Name { get; init; }

    public string HealthStatus { get; init; } = "Unknown";

    public double CpuUsagePercent { get; init; }

    public double MemoryUsagePercent { get; init; }

    public double DiskUsagePercent { get; init; }

    public string DockerStatus { get; init; } = "Unknown";

    public int RunningContainerCount { get; init; }

    public int ContainerCount { get; init; }

    public string PlatformVersion { get; init; } = string.Empty;

    public DateTimeOffset? LastDeploymentUtc { get; init; }
}

/// <summary>
/// Drift detection report for reconciliation.
/// </summary>
public sealed class EnvironmentDriftReport
{
    public required string EnvironmentId { get; init; }

    public DateTimeOffset GeneratedUtc { get; init; }

    public IReadOnlyList<EnvironmentDriftFinding> Findings { get; init; } = [];

    public bool RequiresReconciliation { get; init; }
}

/// <summary>
/// Individual drift finding.
/// </summary>
public sealed class EnvironmentDriftFinding
{
    public required string Category { get; init; }

    public required string Resource { get; init; }

    public required string Expected { get; init; }

    public required string Actual { get; init; }

    public string Severity { get; init; } = "Warning";
}

/// <summary>
/// Chunked upload session metadata.
/// </summary>
public sealed class ChunkedUploadSession
{
    public required string UploadId { get; init; }

    /// <summary>
    /// Correlation id introduced with resumable protocol v1. Empty when deserializing
    /// responses from pre-v1 Runtime Agents so the client can report version drift cleanly.
    /// </summary>
    public string DeploymentId { get; init; } = string.Empty;

    public int ProtocolVersion { get; init; }

    public required string FileName { get; init; }

    public long TotalBytes { get; init; }

    public string Checksum { get; init; } = string.Empty;

    public int ChunkSizeBytes { get; init; }

    public int ReceivedChunks { get; init; }

    public IReadOnlyList<int> ReceivedChunkIndices { get; init; } = [];

    public int TotalChunks { get; init; }

    public long BytesReceived { get; init; }

    public int ProgressPercent { get; init; }

    public string State { get; init; } = "Uploading";

    public DateTimeOffset CreatedUtc { get; init; }

    public DateTimeOffset UpdatedUtc { get; init; }

    public bool Completed { get; init; }

    public string? PackagePath { get; init; }
}

/// <summary>
/// Initiates a chunked package upload.
/// </summary>
public sealed class InitiateChunkedUploadRequest
{
    public int ProtocolVersion { get; init; } = 1;

    public required string DeploymentId { get; init; }

    public string ServiceId { get; init; } = string.Empty;

    public required string FileName { get; init; }

    public long TotalBytes { get; init; }

    public required string Checksum { get; init; }

    public int ChunkSizeBytes { get; init; } = 4 * 1024 * 1024;
}

/// <summary>
/// Metadata supplied with one idempotent upload chunk.
/// </summary>
public sealed class UploadChunkMetadata
{
    public required string DeploymentId { get; init; }

    public required string Checksum { get; init; }

    public long ContentLength { get; init; }
}

/// <summary>
/// Byte-level progress reported while a deployment package is transferred.
/// </summary>
public sealed class DeploymentUploadProgress
{
    public required string DeploymentId { get; init; }

    public required string ServiceId { get; init; }

    public required string FileName { get; init; }

    public int ChunkIndex { get; init; }

    public int TotalChunks { get; init; }

    public long BytesUploaded { get; init; }

    public long TotalBytes { get; init; }

    public double BytesPerSecond { get; init; }

    public TimeSpan? EstimatedRemaining { get; init; }

    public int RetryAttempt { get; init; }

    public string Stage { get; init; } = "Uploading";
}

/// <summary>
/// Cancels an active deployment.
/// </summary>
public sealed class CancelDeploymentRequest
{
    public required string DeploymentId { get; init; }

    public required string RequestedBy { get; init; }
}

/// <summary>
/// Rolls back a platform deployment.
/// </summary>
public sealed class PlatformRollbackRequest
{
    public required string DeploymentId { get; init; }

    public required string RequestedBy { get; init; }
}

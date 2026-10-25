namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// A first-class, provider-agnostic infrastructure requirement (Sprint M2.3 D1). A service
/// declares <em>what</em> it needs — a category of infrastructure — never <em>how</em> that
/// need is fulfilled. "Message Broker" is a requirement category; "RabbitMQ" is a
/// <see cref="Capability"/> that satisfies it. Whether a given environment can satisfy this
/// requirement is decided later, at deployment time, by <see cref="RequirementCapabilityMatcher"/>
/// — Publishing compiles requirements into the contract without resolving them (D7).
/// </summary>
public sealed class Requirement
{
    /// <summary>Stable identifier for this requirement within a service (e.g. <c>"primary-database"</c>).</summary>
    public required string Id { get; init; }

    /// <summary>
    /// The open-taxonomy category this requirement belongs to (e.g. <c>"sql-database"</c>,
    /// <c>"message-broker"</c>). Deliberately a string, not an enum — see
    /// <see cref="RequirementCategories"/> for the well-known values today. A new requirement
    /// category is new data, never a breaking model change, which is the exact property the
    /// closed-world <c>FeatureContract</c>/<c>RuntimeCapabilities</c> booleans lacked.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>Whether deployment must be blocked if no capability satisfies this requirement.</summary>
    public bool Mandatory { get; init; } = true;

    /// <summary>
    /// An optional version constraint on the satisfying capability (e.g. <c>">=13.0"</c>,
    /// <c>"^8.0"</c>, <c>"==7.0"</c>). <see langword="null"/> means any version is acceptable.
    /// Evaluated by <see cref="VersionConstraint"/>.
    /// </summary>
    public string? VersionConstraint { get; init; }

    public string Description { get; init; } = string.Empty;

    /// <summary>Named validation rules the matcher or a future policy engine may enforce beyond category/version (e.g. <c>"connection-string-required"</c>).</summary>
    public IReadOnlyList<string> ValidationRules { get; init; } = [];

    /// <summary>Compatibility metadata: additional categories, beyond <see cref="Category"/>, whose capabilities may also satisfy this requirement.</summary>
    public RequirementCompatibilityMetadata Compatibility { get; init; } = new();
}

/// <summary>Compatibility metadata for a <see cref="Requirement"/> (Sprint M2.3 D1).</summary>
public sealed class RequirementCompatibilityMetadata
{
    /// <summary>
    /// Category aliases this requirement also accepts a match from, beyond its primary
    /// <see cref="Requirement.Category"/> (e.g. a requirement categorized <c>"message-broker"</c>
    /// might also accept a capability categorized <c>"event-stream"</c>).
    /// </summary>
    public IReadOnlyList<string> AcceptedCategories { get; init; } = [];
}

/// <summary>
/// Well-known requirement categories (Sprint M2.3 D1). These are conveniences, not a closed
/// set — <see cref="Requirement.Category"/> accepts any string, and
/// <see cref="Aovias.Deployment.Contracts.Capabilities.CapabilityRegistry"/> is the authority on
/// which categories a real capability currently satisfies.
/// </summary>
public static class RequirementCategories
{
    public const string SqlDatabase = "sql-database";
    public const string MessageBroker = "message-broker";
    public const string PersistentStorage = "persistent-storage";
    public const string TlsCertificates = "tls-certificates";
    public const string ObjectStorage = "object-storage";
    public const string ExternalAiProvider = "external-ai-provider";
    public const string ReverseProxy = "reverse-proxy";
    public const string SecretsProvider = "secrets-provider";
    public const string Metrics = "metrics";
    public const string Logging = "logging";
    public const string HealthMonitoring = "health-monitoring";
    public const string NetworkConnectivity = "network-connectivity";

    public static IReadOnlyList<string> WellKnown { get; } =
    [
        SqlDatabase, MessageBroker, PersistentStorage, TlsCertificates, ObjectStorage,
        ExternalAiProvider, ReverseProxy, SecretsProvider, Metrics, Logging, HealthMonitoring,
        NetworkConnectivity
    ];

    /// <summary>
    /// Sprint M4.4 — categories a service fulfills at runtime via externally supplied
    /// configuration/secrets (an API key, an external endpoint) rather than via any capability an
    /// environment's own <see cref="EnvironmentCapabilityProfile"/> could ever declare. Today this
    /// is exactly <see cref="ExternalAiProvider"/> — the one well-known category whose
    /// <c>capability-registry.json</c> entries (<c>azure-openai</c>, <c>sarvam-ai</c>) represent a
    /// remote third-party service, not something a Docker host or VPS provisions locally. Used by
    /// <c>EnvironmentAdmissionEvaluator</c> so an environment is never marked Not Ready over a
    /// requirement no environment profile is meant to satisfy.
    /// </summary>
    public static IReadOnlyList<string> ExternallyProvisionedCategories { get; } = [ExternalAiProvider];
}

/// <summary>Thrown by <see cref="RequirementValidator"/> when one or more requirements are invalid (Sprint M2.3 D9).</summary>
public sealed class RequirementValidationException : InvalidOperationException
{
    public RequirementValidationException(IReadOnlyList<string> errors)
        : base("Requirement declaration is invalid: " + string.Join("; ", errors)) => Errors = errors;

    public IReadOnlyList<string> Errors { get; }
}

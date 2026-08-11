namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// Something a deployment environment provides, capable of satisfying one or more
/// <see cref="Requirement"/> categories (Sprint M2.3 D2). Capabilities are provider-specific by
/// nature (PostgreSQL, RabbitMQ, Kubernetes Service…) — that specificity is exactly what a
/// <see cref="Requirement"/> is designed to hide from the service author.
/// </summary>
public sealed class Capability
{
    /// <summary>Stable identifier, authoritative within <see cref="CapabilityRegistry"/> (e.g. <c>"postgresql"</c>).</summary>
    public required string Id { get; init; }

    /// <summary>The primary requirement category this capability satisfies (e.g. <c>"sql-database"</c>).</summary>
    public required string Category { get; init; }

    /// <summary>Additional requirement categories this same capability also satisfies (e.g. a broker satisfying both <c>"message-broker"</c> and <c>"event-stream"</c>).</summary>
    public IReadOnlyList<string> SatisfiesCategories { get; init; } = [];

    /// <summary>The capability's own version, evaluated against a requirement's <see cref="Requirement.VersionConstraint"/>.</summary>
    public string Version { get; init; } = "0.0.0";

    /// <summary>Alternate names this capability is also known by (e.g. <c>"postgres"</c>, <c>"psql"</c> for <c>"postgresql"</c>).</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>
    /// The M2.0-era <c>ExternalProviderContract.Provider</c> string this capability corresponds
    /// to, where one exists (e.g. <c>"RabbitMQ"</c>) — a bridge back to the provider domain that
    /// predates this sprint, kept so the two models can coexist without duplication.
    /// </summary>
    public string? ProviderMapping { get; init; }

    /// <summary>Other capability ids this capability depends on being present (Sprint M2.3 D9 circular-dependency check).</summary>
    public IReadOnlyList<string> DependsOnCapabilityIds { get; init; } = [];

    public string Description { get; init; } = string.Empty;
}

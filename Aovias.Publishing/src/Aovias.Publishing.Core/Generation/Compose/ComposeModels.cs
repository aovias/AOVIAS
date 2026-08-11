namespace Aovias.Publishing.Core.Generation.Compose;

/// <summary>
/// Strongly-typed, schema-driven representation of a docker-compose file.
/// There is no <c>version</c> field (obsolete in Compose v2+). The model is
/// serialized deterministically by <see cref="ComposeYamlSerializer"/>.
/// </summary>
public sealed class ComposeDocument
{
    /// <summary>Ordered services keyed by service name.</summary>
    public List<ComposeService> Services { get; } = [];

    /// <summary>Ordered networks keyed by network alias.</summary>
    public List<ComposeNetwork> Networks { get; } = [];
}

/// <summary>A single compose service definition.</summary>
public sealed class ComposeService
{
    public required string Name { get; init; }

    public required string Image { get; init; }

    public required string ContainerName { get; init; }

    public string Restart { get; init; } = "unless-stopped";

    public List<string> Ports { get; } = [];

    /// <summary>Ordered environment variables (preserves declaration order).</summary>
    public List<KeyValuePair<string, string>> Environment { get; } = [];

    public List<string> Volumes { get; } = [];

    /// <summary>Network aliases the service attaches to.</summary>
    public List<string> NetworkAliases { get; } = [];

    public ComposeHealthcheck? Healthcheck { get; init; }

    /// <summary>Ordered labels.</summary>
    public List<KeyValuePair<string, string>> Labels { get; } = [];
}

/// <summary>Compose healthcheck definition.</summary>
public sealed class ComposeHealthcheck
{
    /// <summary>Test command (exec form, for example <c>CMD wget ...</c>).</summary>
    public required IReadOnlyList<string> Test { get; init; }

    public string Interval { get; init; } = "30s";

    public string Timeout { get; init; } = "5s";

    public int Retries { get; init; } = 3;

    public string StartPeriod { get; init; } = "20s";
}

/// <summary>Compose network definition.</summary>
public sealed class ComposeNetwork
{
    /// <summary>Network alias referenced by services.</summary>
    public required string Alias { get; init; }

    /// <summary>Concrete network name (may be an environment reference).</summary>
    public required string Name { get; init; }

    public bool External { get; init; } = true;
}

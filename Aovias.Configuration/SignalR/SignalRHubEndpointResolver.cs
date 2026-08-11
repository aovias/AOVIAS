namespace Aovias.Configuration.SignalR;

/// <summary>
/// Builds canonical SignalR hub paths and URLs from AOVIAS API and SignalR settings.
/// </summary>
public static class SignalRHubEndpointResolver
{
    public const string CanonicalHubPath = "/hubs/deployment";

    public static readonly IReadOnlyList<string> LegacyHubPaths = ["/hubs/runtime", "/api/runtime"];

    public static readonly IReadOnlyList<string> SupportedProtocols = ["WebSockets", "LongPolling"];

    public static IEnumerable<string> GetHubRegistrationPaths(string configuredHubPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredHubPath);

        yield return NormalizeHubPath(configuredHubPath);

        foreach (var legacyPath in LegacyHubPaths)
        {
            var normalizedLegacy = NormalizeHubPath(legacyPath);
            if (!string.Equals(normalizedLegacy, NormalizeHubPath(configuredHubPath), StringComparison.OrdinalIgnoreCase))
            {
                yield return normalizedLegacy;
            }
        }
    }

    public static string ResolveClientHubUrl(
        string clientBaseUrl,
        string? clientPathBase,
        string? discoveredHubPath,
        string configuredHubPath)
    {
        var hubPath = string.IsNullOrWhiteSpace(discoveredHubPath)
            ? configuredHubPath
            : discoveredHubPath;

        return ResolveHubUrl(clientBaseUrl, clientPathBase, hubPath);
    }

    /// <summary>
    /// Ordered hub paths to attempt when connecting, including configured and legacy aliases.
    /// </summary>
    public static IReadOnlyList<string> GetConnectionCandidateHubPaths(
        string? discoveredHubPath,
        string configuredHubPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredHubPath);

        var candidates = new List<string>();
        AddCandidate(candidates, discoveredHubPath);
        AddCandidate(candidates, configuredHubPath);

        foreach (var legacyPath in LegacyHubPaths)
        {
            AddCandidate(candidates, legacyPath);
        }

        return candidates;
    }

    public static IReadOnlyList<string> ResolveConnectionCandidateHubUrls(
        string clientBaseUrl,
        string? clientPathBase,
        string? discoveredHubPath,
        string configuredHubPath)
    {
        return GetConnectionCandidateHubPaths(discoveredHubPath, configuredHubPath)
            .Select(path => ResolveHubUrl(clientBaseUrl, clientPathBase, path))
            .ToList();
    }

    public static string ResolveHubPath(string? pathBase, string hubPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hubPath);

        var normalizedHub = NormalizeHubPath(hubPath);
        if (string.IsNullOrWhiteSpace(pathBase))
        {
            return normalizedHub;
        }

        var normalizedPathBase = NormalizePathBase(pathBase);
        if (normalizedHub.StartsWith(normalizedPathBase, StringComparison.OrdinalIgnoreCase))
        {
            return normalizedHub;
        }

        return $"{normalizedPathBase}{normalizedHub}";
    }

    public static string ResolveHubUrl(string baseUrl, string? pathBase, string hubPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        var normalizedHub = NormalizeHubPath(hubPath);
        var normalizedBase = baseUrl.TrimEnd('/');

        if (string.IsNullOrWhiteSpace(pathBase))
        {
            return $"{normalizedBase}{normalizedHub}";
        }

        var normalizedPathBase = NormalizePathBase(pathBase);
        if (normalizedBase.EndsWith(normalizedPathBase, StringComparison.OrdinalIgnoreCase))
        {
            return $"{normalizedBase}{normalizedHub}";
        }

        return $"{normalizedBase}{normalizedPathBase}{normalizedHub}";
    }

    public static string ResolveNegotiatePath(string? pathBase, string hubPath)
        => $"{ResolveHubPath(pathBase, hubPath).TrimEnd('/')}/negotiate?negotiateVersion=1";

    public static string ResolveNegotiateUrl(string baseUrl, string? pathBase, string hubPath)
        => ResolveHubUrl(baseUrl, pathBase, hubPath).TrimEnd('/') + "/negotiate?negotiateVersion=1";

    private static string NormalizePathBase(string pathBase)
        => pathBase.StartsWith('/') ? pathBase.TrimEnd('/') : $"/{pathBase.Trim('/')}";

    private static string NormalizeHubPath(string hubPath)
        => hubPath.StartsWith('/') ? hubPath : $"/{hubPath}";

    private static void AddCandidate(ICollection<string> candidates, string? hubPath)
    {
        if (string.IsNullOrWhiteSpace(hubPath))
        {
            return;
        }

        var normalized = NormalizeHubPath(hubPath);
        if (candidates.Any(existing => string.Equals(existing, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        candidates.Add(normalized);
    }
}

using Aovias.Configuration.Deployment;

namespace Aovias.Publishing.Core.Reporting;

/// <summary>
/// Result of comparing a single environment variable across profiles.
/// </summary>
public sealed record EnvironmentVariableComparison(
    string Key,
    bool ExpectedToDiffer,
    IReadOnlyDictionary<string, string> ValuesByProfile)
{
    /// <summary>True when the value is not identical across all compared profiles.</summary>
    public bool Differs => ValuesByProfile.Values.Distinct(StringComparer.Ordinal).Count() > 1;

    /// <summary>
    /// A violation is an unexpected difference: a key that must be identical across
    /// every environment but is not.
    /// </summary>
    public bool IsViolation => Differs && !ExpectedToDiffer;
}

/// <summary>
/// Environment comparison report (D10): publishes the resolved profiles for
/// Local/Demo/UAT/Production and asserts only the expected environment-specific
/// values differ; everything else must be identical.
/// </summary>
public sealed class EnvironmentComparisonReport
{
    public required IReadOnlyList<string> Profiles { get; init; }

    public required IReadOnlyList<EnvironmentVariableComparison> Variables { get; init; }

    public IReadOnlyList<EnvironmentVariableComparison> Violations
        => Variables.Where(variable => variable.IsViolation).ToList();

    public bool IsConsistent => Violations.Count == 0;
}

/// <summary>
/// Compares resolved <see cref="DeploymentProfile"/> environment maps across
/// environments to prove determinism and isolate expected differences.
/// </summary>
public static class EnvironmentComparer
{
    /// <summary>
    /// Keys that legitimately vary per environment. Every other key must be
    /// byte-for-byte identical across profiles.
    /// </summary>
    private static readonly HashSet<string> ExpectedToDifferKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "DOTNET_ENVIRONMENT",
        "AOVIAS_ACTIVE_PROFILE",
        "AOVIAS_HOST_CONFIG_ROOT",
        "AOVIAS_HOST_PACKAGES_ROOT",
        "AOVIAS_HOST_LOGS_ROOT",
        "AOVIAS_HOST_DEPLOYMENTS_ROOT",
        // Docker network naming is a deployment-profile concern (isolating
        // environments that may share a host, e.g. Demo and QA side by side).
        // Publishing only ever emits the ${AOVIAS_DOCKER_NETWORK} reference.
        "AOVIAS_DOCKER_NETWORK"
    };

    public static EnvironmentComparisonReport Compare(string configRoot, IReadOnlyList<string> profileNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configRoot);
        if (profileNames is not { Count: > 1 })
        {
            throw new ArgumentException("At least two profiles are required for comparison.", nameof(profileNames));
        }

        var resolved = profileNames.ToDictionary(
            name => name,
            name => DeploymentPathResolver.Resolve(configRoot, name),
            StringComparer.OrdinalIgnoreCase);

        var keys = resolved.Values
            .SelectMany(profile => profile.ToEnvironmentVariables().Select(pair => pair.Key))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        var comparisons = new List<EnvironmentVariableComparison>(keys.Count);
        foreach (var key in keys)
        {
            var valuesByProfile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var (name, profile) in resolved)
            {
                var match = profile.ToEnvironmentVariables()
                    .FirstOrDefault(pair => string.Equals(pair.Key, key, StringComparison.Ordinal));
                valuesByProfile[name] = match.Value ?? string.Empty;
            }

            comparisons.Add(new EnvironmentVariableComparison(
                key,
                ExpectedToDifferKeys.Contains(key),
                valuesByProfile));
        }

        return new EnvironmentComparisonReport
        {
            Profiles = profileNames.ToList(),
            Variables = comparisons
        };
    }
}

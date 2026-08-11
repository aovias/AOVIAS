using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts;
using Aovias.Publishing.Contracts;

namespace Aovias.DeploymentValidation.Rules;

/// <summary>
/// Validates generated .env.template deployment artifacts.
/// </summary>
public static class EnvTemplateValidationRules
{
    private static readonly string[] RequiredVariables =
    [
        "DOTNET_ENVIRONMENT=",
        "AOVIAS_CONFIG_ROOT=",
        "AOVIAS_ACTIVE_PROFILE=",
        "AOVIAS_PROFILES_DIRECTORY=",
        "AOVIAS_SECRETS_DIRECTORY=",
        "AOVIAS_PACKAGES_ROOT=",
        "AOVIAS_LOGS_ROOT=",
        "AOVIAS_DEPLOYMENTS_ROOT=",
        "AOVIAS_DOCKER_NETWORK=",
        "AOVIAS_HOST_CONFIG_ROOT=",
        "AOVIAS_HOST_PACKAGES_ROOT=",
        "AOVIAS_HOST_LOGS_ROOT=",
        "AOVIAS_HOST_DEPLOYMENTS_ROOT="
    ];

    private static readonly string[] EnvironmentReferenceVariables =
    [
        "DOTNET_ENVIRONMENT",
        "AOVIAS_ACTIVE_PROFILE",
        "AOVIAS_DOCKER_NETWORK",
        "AOVIAS_HOST_CONFIG_ROOT",
        "AOVIAS_HOST_PACKAGES_ROOT",
        "AOVIAS_HOST_LOGS_ROOT",
        "AOVIAS_HOST_DEPLOYMENTS_ROOT"
    ];

    /// <summary>
    /// Validates .env.template content. When <paramref name="expectedHostSettings"/> is
    /// supplied, performs resolved-environment validation; otherwise validates the
    /// environment-neutral publishing template.
    /// </summary>
    public static IReadOnlyList<string> Validate(
        string content,
        string artifactName,
        DeploymentHostSettings? expectedHostSettings = null)
        => expectedHostSettings is null
            ? ValidateNeutralTemplate(content, artifactName)
            : ValidateResolvedEnvironment(content, artifactName, expectedHostSettings);

    /// <summary>
    /// Validates that a published .env.template is environment-neutral.
    /// </summary>
    public static IReadOnlyList<string> ValidateNeutralTemplate(string content, string artifactName)
    {
        var violations = new List<string>();
        violations.AddRange(ValidateRequiredVariables(content, artifactName));

        if (violations.Count > 0 && string.IsNullOrWhiteSpace(content))
        {
            return violations;
        }

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith('#') || !line.Contains('='))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            var key = line[..separatorIndex];
            var value = line[(separatorIndex + 1)..];

            if (DeploymentHostPathsResolver.ContainsPlaceholder(value))
            {
                violations.Add($"{artifactName} contains unresolved placeholder value '{value}' on line '{line}'.");
            }

            if (EnvironmentReferenceVariables.Contains(key, StringComparer.OrdinalIgnoreCase)
                && !IsEnvironmentReference(value))
            {
                violations.Add(
                    $"{artifactName} variable '{key}' must use a deployment reference such as '${{{key}}}' but was '{value}'.");
            }
        }

        foreach (var (key, expectedValue) in NeutralTemplateVariables())
        {
            var actualValue = ReadVariable(content, key);
            if (string.IsNullOrWhiteSpace(actualValue))
            {
                continue;
            }

            if (IsEnvironmentReference(expectedValue))
            {
                if (!IsEnvironmentReference(actualValue))
                {
                    violations.Add(
                        $"{artifactName} variable '{key}' must remain an environment reference but was '{actualValue}'.");
                }

                continue;
            }

            if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
            {
                violations.Add(
                    $"{artifactName} variable '{key}' must be '{expectedValue}' but was '{actualValue}'.");
            }
        }

        return violations;
    }

    private static IReadOnlyList<string> ValidateResolvedEnvironment(
        string content,
        string artifactName,
        DeploymentHostSettings expectedHostSettings)
    {
        var violations = new List<string>(ValidateRequiredVariables(content, artifactName));

        foreach (var line in content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.Contains('='))
            {
                continue;
            }

            var value = line[(line.IndexOf('=') + 1)..];
            if (DeploymentHostPathsResolver.ContainsPlaceholder(value))
            {
                violations.Add($"{artifactName} contains unresolved placeholder value '{value}' on line '{line}'.");
            }
        }

        violations.AddRange(ValidateExpectedPaths(content, artifactName, expectedHostSettings));
        return violations;
    }

    private static IEnumerable<string> ValidateRequiredVariables(string content, string artifactName)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            yield return $"{artifactName} is empty.";
            yield break;
        }

        foreach (var requiredVariable in RequiredVariables)
        {
            if (!content.Contains(requiredVariable, StringComparison.Ordinal))
            {
                yield return $"{artifactName} is missing required variable '{requiredVariable.TrimEnd('=')}'.";
            }
        }
    }

    private static IEnumerable<string> ValidateExpectedPaths(
        string content,
        string artifactName,
        DeploymentHostSettings expectedHostSettings)
    {
        var expectations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AOVIAS_HOST_CONFIG_ROOT"] = expectedHostSettings.HostConfigRoot,
            ["AOVIAS_HOST_PACKAGES_ROOT"] = expectedHostSettings.HostPackagesRoot,
            ["AOVIAS_HOST_LOGS_ROOT"] = expectedHostSettings.HostLogsRoot,
            ["AOVIAS_DOCKER_NETWORK"] = expectedHostSettings.DockerNetwork
        };

        foreach (var (variable, expectedValue) in expectations)
        {
            var actualValue = ReadVariable(content, variable);
            if (string.IsNullOrWhiteSpace(actualValue))
            {
                yield return $"{artifactName} does not define '{variable}'.";
                continue;
            }

            if (!string.Equals(actualValue, expectedValue, StringComparison.Ordinal))
            {
                yield return $"{artifactName} variable '{variable}' must be '{expectedValue}' but was '{actualValue}'.";
            }
        }
    }

    private static string? ReadVariable(string content, string variableName)
    {
        foreach (var line in content.Split('\n'))
        {
            if (!line.StartsWith($"{variableName}=", StringComparison.Ordinal))
            {
                continue;
            }

            return line[(variableName.Length + 1)..].Trim();
        }

        return null;
    }

    private static bool IsEnvironmentReference(string value)
        => value.StartsWith("${", StringComparison.Ordinal) && value.Contains('}', StringComparison.Ordinal);

    private static IReadOnlyList<KeyValuePair<string, string>> NeutralTemplateVariables()
        =>
        [
            new("DOTNET_ENVIRONMENT", "${DOTNET_ENVIRONMENT:-Production}"),
            new("AOVIAS_CONFIG_ROOT", PlatformRuntimeDefaults.ConfigRoot),
            new("AOVIAS_ACTIVE_PROFILE", "${AOVIAS_ACTIVE_PROFILE}"),
            new("AOVIAS_PROFILES_DIRECTORY", "${AOVIAS_PROFILES_DIRECTORY:-profiles}"),
            new("AOVIAS_SECRETS_DIRECTORY", "${AOVIAS_SECRETS_DIRECTORY:-secrets}"),
            new("AOVIAS_PACKAGES_ROOT", PlatformRuntimeDefaults.PackagesRoot),
            new("AOVIAS_LOGS_ROOT", PlatformRuntimeDefaults.LogsRoot),
            new("AOVIAS_DEPLOYMENTS_ROOT", PlatformDeploymentDefaults.DeploymentRoot),
            new("AOVIAS_DOCKER_NETWORK", "${AOVIAS_DOCKER_NETWORK}"),
            new("AOVIAS_HOST_CONFIG_ROOT", "${AOVIAS_HOST_CONFIG_ROOT}"),
            new("AOVIAS_HOST_PACKAGES_ROOT", "${AOVIAS_HOST_PACKAGES_ROOT}"),
            new("AOVIAS_HOST_LOGS_ROOT", "${AOVIAS_HOST_LOGS_ROOT}"),
            new("AOVIAS_HOST_DEPLOYMENTS_ROOT", PlatformDeploymentDefaults.HostDeploymentsRootToken)
        ];
}

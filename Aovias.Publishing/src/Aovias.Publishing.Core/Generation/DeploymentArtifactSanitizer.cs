using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Aovias.DeploymentValidation.Rules;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Generation;

/// <summary>
/// Sanitizes deployment artifact content to satisfy environment-neutral publishing rules.
/// </summary>
internal static class DeploymentArtifactSanitizer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static readonly Regex LocalhostPattern = new(
        @"\blocalhost\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex LoopbackPattern = new(
        @"\b127\.0\.0\.1\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static bool IsRuntimeAgentService(PublishingContext context)
    {
        var discovery = context.Discovery;
        if (discovery is null)
        {
            return false;
        }

        var serviceId = context.Request.ServiceId ?? discovery.ServiceId;
        return RuntimeAgentConventions.IsRuntimeAgent(serviceId, discovery.ProjectRoot, discovery.ProjectPath);
    }

    internal static string PrepareConfigurationTemplateJson(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject
                   ?? throw new InvalidOperationException($"{DeployPackageLayout.ConfigTemplateFile} must be a JSON object.");

        SanitizeTemplateNode(node, string.Empty);

        var sanitized = node.ToJsonString(JsonOptions);
        var violations = FindConfigurationTemplateViolations(sanitized);
        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                $"{DeployPackageLayout.ConfigTemplateFile} is not deployment-neutral: {string.Join("; ", violations)}");
        }

        return sanitized;
    }

    internal static void SanitizeTemplateNode(JsonNode node, string keyPrefix)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    var childKey = string.IsNullOrEmpty(keyPrefix)
                        ? property.Key
                        : $"{keyPrefix}:{property.Key}";

                    if (property.Value is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        obj[property.Key] = NeutralizeConfigurationValue(childKey, text);
                    }
                    else if (property.Value is not null)
                    {
                        SanitizeTemplateNode(property.Value, childKey);
                    }
                }

                break;
            case JsonArray array:
                for (var index = 0; index < array.Count; index++)
                {
                    var childKey = $"{keyPrefix}:{index}";
                    if (array[index] is JsonValue value && value.TryGetValue<string>(out var text))
                    {
                        array[index] = NeutralizeConfigurationValue(childKey, text);
                    }
                    else if (array[index] is not null)
                    {
                        SanitizeTemplateNode(array[index]!, childKey);
                    }
                }

                break;
        }
    }

    internal static string NeutralizeConfigurationValue(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (ContainsForbiddenEnvironmentReference(value))
        {
            return $"${{{PlaceholderFormatter.ToPlaceholder(key)}}}";
        }

        return value;
    }

    internal static IReadOnlyList<string> FindConfigurationTemplateViolations(string content)
        => EnvironmentNeutralRules.FindViolations(
            content,
            DeployPackageLayout.ConfigTemplateFile,
            EnvironmentNeutralScanProfile.PackageArtifact);

    private static bool ContainsForbiddenEnvironmentReference(string value)
    {
        if (LocalhostPattern.IsMatch(value) || LoopbackPattern.IsMatch(value))
        {
            return true;
        }

        return EnvironmentNeutralRules.FindViolations(
                value,
                "value",
                EnvironmentNeutralScanProfile.PackageArtifact)
            .Count > 0;
    }
}

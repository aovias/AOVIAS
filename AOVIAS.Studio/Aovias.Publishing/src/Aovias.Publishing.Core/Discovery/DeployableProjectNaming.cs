using System.Text;
using System.Text.RegularExpressions;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Discovery;

/// <summary>
/// Resolves canonical service identifiers from discovery metadata when deployment
/// metadata is not yet loaded.
/// </summary>
public static class DeployableProjectNaming
{
    private static readonly Regex PascalCaseBoundary = new("(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled);

    public static string ToCanonicalServiceId(ProjectDiscoveryResult discovery)
    {
        if (discovery.ProjectType == PublishingProjectType.Angular)
        {
            return "portal";
        }

        if (RuntimeAgentConventions.IsRuntimeAgent(discovery.ServiceId, discovery.ProjectPath, discovery.ProjectRoot))
        {
            return RuntimeAgentConventions.ServiceId;
        }

        if (discovery.ServiceId.Contains("AIOrchestrator", StringComparison.OrdinalIgnoreCase))
        {
            return "ai";
        }

        if (discovery.ServiceId.Contains("OutboxRelay", StringComparison.OrdinalIgnoreCase))
        {
            return "outbox-worker";
        }

        return ToKebabCase(discovery.ServiceId);
    }

    public static string ToDisplayName(ProjectDiscoveryResult discovery, string serviceId)
    {
        if (discovery.ProjectType == PublishingProjectType.Angular)
        {
            return "Angular Portal";
        }

        return discovery.ServiceName;
    }

    public static string ToKebabCase(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var normalized = PascalCaseBoundary.Replace(value.Trim(), "-").ToLowerInvariant();
        return normalized.Replace('_', '-');
    }
}

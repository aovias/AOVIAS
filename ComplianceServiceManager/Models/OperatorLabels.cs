using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Models.ServiceInstances;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Models;

/// <summary>
/// Canonical operator-facing labels for Shared / Cursor / Claude.
/// Presentation only — does not change runtime, Git, or ownership rules.
/// </summary>
public static class OperatorLabels
{
    public static string Workspace(string? workspaceId)
    {
        if (string.Equals(workspaceId, "cursor", StringComparison.OrdinalIgnoreCase))
        {
            return "Cursor";
        }

        if (string.Equals(workspaceId, "claude", StringComparison.OrdinalIgnoreCase))
        {
            return "Claude";
        }

        if (string.Equals(workspaceId, "main", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(workspaceId))
        {
            return "Shared";
        }

        return workspaceId.Trim();
    }

    public static string Workspace(string? workspaceId, WorkspaceAgentOwner owner)
        => owner switch
        {
            WorkspaceAgentOwner.Cursor => "Cursor",
            WorkspaceAgentOwner.Claude => "Claude",
            WorkspaceAgentOwner.Integration => "Shared",
            _ => Workspace(workspaceId)
        };

    public static string Agent(WorkspaceAgentOwner owner)
        => owner switch
        {
            WorkspaceAgentOwner.Cursor => "Cursor",
            WorkspaceAgentOwner.Claude => "Claude",
            WorkspaceAgentOwner.Integration => "Shared",
            _ => "Unknown"
        };

    public static string Agent(string? workspaceId)
        => Workspace(workspaceId);

    public static string Owner(ServiceInstanceOwnership ownership, string? workspaceId, bool owned = true)
    {
        if (ownership == ServiceInstanceOwnership.Shared)
        {
            return "Shared Runtime";
        }

        return owned ? $"{Workspace(workspaceId)} Workspace" : "External";
    }

    public static string ShortSha(string? sha)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return "—";
        }

        return sha.Length <= 12 ? sha : sha[..12];
    }

    public static string Pressure(ResourcePressureLevel level)
        => level switch
        {
            ResourcePressureLevel.Normal => "Normal",
            ResourcePressureLevel.Warning => "Warning",
            ResourcePressureLevel.High => "High",
            ResourcePressureLevel.Critical => "Critical",
            _ => "Unknown"
        };

    public static string ResourceLevel(RuntimeProfileResourceClass value)
        => value switch
        {
            RuntimeProfileResourceClass.Light => "Low",
            RuntimeProfileResourceClass.Medium => "Moderate",
            RuntimeProfileResourceClass.Heavy => "High",
            _ => "Unknown"
        };
}

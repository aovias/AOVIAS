using System.Text.Json.Serialization;
using ComplianceServiceManager.Models.Workspaces;

namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>Persisted Studio metadata for known local development repositories/workspaces.</summary>
public sealed class DevelopmentWorkspaceRegistryDocument
{
    public List<ConfiguredRepository> Repositories { get; set; } = [];
}

public sealed class ConfiguredRepository
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string RootPath { get; set; } = string.Empty;

    public string DefaultBranch { get; set; } = "main";

    public List<ConfiguredWorkspace> Workspaces { get; set; } = [];
}

public sealed class ConfiguredWorkspace
{
    public string Id { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkspaceAgentOwner AgentOwner { get; set; } = WorkspaceAgentOwner.Unknown;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Other;

    public bool IsMain { get; set; }

    public bool Enabled { get; set; } = true;
}

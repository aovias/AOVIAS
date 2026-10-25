namespace ComplianceServiceManager.Models.ServiceInstances;

/// <summary>
/// Persisted stable identity for a service execution context.
/// Volatile runtime fields (PID, live status, CPU) are never stored.
/// </summary>
public sealed class ServiceInstanceDefinition
{
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Canonical service key — typically the discovered service name.</summary>
    public string ServiceId { get; set; } = string.Empty;

    public string ServiceName { get; set; } = string.Empty;

    public ServiceExecutionScope ExecutionScope { get; set; } = ServiceExecutionScope.Shared;

    public ServiceInstanceOwnership Ownership { get; set; } = ServiceInstanceOwnership.Shared;

    /// <summary>D1 workspace id when <see cref="ExecutionScope"/> is Workspace; null for Shared.</summary>
    public string? WorkspaceId { get; set; }

    /// <summary>Optional D4 agent session id.</summary>
    public string? AgentSessionId { get; set; }

    /// <summary>Optional D2 runtime profile id.</summary>
    public string? RuntimeProfileId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ServiceInstanceRegistryDocument
{
    public List<ServiceInstanceDefinition> Instances { get; set; } = [];
}

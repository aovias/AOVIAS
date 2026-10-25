namespace ComplianceServiceManager.Models.PortRegistry;

/// <summary>Stable registry assignment of a local TCP port to a ServiceInstance.</summary>
public sealed class PortAllocationDefinition
{
    public int Port { get; set; }

    public string InstanceId { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;

    public string? WorkspaceId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Notes { get; set; }
}

public sealed class PortRegistryDocument
{
    public List<PortAllocationDefinition> Allocations { get; set; } = [];
}

/// <summary>Point-in-time OS occupancy observation (never persisted as authoritative).</summary>
public sealed class PortOccupancyObservation
{
    public int Port { get; init; }

    public bool IsListening { get; init; }

    public int? ProcessId { get; init; }

    public string? ProcessName { get; init; }

    public string? Error { get; init; }

    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;

    public string AvailabilityDisplay =>
        !string.IsNullOrWhiteSpace(Error)
            ? "Check failed"
            : IsListening
                ? "Occupied (at last check)"
                : "Available (at last check)";
}

/// <summary>Effective display status combining registry assignment and OS occupancy.</summary>
public enum PortEffectiveStatus
{
    Unassigned = 0,
    AssignedAvailable = 1,
    AssignedOccupied = 2,
    AssignedConflict = 3,
    AssignedUnavailable = 4,
    OccupiedUnassigned = 5
}

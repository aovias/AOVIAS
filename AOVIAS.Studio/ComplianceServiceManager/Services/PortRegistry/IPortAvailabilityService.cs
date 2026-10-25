using ComplianceServiceManager.Models.PortRegistry;

namespace ComplianceServiceManager.Services.PortRegistry;

/// <summary>
/// Point-in-time TCP port availability on the local machine.
/// Does not start/stop processes. Observations are not permanent claims.
/// </summary>
public interface IPortAvailabilityService
{
    PortOccupancyObservation Observe(int port);

    /// <summary>Read-only helper for future allocators — does not reserve or assign.</summary>
    int? FindFirstAvailablePort(int startInclusive, int endInclusive, IEnumerable<int>? excludedPorts = null);
}

/// <summary>Result of a registry mutation request.</summary>
public sealed class PortRegistryResult
{
    public bool Succeeded { get; init; }

    public string? Error { get; init; }

    public PortAllocationItem? Allocation { get; init; }

    public static PortRegistryResult Ok(PortAllocationItem? allocation = null)
        => new() { Succeeded = true, Allocation = allocation };

    public static PortRegistryResult Fail(string error)
        => new() { Succeeded = false, Error = error };
}

namespace Aovias.Runtime.Contracts.Observability;

/// <summary>
/// Observed Docker container state.
/// </summary>
public sealed class ContainerObservationDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Image { get; init; }

    public required string Status { get; init; }

    public required string State { get; init; }

    public string? Health { get; init; }

    public double CpuUsagePercent { get; init; }

    public double MemoryUsageMegabytes { get; init; }

    public int RestartCount { get; init; }

    public string? Network { get; init; }

    public string? Volume { get; init; }
}

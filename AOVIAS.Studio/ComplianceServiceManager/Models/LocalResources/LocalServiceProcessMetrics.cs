namespace ComplianceServiceManager.Models.LocalResources;

/// <summary>Associates a discovered Local Dev service with an optional process sample.</summary>
public sealed class LocalServiceProcessMetrics
{
    public string ServiceName { get; init; } = string.Empty;

    public int? ProcessId { get; init; }

    public LocalProcessResourceMetrics? Process { get; init; }

    public string AssociationNote { get; init; } = string.Empty;
}

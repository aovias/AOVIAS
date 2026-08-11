namespace ComplianceServiceManager.Operations;

/// <summary>The static, pre-run definition of one stage in an <see cref="OperationDescriptor"/>'s
/// ordered stage list — an executor's real unit of work, not a cosmetic UI grouping. For Bootstrap
/// this is 1:1 with <c>Aovias.Bootstrap.Contracts.BootstrapPhaseName</c>; other operations define
/// their own stage sets.</summary>
public sealed class OperationStageDefinition
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string Description { get; init; } = string.Empty;

    /// <summary>Relative cost used for overall-progress weighting (see <see cref="OperationInstance.OverallProgress"/>). Stages with equal weight contribute equally; a heavier stage (e.g. uploading a large package) should be weighted higher than a light one (e.g. a connectivity check).</summary>
    public double Weight { get; init; } = 1.0;
}

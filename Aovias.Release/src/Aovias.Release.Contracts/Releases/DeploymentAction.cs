using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.Identity;
using Aovias.Release.Contracts.Packages;

namespace Aovias.Release.Contracts.Releases;

/// <summary>
/// Required action item within a deployment plan.
/// </summary>
public sealed class DeploymentAction
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public int Order { get; init; }

    public bool RequiresConfirmation { get; init; }
}

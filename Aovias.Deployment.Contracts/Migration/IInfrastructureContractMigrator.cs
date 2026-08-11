using Aovias.Deployment.Contracts.Infrastructure;

namespace Aovias.Deployment.Contracts.Migration;

/// <summary>
/// Upgrades a legacy schema v1 infrastructure contract to the current unified
/// <see cref="InfrastructureContract"/> shape (Sprint M2.2 D3). A migrator never touches the
/// original package on disk — it produces a new in-memory instance only, which is why
/// <see cref="Migrate"/> takes and returns plain values rather than a file path.
///
/// Future schema revisions add a new migrator implementing this same interface (e.g. a v2-to-v3
/// migrator taking the current <see cref="InfrastructureContract"/> and returning a v3 type) rather
/// than requiring changes to this interface or to <see cref="Aovias.Deployment.Contracts.Reading.DeploymentPackageContractReader"/>.
/// </summary>
public interface IInfrastructureContractMigrator
{
    /// <summary>The schema version this migrator upgrades from.</summary>
    int FromSchemaVersion { get; }

    /// <summary>Produces a current-schema contract from a legacy schema v1 contract.</summary>
    InfrastructureContract Migrate(InfrastructureContractV1 source);
}

namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Classifies deployment failures for operator visibility and automated rollback policy.
/// </summary>
public enum DeploymentFailureCategory
{
    Unknown = 0,
    Package,
    Upload,
    Authentication,
    Contracts,
    Configuration,
    Infrastructure,
    Docker,
    Network,
    Health,
    Smoke,
    Certification,
    Dependencies,
    Database,
    Secrets,
    Certificates,
    Runtime,
    Application,
    Rollback
}

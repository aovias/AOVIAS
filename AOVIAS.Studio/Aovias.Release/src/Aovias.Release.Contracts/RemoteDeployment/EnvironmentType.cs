namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Supported deployment target environment classifications.
/// </summary>
public enum EnvironmentType
{
    LocalDocker,
    Demo,
    Qa,
    Uat,
    Production,
    Customer
}

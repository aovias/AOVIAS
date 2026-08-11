namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// Authentication modes supported for Runtime Agent registration.
/// </summary>
public enum EnvironmentAuthenticationMode
{
    ApiKey,
    Jwt,
    MutualTls,
    Certificate
}

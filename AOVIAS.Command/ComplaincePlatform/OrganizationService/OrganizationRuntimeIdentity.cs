namespace OrganizationService;

/// <summary>
/// Operator-visible identity for this OrganizationService build.
/// GET /platform/runtime-identity and the X-Aovias-Runtime-Identity response header
/// distinguish a Cursor workspace instance from Shared Runtime without changing
/// ports, authentication, schema, or infrastructure.
/// </summary>
public static class OrganizationRuntimeIdentity
{
    public const string HeaderName = "X-Aovias-Runtime-Identity";
    public const string Value = "v28-mode-b-backend-isolation";
    public const string ServiceName = "OrganizationService";

    public static OrganizationRuntimeIdentityPayload CreatePayload(
        string environmentName,
        string informationalVersion)
        => new(ServiceName, Value, environmentName, informationalVersion);
}

public sealed record OrganizationRuntimeIdentityPayload(
    string Service,
    string RuntimeIdentity,
    string Environment,
    string InformationalVersion);

namespace Aovias.Deployment.Contracts.Smoke;

/// <summary>
/// Default smoke test declarations per platform service.
/// </summary>
public static class ServiceSmokeTestCatalog
{
    public static ServiceSmokeTestSuite GetSuite(string serviceId)
    {
        return serviceId.ToLowerInvariant() switch
        {
            "runtime-agent" => Suite("runtime-agent",
                Http("health", "/api/runtime/health"),
                Http("version", "/api/runtime/version"),
                Http("capabilities", "/api/runtime/capabilities")),
            "gateway" => Suite("gateway",
                Http("health", "/health"),
                Http("swagger", "/swagger/index.html", optional: true),
                Http("api-version", "/api/version", optional: true)),
            "identity-service" => Suite("identity-service",
                Http("health", "/health"),
                Http("discovery", "/.well-known/openid-configuration"),
                Http("jwks", "/.well-known/openid-configuration/jwks", optional: true)),
            "organization-service" => Suite("organization-service",
                Http("health", "/health")),
            "compliance-service" => Suite("compliance-service",
                Http("health", "/health"),
                Http("ready", "/health/ready", optional: true)),
            "audit-service" => Suite("audit-service",
                Http("health", "/health")),
            "ai" => Suite("ai",
                Http("health", "/health")),
            "portal" => Suite("portal",
                Http("index", "/"),
                Static("assets", "/favicon.ico", optional: true)),
            _ => Suite(serviceId, Http("health", "/health"))
        };
    }

    public static IReadOnlyList<ServiceSmokeTestSuite> GetPlatformSuites(IEnumerable<string> serviceIds)
        => serviceIds.Select(GetSuite).ToList();

    private static ServiceSmokeTestSuite Suite(string serviceId, params ServiceSmokeTestDefinition[] tests)
        => new() { ServiceId = serviceId, Tests = tests };

    private static ServiceSmokeTestDefinition Http(string id, string path, bool optional = false)
        => new()
        {
            Id = id,
            Name = id,
            Kind = SmokeTestKind.HttpGet,
            Path = path,
            ExpectedStatus = 200,
            Required = !optional
        };

    private static ServiceSmokeTestDefinition Static(string id, string path, bool optional = false)
        => new()
        {
            Id = id,
            Name = id,
            Kind = SmokeTestKind.StaticAsset,
            Path = path,
            ExpectedStatus = 200,
            Required = !optional
        };
}

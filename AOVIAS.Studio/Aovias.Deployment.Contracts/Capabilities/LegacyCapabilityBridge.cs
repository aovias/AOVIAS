namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// Bridges the pre-M2.3 <see cref="RuntimeCapabilities"/> boolean flags to an equivalent
/// <see cref="Capability"/> list (Sprint M2.3 D8), so that
/// <see cref="Infrastructure.Provisioners.DependencyInfrastructureProvisioner"/> can run
/// requirement-capability matching for every deployment even when neither DRV nor Runtime Agent
/// has been updated to populate <see cref="Infrastructure.InfrastructureProvisioningContext.AvailableCapabilities"/>
/// directly. This is a compatibility shim only: it is deliberately conservative and only maps a
/// flag when it is <see langword="true"/> — a runtime that reports a capability as unavailable
/// contributes no <see cref="Capability"/> entry, exactly like today's boolean checks would block.
/// </summary>
public static class LegacyCapabilityBridge
{
    /// <summary>
    /// Translates <paramref name="runtimeCapabilities"/> into the capability ids known to
    /// <see cref="CapabilityRegistry"/> that correspond to each enabled legacy flag. Capabilities
    /// resolved here always come from the registry (never synthesized inline) so their
    /// <see cref="Capability.Category"/>/<see cref="Capability.SatisfiesCategories"/>/<see cref="Capability.Version"/>
    /// stay authoritative and consistent with everything else in the platform.
    /// </summary>
    public static IReadOnlyList<Capability> FromRuntimeCapabilities(RuntimeCapabilities runtimeCapabilities)
    {
        ArgumentNullException.ThrowIfNull(runtimeCapabilities);

        var ids = new List<string>();
        if (runtimeCapabilities.SqlServer) { ids.Add("sqlserver"); }
        if (runtimeCapabilities.RabbitMq) { ids.Add("rabbitmq"); }
        if (runtimeCapabilities.ServiceBus) { ids.Add("azure-service-bus"); }
        if (runtimeCapabilities.Certificates) { ids.Add("tls-file-certificates"); }
        if (runtimeCapabilities.Nginx) { ids.Add("reverse-proxy-nginx"); }

        // Docker is the platform's baseline runtime engine, not an optional environment feature
        // (RuntimeCapabilities.Docker defaults to true and every deployable service assumes it).
        // Two things ride along with it that have no dedicated legacy boolean of their own:
        // Docker network connectivity, and HTTP health probing — every service's HealthContract
        // already assumes a probe exists, so "health-monitoring" is bridged here rather than left
        // permanently unsatisfiable for every caller that has not adopted AvailableCapabilities
        // directly. This mirrors the platform's existing assumption that health checks are a
        // built-in engine capability, not an environment-declared one.
        if (runtimeCapabilities.Docker)
        {
            ids.Add("docker-network");
            ids.Add("http-health-probe");
        }

        // SignalR has no equivalent Requirement category in this sprint's taxonomy (a transport
        // concern, not an infrastructure requirement a service declares) — deliberately not
        // bridged. Redis has no capability-registry entry yet either (no service today declares
        // a matching requirement); see M2.3 report Technical Debt for both gaps.
        return ids
            .Select(CapabilityRegistry.FindById)
            .Where(capability => capability is not null)
            .Select(capability => capability!)
            .ToList();
    }
}

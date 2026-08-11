using Aovias.Runtime.Agent.Application.Interfaces;
using Aovias.Runtime.Contracts.Health;

namespace Aovias.Runtime.Agent.Infrastructure.Health;

/// <summary>
/// Aggregates health from manifest, system, and Docker providers.
/// </summary>
public sealed class HealthProvider : IHealthProvider
{
    private readonly IRuntimeManifestProvider manifestProvider;
    private readonly IDockerProvider dockerProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthProvider"/> class.
    /// </summary>
    public HealthProvider(IRuntimeManifestProvider manifestProvider, IDockerProvider dockerProvider)
    {
        this.manifestProvider = manifestProvider;
        this.dockerProvider = dockerProvider;
    }

    /// <inheritdoc />
    public async Task<RuntimeHealthDto> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await manifestProvider.GetManifestAsync(cancellationToken);
        var docker = await dockerProvider.GetOverviewAsync(cancellationToken);

        var components = new List<RuntimeComponentHealthDto>
        {
            new()
            {
                Name = "Docker Engine",
                Status = docker.Status,
                Health = docker.Status.Equals("Running", StringComparison.OrdinalIgnoreCase) ? "Healthy" : "Degraded"
            }
        };

        components.AddRange(manifest.InfrastructureServices.Select(service => new RuntimeComponentHealthDto
        {
            Name = service.Name,
            Status = service.Status,
            Health = service.Health
        }));

        var overallHealthy = components.All(component =>
            component.Health.Equals("Healthy", StringComparison.OrdinalIgnoreCase) ||
            component.Health.Equals("Valid", StringComparison.OrdinalIgnoreCase) ||
            component.Health.Equals("Running", StringComparison.OrdinalIgnoreCase));

        return new RuntimeHealthDto
        {
            Status = overallHealthy ? "Healthy" : "Degraded",
            Message = overallHealthy
                ? "Runtime control plane is operating normally."
                : "One or more runtime components require attention.",
            CheckedAt = DateTimeOffset.UtcNow,
            Components = components
        };
    }
}

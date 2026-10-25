using System.Text.Json;
using Aovias.Deployment.Contracts.Smoke;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.DeploymentValidation.Services;

/// <summary>
/// Validates packages, startup order, and smoke declarations for a published solution.
/// </summary>
public sealed class PlatformDeploymentSimulationService : IPlatformDeploymentSimulationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<PlatformDeploymentSimulationResult> SimulateAsync(
        string releaseLibraryRoot,
        CancellationToken cancellationToken = default)
    {
        var simulationId = Guid.NewGuid().ToString("N");
        var started = DateTimeOffset.UtcNow;
        var steps = new List<string>();

        try
        {
            steps.Add("Create temporary validation context");
            var manifestPath = Path.Combine(releaseLibraryRoot, "solution-deployment.json");
            if (!File.Exists(manifestPath))
            {
                return Failed(simulationId, started, steps, "Solution deployment manifest was not found.");
            }

            await using var stream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<PlatformDeploymentManifest>(stream, JsonOptions, cancellationToken)
                ?? throw new InvalidOperationException("Solution deployment manifest is invalid.");

            steps.Add($"Validate startup order for {manifest.Services.Count} services");
            foreach (var service in manifest.Services.OrderBy(item => item.StartupOrder))
            {
                var packagePath = ResolvePackage(releaseLibraryRoot, service);
                steps.Add($"Verify package for {service.ServiceId}");
                if (!File.Exists(packagePath))
                {
                    return Failed(simulationId, started, steps, $"Missing package for {service.ServiceId}: {packagePath}");
                }

                steps.Add($"Validate smoke test declarations for {service.ServiceId}");
                var suite = ServiceSmokeTestCatalog.GetSuite(service.ServiceId);
                if (suite.Tests.Count == 0)
                {
                    return Failed(simulationId, started, steps, $"No smoke tests declared for {service.ServiceId}.");
                }

                foreach (var dependency in service.DependsOn)
                {
                    if (!manifest.Services.Any(item =>
                            string.Equals(item.ServiceId, dependency, StringComparison.OrdinalIgnoreCase)))
                    {
                        return Failed(simulationId, started, steps, $"Missing dependency '{dependency}' for {service.ServiceId}.");
                    }
                }
            }

            steps.Add("Destroy temporary validation context");
            steps.Add("Simulation passed");

            var preview = PlatformCertificationBuilder.Build(
                simulationId,
                manifest,
                new InfrastructureReadinessSnapshot
                {
                    Infrastructure = "Pending",
                    Contracts = "Pending",
                    Containers = "Pending",
                    Dependencies = "Pending",
                    Health = "Pending",
                    Runtime = "Pending"
                },
                new SmokeTestSuiteResult
                {
                    Passed = false,
                    PassedCount = 0,
                    FailedCount = 0,
                    Results = []
                },
                ["Publish-time simulation validates manifest and package contracts only. Deploy-time gates remain pending."]);

            return new PlatformDeploymentSimulationResult
            {
                SimulationId = simulationId,
                Passed = true,
                Message = "Platform deployment simulation passed.",
                StartedUtc = started,
                CompletedUtc = DateTimeOffset.UtcNow,
                Steps = steps,
                CertificationPreview = preview
            };
        }
        catch (Exception exception)
        {
            return Failed(simulationId, started, steps, exception.Message);
        }
    }

    private static string ResolvePackage(string root, PlatformDeploymentServiceEntry service)
    {
        var direct = Path.Combine(root, service.PackageFileName);
        if (File.Exists(direct))
        {
            return direct;
        }

        return Directory.EnumerateFiles(root, service.PackageFileName, SearchOption.AllDirectories).FirstOrDefault()
            ?? direct;
    }

    private static PlatformDeploymentSimulationResult Failed(
        string simulationId,
        DateTimeOffset started,
        IReadOnlyList<string> steps,
        string message)
        => new()
        {
            SimulationId = simulationId,
            Passed = false,
            Message = message,
            StartedUtc = started,
            CompletedUtc = DateTimeOffset.UtcNow,
            Steps = steps.Append($"FAILED: {message}").ToList()
        };
}

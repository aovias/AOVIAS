namespace Aovias.Deployment.Contracts.Reconciliation;

/// <summary>
/// The single <see cref="IDriftDetectionEngine"/> implementation (Sprint M2.6 D3). Purely
/// compares two already-built, immutable snapshots (<see cref="DesiredState"/>,
/// <see cref="ObservedState"/>) and classifies every difference via <see cref="DriftRepairMatrix"/>
/// (D4) — performs no I/O and no repair of any kind (Final Engineering Rule: "Drift Detection
/// never performs repairs"). Calling this twice with the same two inputs always returns an
/// identical <see cref="DriftReport"/> (modulo <see cref="DriftReport.GeneratedUtc"/> and the
/// caller-supplied correlation id) — reconciliation is deterministic (Final Engineering Rule 8).
/// </summary>
public sealed class DriftDetectionEngine : IDriftDetectionEngine
{
    public DriftReport DetectDrift(DesiredState desired, ObservedState observed, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(desired);
        ArgumentNullException.ThrowIfNull(observed);

        var findings = new List<DriftFinding>();

        DetectServiceDrift(desired, observed, findings);
        DetectContainerDrift(desired, observed, findings);
        DetectNetworkDrift(desired, observed, findings);
        DetectVolumeDrift(desired, observed, findings);
        DetectFilesystemDrift(desired, observed, findings);
        DetectSymbolicLinkDrift(desired, observed, findings);
        DetectCertificateDrift(desired, observed, findings);
        DetectSecretDrift(desired, observed, findings);
        DetectHealthEndpointDrift(desired, observed, findings);
        DetectExtraResourceDrift(desired, observed, findings);

        return new DriftReport
        {
            EnvironmentId = desired.EnvironmentId,
            ServiceId = desired.ServiceId,
            CorrelationId = correlationId,
            GeneratedUtc = DateTimeOffset.UtcNow,
            Findings = findings
        };
    }

    private static void DetectServiceDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var desiredService in desired.Services)
        {
            var observedService = observed.Services.FirstOrDefault(s => string.Equals(s.ServiceId, desiredService.ServiceId, StringComparison.OrdinalIgnoreCase));
            if (observedService is null || observedService.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (desiredService.Required && !observedService.Running)
            {
                Add(findings, DriftCategory.UnhealthyService, ReconciliationResourceTypes.Service, desiredService.ServiceId, "Running", "NotRunning",
                    $"Required service '{desiredService.ServiceId}' is not running.");
            }
        }
    }

    private static void DetectContainerDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var container in desired.Containers)
        {
            var observedContainer = observed.Containers.FirstOrDefault(c => string.Equals(c.ContainerName, container.ContainerName, StringComparison.OrdinalIgnoreCase));

            if (observedContainer is null || observedContainer.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue; // Cannot discover -> cannot honestly claim drift (D11: fail safely, never guess).
            }

            if (!observedContainer.Exists)
            {
                Add(findings, DriftCategory.MissingResource, ReconciliationResourceTypes.Container, container.ContainerName, "Exists", "Missing",
                    $"Container '{container.ContainerName}' does not exist.");
                continue;
            }

            if (!string.Equals(observedContainer.State, "running", StringComparison.OrdinalIgnoreCase))
            {
                Add(findings, DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, container.ContainerName, "running", observedContainer.State ?? "unknown",
                    $"Container '{container.ContainerName}' is not running (state: {observedContainer.State}).");
            }
            else if (observedContainer.Health is not null && !string.Equals(observedContainer.Health, "healthy", StringComparison.OrdinalIgnoreCase))
            {
                Add(findings, DriftCategory.UnhealthyService, ReconciliationResourceTypes.Container, container.ContainerName, "healthy", observedContainer.Health,
                    $"Container '{container.ContainerName}' health status is '{observedContainer.Health}'.");
            }

            if (!string.IsNullOrWhiteSpace(observedContainer.ImageReference) &&
                !string.Equals(observedContainer.ImageReference, container.ImageReference, StringComparison.OrdinalIgnoreCase))
            {
                Add(findings, DriftCategory.VersionMismatch, ReconciliationResourceTypes.PackageVersion, container.ContainerName, container.ImageReference, observedContainer.ImageReference,
                    $"Container '{container.ContainerName}' is running image '{observedContainer.ImageReference}', expected '{container.ImageReference}'.");
            }
        }
    }

    private static void DetectNetworkDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var network in desired.Networks)
        {
            var observedNetwork = observed.Networks.FirstOrDefault(n => string.Equals(n.NetworkName, network.NetworkName, StringComparison.OrdinalIgnoreCase));
            if (observedNetwork is null || observedNetwork.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedNetwork.Exists)
            {
                Add(findings, DriftCategory.MissingResource, ReconciliationResourceTypes.DockerNetwork, network.NetworkName, "Exists", "Missing",
                    $"Docker network '{network.NetworkName}' does not exist.");
            }
        }
    }

    private static void DetectVolumeDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var volume in desired.Volumes)
        {
            var observedVolume = observed.Volumes.FirstOrDefault(v => string.Equals(v.HostToken, volume.HostToken, StringComparison.OrdinalIgnoreCase));
            if (observedVolume is null || observedVolume.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedVolume.Exists)
            {
                Add(findings, DriftCategory.MissingResource, ReconciliationResourceTypes.Volume, volume.HostToken, "Exists", "Missing",
                    $"Volume '{volume.HostToken}' does not exist.");
            }
        }
    }

    private static void DetectFilesystemDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var path in desired.FilesystemPaths)
        {
            var observedPath = observed.FilesystemPaths.FirstOrDefault(p => string.Equals(p.Path, path.Path, StringComparison.OrdinalIgnoreCase));
            if (observedPath is null || observedPath.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedPath.Exists)
            {
                Add(findings, DriftCategory.MissingResource, ReconciliationResourceTypes.FilesystemPath, path.Path, "Exists", "Missing",
                    $"Directory '{path.Path}' does not exist.");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(observedPath.Permission) && !string.Equals(observedPath.Permission, path.Permission, StringComparison.Ordinal))
            {
                var resourceType = path.IsSensitive ? ReconciliationResourceTypes.SensitivePath : ReconciliationResourceTypes.FilesystemPath;
                Add(findings, DriftCategory.PermissionDrift, resourceType, path.Path, path.Permission, observedPath.Permission,
                    $"Directory '{path.Path}' has permission '{observedPath.Permission}', expected '{path.Permission}'.");
            }
        }
    }

    private static void DetectSymbolicLinkDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var link in desired.SymbolicLinks)
        {
            var observedLink = observed.SymbolicLinks.FirstOrDefault(l => string.Equals(l.LinkPath, link.LinkPath, StringComparison.OrdinalIgnoreCase));
            if (observedLink is null || observedLink.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedLink.Exists || !string.Equals(observedLink.ActualTargetPath, link.TargetPath, StringComparison.OrdinalIgnoreCase))
            {
                Add(findings, DriftCategory.MissingResource, ReconciliationResourceTypes.SymbolicLink, link.LinkPath, link.TargetPath, observedLink.ActualTargetPath ?? "Missing",
                    $"Symbolic link '{link.LinkPath}' does not point to '{link.TargetPath}'.");
            }
        }
    }

    private static void DetectCertificateDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var certificate in desired.Certificates.Where(c => c.Required))
        {
            var observedCertificate = observed.Certificates.FirstOrDefault(c => string.Equals(c.Id, certificate.Id, StringComparison.OrdinalIgnoreCase));
            if (observedCertificate is null || observedCertificate.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedCertificate.Exists)
            {
                Add(findings, DriftCategory.MissingCertificate, ReconciliationResourceTypes.Certificate, certificate.Id, "Exists", "Missing",
                    $"Required certificate '{certificate.Id}' is missing.");
            }
        }
    }

    private static void DetectSecretDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var secret in desired.Secrets.Where(s => s.Required))
        {
            var observedSecret = observed.Secrets.FirstOrDefault(s => string.Equals(s.Id, secret.Id, StringComparison.OrdinalIgnoreCase));
            if (observedSecret is null || observedSecret.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedSecret.Exists)
            {
                Add(findings, DriftCategory.SecretDrift, ReconciliationResourceTypes.Secret, secret.Id, "Exists", "Missing",
                    $"Required secret '{secret.Id}' is missing.");
            }
        }
    }

    private static void DetectHealthEndpointDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        foreach (var endpoint in desired.HealthEndpoints)
        {
            var observedEndpoint = observed.HealthEndpoints.FirstOrDefault(e => string.Equals(e.Id, endpoint.Id, StringComparison.OrdinalIgnoreCase));
            if (observedEndpoint is null || observedEndpoint.DiscoveryStatus != DiscoveryStatus.Observed)
            {
                continue;
            }

            if (!observedEndpoint.IsHealthy)
            {
                Add(findings, DriftCategory.UnhealthyService, ReconciliationResourceTypes.HealthEndpoint, endpoint.Id, "Healthy", observedEndpoint.Message ?? "Unhealthy",
                    $"Health endpoint '{endpoint.Id}' ({endpoint.Url}) is unhealthy: {observedEndpoint.Message}");
            }
        }
    }

    /// <summary>
    /// Extra-resource detection is deliberately narrow (Sprint M2.6 Technical Debt): a container is
    /// flagged only when it shares this service's exact desired image reference under a different
    /// name — a proxy for "a stray duplicate of this same deployment is still running." No
    /// ownership-tagging convention exists in this codebase yet to detect extra resources more
    /// generally (e.g. an unrelated container this service never declared any interest in), so this
    /// engine never fabricates a broader claim than the data actually supports.
    /// </summary>
    private static void DetectExtraResourceDrift(DesiredState desired, ObservedState observed, List<DriftFinding> findings)
    {
        var desiredNames = desired.Containers.Select(c => c.ContainerName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var desiredImages = desired.Containers.Select(c => c.ImageReference).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var name in observed.AllDiscoveredContainerNames)
        {
            if (desiredNames.Contains(name))
            {
                continue;
            }

            var match = observed.Containers.FirstOrDefault(c => string.Equals(c.ContainerName, name, StringComparison.OrdinalIgnoreCase));
            if (match?.ImageReference is not null && desiredImages.Contains(match.ImageReference))
            {
                Add(findings, DriftCategory.ExtraResource, ReconciliationResourceTypes.Container, name, "NotPresent", "Present",
                    $"Container '{name}' runs an image also used by this service's desired container, but is not the desired container itself — likely a stray duplicate.");
            }
        }
    }

    private static void Add(
        List<DriftFinding> findings,
        DriftCategory category,
        string resourceType,
        string resourceId,
        string expected,
        string actual,
        string description)
    {
        var entry = DriftRepairMatrix.Resolve(category, resourceType);
        findings.Add(new DriftFinding
        {
            // Deterministic, not a fresh guid: the same drift, found in two separate cycles,
            // must yield the same FindingId so a caller (e.g. Drift History) can recognize
            // "this is the same unresolved finding as last cycle" versus "this is new" (D9).
            FindingId = $"{category}:{resourceType}:{resourceId}",
            Category = category,
            Severity = entry.Severity,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Expected = expected,
            Actual = actual,
            Description = description
        });
    }
}

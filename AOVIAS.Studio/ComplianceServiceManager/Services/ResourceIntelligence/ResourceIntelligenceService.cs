using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.AgentSessions;
using ComplianceServiceManager.Models.LocalResources;
using ComplianceServiceManager.Models.ResourceIntelligence;
using ComplianceServiceManager.Models.RuntimeProfiles;
using ComplianceServiceManager.Services.AgentSessions;
using ComplianceServiceManager.Services.LocalResources;
using ComplianceServiceManager.Services.RuntimeProfiles;

namespace ComplianceServiceManager.Services.ResourceIntelligence;

/// <summary>
/// Deterministic resource decision-support. Consumes S3 snapshots; never starts/stops services or mutates Git.
/// </summary>
public sealed class ResourceIntelligenceService : IResourceIntelligenceService, IDisposable
{
    private readonly ILocalResourceMonitoringService resources;
    private readonly IRuntimeProfileOrchestrator? profiles;
    private readonly IAgentSessionService? sessions;
    private readonly object gate = new();
    private readonly Queue<LocalResourceSnapshot> history = new();
    private bool disposed;

    public ResourceIntelligenceService(
        ILocalResourceMonitoringService resources,
        ResourceThresholdPolicy? policy = null,
        IRuntimeProfileOrchestrator? profiles = null,
        IAgentSessionService? sessions = null)
    {
        this.resources = resources;
        this.profiles = profiles;
        this.sessions = sessions;
        Policy = policy ?? ResourceThresholdPolicy.Default;
        resources.SnapshotUpdated += OnSnapshotUpdated;
        if (resources.LatestSnapshot is not null)
        {
            AssessFromSnapshot(resources.LatestSnapshot);
        }
    }

    public ResourceThresholdPolicy Policy { get; }

    public ResourceAssessment? LatestAssessment { get; private set; }

    public event Action<ResourceAssessment>? AssessmentUpdated;

    public ResourceAssessment Assess(string? requestedProfileId = null)
        => AssessFromSnapshot(resources.LatestSnapshot, requestedProfileId);

    public ResourceAssessment AssessFromSnapshot(LocalResourceSnapshot? snapshot, string? requestedProfileId = null)
    {
        if (snapshot is not null)
        {
            lock (gate)
            {
                history.Enqueue(snapshot);
                while (history.Count > Math.Max(1, Policy.HistoryCapacity))
                {
                    history.Dequeue();
                }
            }
        }

        var host = snapshot?.Host;
        var cpuWindow = AverageCpu(Policy.CpuWindowSampleCount);
        var cpuSample = host?.CpuPercent;
        var cpuForPressure = cpuWindow ?? cpuSample;
        var memory = host?.MemoryPercent;
        var disk = host?.DiskPercent;

        var cpuPressure = Classify(cpuForPressure, Policy.CpuNormalMaxPercent, Policy.CpuWarningMaxPercent, Policy.CpuHighMaxPercent);
        var memoryPressure = Classify(memory, Policy.MemoryNormalMaxPercent, Policy.MemoryWarningMaxPercent, Policy.MemoryHighMaxPercent);
        var diskPressure = Classify(disk, Policy.DiskNormalMaxPercent, Policy.DiskWarningMaxPercent, Policy.DiskHighMaxPercent);
        var overall = Max(cpuPressure, memoryPressure, diskPressure);

        var activeSessions = (sessions?.GetSessions() ?? [])
            .Where(session => session.Status is AgentSessionStatus.Active or AgentSessionStatus.Paused or AgentSessionStatus.Stale)
            .Select(session => new SessionResourceContribution
            {
                SessionId = session.Id,
                AgentDisplay = session.AgentDisplay,
                Title = session.Title,
                RuntimeProfileDisplay = session.RuntimeProfileDisplay,
                StatusDisplay = session.StatusDisplay
            })
            .ToList();

        var profileEstimates = (profiles?.GetProfiles() ?? [])
            .Select(profile => EstimateProfile(profile.ToDefinition()))
            .OrderBy(estimate => estimate.MemoryClass)
            .ThenBy(estimate => estimate.ExpectedProcessCount)
            .ThenBy(estimate => estimate.ProfileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var activeProfiles = (profiles?.GetProfiles() ?? [])
            .Count(profile => profile.Status is RuntimeProfileStatus.Running or RuntimeProfileStatus.PartiallyRunning or RuntimeProfileStatus.Starting);

        var activeServices = snapshot?.Services.Count(service => service.Process?.IsAccessible == true) ?? 0;

        var recommendations = BuildRecommendations(
            overall,
            memoryPressure,
            cpuPressure,
            diskPressure,
            host,
            cpuForPressure,
            activeSessions,
            profileEstimates,
            requestedProfileId);

        var assessment = new ResourceAssessment
        {
            Timestamp = snapshot?.CapturedAt ?? DateTimeOffset.UtcNow,
            CpuPercent = cpuSample,
            CpuWindowAveragePercent = cpuWindow,
            MemoryPercent = memory,
            MemoryAvailableBytes = host?.MemoryAvailableBytes ?? 0,
            MemoryUsedBytes = host?.MemoryUsedBytes ?? 0,
            MemoryTotalBytes = host?.MemoryTotalBytes ?? 0,
            DiskPercent = disk,
            CpuPressure = cpuPressure,
            MemoryPressure = memoryPressure,
            DiskPressure = diskPressure,
            OverallPressure = overall,
            ActiveServiceCount = activeServices,
            ActiveSessionCount = activeSessions.Count,
            ActiveProfileCount = activeProfiles,
            ActiveSessions = activeSessions,
            ProfileEstimates = profileEstimates,
            Recommendations = recommendations,
            Summary = BuildSummary(overall, memory, cpuForPressure, disk, activeSessions.Count, activeServices)
        };

        LatestAssessment = assessment;
        AssessmentUpdated?.Invoke(assessment);
        return assessment;
    }

    public RuntimeProfileResourceEstimate EstimateProfile(RuntimeProfileDefinition profile)
    {
        var count = profile.Services.Count(entry => entry.Enabled);
        var memoryClass = count switch
        {
            <= 2 => RuntimeProfileResourceClass.Light,
            <= 5 => RuntimeProfileResourceClass.Medium,
            _ => RuntimeProfileResourceClass.Heavy
        };
        var cpuClass = count switch
        {
            <= 2 => RuntimeProfileResourceClass.Light,
            <= 5 => RuntimeProfileResourceClass.Medium,
            _ => RuntimeProfileResourceClass.Heavy
        };

        return new RuntimeProfileResourceEstimate
        {
            ProfileId = profile.Id,
            ProfileName = profile.Name,
            ExpectedProcessCount = count,
            MemoryClass = memoryClass,
            CpuClass = cpuClass,
            Explanation =
                $"{count} enabled service(s). Estimated resource level: {OperatorLabels.ResourceLevel(memoryClass)} " +
                $"(CPU {OperatorLabels.ResourceLevel(cpuClass)}). " +
                "These are relative estimates for comparing profiles, not guaranteed memory or CPU amounts."
        };
    }

    public IReadOnlyList<RuntimeProfileResourceEstimate> RankProfilesLightestFirst()
        => (profiles?.GetProfiles() ?? [])
            .Select(profile => EstimateProfile(profile.ToDefinition()))
            .OrderBy(estimate => estimate.MemoryClass)
            .ThenBy(estimate => estimate.ExpectedProcessCount)
            .ThenBy(estimate => estimate.ProfileName, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        resources.SnapshotUpdated -= OnSnapshotUpdated;
    }

    private void OnSnapshotUpdated(LocalResourceSnapshot snapshot)
        => AssessFromSnapshot(snapshot);

    private List<ResourceRecommendation> BuildRecommendations(
        ResourcePressureLevel overall,
        ResourcePressureLevel memoryPressure,
        ResourcePressureLevel cpuPressure,
        ResourcePressureLevel diskPressure,
        LocalHostResourceMetrics? host,
        double? cpuPercent,
        IReadOnlyList<SessionResourceContribution> activeSessions,
        IReadOnlyList<RuntimeProfileResourceEstimate> profileEstimates,
        string? requestedProfileId)
    {
        var list = new List<ResourceRecommendation>();
        var now = DateTimeOffset.UtcNow;
        var lighter = profileEstimates.FirstOrDefault();

        if (memoryPressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical)
        {
            var available = LocalResourceFormatting.FormatBytes(host?.MemoryAvailableBytes ?? 0);
            list.Add(new ResourceRecommendation
            {
                Id = "low-memory",
                Kind = ResourceRecommendationKind.LowMemory,
                Severity = memoryPressure == ResourcePressureLevel.Critical
                    ? ResourceRecommendationSeverity.Critical
                    : ResourceRecommendationSeverity.High,
                Title = "High memory pressure",
                Explanation =
                    $"Memory usage is currently {host?.MemoryPercent:0.#}% with {available} available. " +
                    "Starting additional heavy runtime stacks may increase pressure. " +
                    "This is an advisory classification based on configured thresholds, not an OS guarantee.",
                SuggestedAction = lighter is null ? "View Resources" : $"Consider lighter profile '{lighter.ProfileName}'.",
                RelatedProfileId = lighter?.ProfileId,
                RelatedProfileName = lighter?.ProfileName,
                Timestamp = now
            });
        }

        if (cpuPressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical)
        {
            list.Add(new ResourceRecommendation
            {
                Id = "cpu-pressure",
                Kind = ResourceRecommendationKind.CpuPressure,
                Severity = ResourceRecommendationSeverity.Warning,
                Title = "CPU pressure",
                Explanation =
                    $"CPU utilization {(cpuPercent is null ? "is elevated" : $"has averaged {cpuPercent:0.#}% over recent samples")}. " +
                    "A single noisy sample is not treated as definitive; short-window averages are preferred when available.",
                SuggestedAction = "Avoid launching additional heavy profiles until CPU settles.",
                Timestamp = now
            });
        }

        if (diskPressure is ResourcePressureLevel.High or ResourcePressureLevel.Critical)
        {
            list.Add(new ResourceRecommendation
            {
                Id = "disk-pressure",
                Kind = ResourceRecommendationKind.DiskPressure,
                Severity = ResourceRecommendationSeverity.Warning,
                Title = "Disk pressure",
                Explanation =
                    $"Development volume usage is {host?.DiskPercent:0.#}% on '{host?.DiskRootPath}'. " +
                    "Low free space can slow builds and service starts.",
                SuggestedAction = "Free disk space on the development volume before large stacks.",
                Timestamp = now
            });
        }

        if (activeSessions.Count >= 2 && memoryPressure >= ResourcePressureLevel.Warning)
        {
            list.Add(new ResourceRecommendation
            {
                Id = "multi-agent",
                Kind = ResourceRecommendationKind.MultipleAgentPressure,
                Severity = ResourceRecommendationSeverity.Warning,
                Title = "Multiple agents active",
                Explanation =
                    $"{activeSessions.Count} development sessions are active/paused. " +
                    string.Join("; ", activeSessions.Select(session => $"{session.AgentDisplay}: {session.Title} ({session.RuntimeProfileDisplay})")) +
                    ". Adding another full stack may increase memory pressure.",
                SuggestedAction = lighter is null ? null : $"Prefer '{lighter.ProfileName}' if starting another runtime.",
                RelatedProfileId = lighter?.ProfileId,
                RelatedProfileName = lighter?.ProfileName,
                Timestamp = now
            });
        }

        if (!string.IsNullOrWhiteSpace(requestedProfileId))
        {
            var requested = profileEstimates.FirstOrDefault(estimate =>
                string.Equals(estimate.ProfileId, requestedProfileId, StringComparison.OrdinalIgnoreCase));
            if (requested is not null)
            {
                var lighterThanRequested = profileEstimates
                    .Where(estimate => estimate.MemoryClass < requested.MemoryClass
                                       || (estimate.MemoryClass == requested.MemoryClass
                                           && estimate.ExpectedProcessCount < requested.ExpectedProcessCount))
                    .OrderBy(estimate => estimate.MemoryClass)
                    .ThenBy(estimate => estimate.ExpectedProcessCount)
                    .FirstOrDefault();

                if (requested.MemoryClass >= RuntimeProfileResourceClass.Heavy
                    && memoryPressure >= ResourcePressureLevel.Warning
                    && lighterThanRequested is not null)
                {
                    list.Add(new ResourceRecommendation
                    {
                        Id = "profile-too-heavy",
                        Kind = ResourceRecommendationKind.ProfileTooHeavy,
                        Severity = ResourceRecommendationSeverity.High,
                        Title = "High resource demand",
                        Explanation =
                            $"Requested profile '{requested.ProfileName}' has estimated resource level {OperatorLabels.ResourceLevel(requested.MemoryClass)} " +
                            $"and starts {requested.ExpectedProcessCount} services. Memory is currently {host?.MemoryPercent:0.#}% " +
                            $"({LocalResourceFormatting.FormatBytes(host?.MemoryAvailableBytes ?? 0)} available). " +
                            $"'{lighterThanRequested.ProfileName}' starts fewer services ({lighterThanRequested.ExpectedProcessCount}) " +
                            "and is less likely to increase memory pressure.",
                        SuggestedAction = $"Use Suggested Profile: {lighterThanRequested.ProfileName}",
                        RelatedProfileId = lighterThanRequested.ProfileId,
                        RelatedProfileName = lighterThanRequested.ProfileName,
                        Timestamp = now
                    });
                }
            }
        }
        else if (overall >= ResourcePressureLevel.High && lighter is not null)
        {
            list.Add(new ResourceRecommendation
            {
                Id = "prefer-lighter",
                Kind = ResourceRecommendationKind.PreferLighterProfile,
                Severity = ResourceRecommendationSeverity.High,
                Title = "Consider a lighter profile",
                Explanation =
                    $"Overall pressure is {OperatorLabels.Pressure(overall)}. If you need to start a runtime, " +
                    $"'{lighter.ProfileName}' is currently the lightest option " +
                    $"({lighter.ExpectedProcessCount} services, estimated resource level {OperatorLabels.ResourceLevel(lighter.MemoryClass)}).",
                SuggestedAction = $"Use Suggested Profile: {lighter.ProfileName}",
                RelatedProfileId = lighter.ProfileId,
                RelatedProfileName = lighter.ProfileName,
                Timestamp = now
            });
        }

        if (list.Count == 0)
        {
            list.Add(new ResourceRecommendation
            {
                Id = "capacity-ok",
                Kind = ResourceRecommendationKind.CapacityOk,
                Severity = ResourceRecommendationSeverity.Info,
                Title = "Capacity looks acceptable",
                Explanation =
                    $"Overall pressure is {OperatorLabels.Pressure(overall)}. Memory {host?.MemoryPercent:0.#}%, " +
                    $"CPU {(cpuPercent is null ? "n/a" : $"{cpuPercent:0.#}%")}, " +
                    $"Disk {host?.DiskPercent:0.#}%. No advisory restriction is suggested.",
                SuggestedAction = null,
                Timestamp = now
            });
        }

        return list
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private double? AverageCpu(int window)
    {
        lock (gate)
        {
            var samples = history
                .Select(item => item.Host.CpuPercent)
                .Where(value => value is not null)
                .Select(value => value!.Value)
                .TakeLast(Math.Max(1, window))
                .ToList();
            if (samples.Count < Math.Max(1, window))
            {
                return samples.Count == 0 ? null : samples.Average();
            }

            return samples.Average();
        }
    }

    private static ResourcePressureLevel Classify(double? percent, double normalMax, double warningMax, double highMax)
    {
        if (percent is null || double.IsNaN(percent.Value))
        {
            return ResourcePressureLevel.Unknown;
        }

        var value = Math.Clamp(percent.Value, 0, 100);
        if (value < normalMax)
        {
            return ResourcePressureLevel.Normal;
        }

        if (value < warningMax)
        {
            return ResourcePressureLevel.Warning;
        }

        if (value < highMax)
        {
            return ResourcePressureLevel.High;
        }

        return ResourcePressureLevel.Critical;
    }

    private static ResourcePressureLevel Max(params ResourcePressureLevel[] levels)
    {
        var known = levels.Where(level => level != ResourcePressureLevel.Unknown).ToList();
        return known.Count == 0 ? ResourcePressureLevel.Unknown : known.Max();
    }

    private static string BuildSummary(
        ResourcePressureLevel overall,
        double? memory,
        double? cpu,
        double? disk,
        int sessions,
        int services)
        => $"Overall {overall}. Memory {(memory is null ? "n/a" : $"{memory:0.#}%")}, " +
           $"CPU {(cpu is null ? "n/a" : $"{cpu:0.#}%")}, Disk {(disk is null ? "n/a" : $"{disk:0.#}%")}. " +
           $"{sessions} session(s), {services} observed service process(es). Advisory only.";
}

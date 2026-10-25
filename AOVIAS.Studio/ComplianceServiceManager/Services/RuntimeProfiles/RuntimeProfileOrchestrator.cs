using System.Windows;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.RuntimeProfiles;

namespace ComplianceServiceManager.Services.RuntimeProfiles;

/// <summary>
/// Orchestrates dependency-aware profile start/stop over <see cref="ServiceManagerService"/>.
/// Does not replace process management; only sequences and scopes ownership.
/// </summary>
public sealed class RuntimeProfileOrchestrator : IRuntimeProfileOrchestrator
{
    private readonly RuntimeProfileRegistry registry;
    private readonly RuntimeProfileValidator validator;
    private readonly IServiceRuntimeHost serviceManager;
    private readonly IHealthProbe healthCheckService;
    private readonly ILoggingService logger;
    private readonly object gate = new();
    private List<RuntimeProfileItem> profiles = [];
    private TimeSpan healthWaitTimeout = TimeSpan.FromSeconds(90);
    private TimeSpan healthPollInterval = TimeSpan.FromSeconds(2);

    public RuntimeProfileOrchestrator(
        RuntimeProfileRegistry registry,
        RuntimeProfileValidator validator,
        IServiceRuntimeHost serviceManager,
        IHealthProbe healthCheckService,
        ILoggingService logger)
    {
        this.registry = registry;
        this.validator = validator;
        this.serviceManager = serviceManager;
        this.healthCheckService = healthCheckService;
        this.logger = logger;
        Reload();
    }

    /// <summary>Test seam: shorten health wait without changing production defaults.</summary>
    internal void ConfigureHealthWaitForTests(TimeSpan timeout, TimeSpan pollInterval)
    {
        healthWaitTimeout = timeout;
        healthPollInterval = pollInterval;
    }

    public event Action? ProfilesChanged;

    public IReadOnlyList<RuntimeProfileItem> GetProfiles()
    {
        lock (gate)
        {
            return profiles.ToList();
        }
    }

    public void Reload()
    {
        lock (gate)
        {
            profiles = registry.GetProfiles()
                .Select(RuntimeProfileItem.FromDefinition)
                .ToList();
        }

        ValidateAll();
        ProfilesChanged?.Invoke();
    }

    public ProfileValidationResult Validate(RuntimeProfileItem profile)
    {
        var result = validator.Validate(profile.ToDefinition(), serviceManager.Services);
        profile.ApplyValidation(result);
        return result;
    }

    public void ValidateAll()
    {
        foreach (var profile in GetProfiles())
        {
            Validate(profile);
        }
    }

    public void SaveProfile(RuntimeProfileItem profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id))
        {
            profile.Id = Guid.NewGuid().ToString("N");
        }

        profile.ServiceCount = profile.Services.Count(entry => entry.Enabled);
        var ownershipSnapshot = SnapshotOwnership();
        registry.Upsert(profile.ToDefinition());
        Reload();
        RestoreOwnership(ownershipSnapshot);
    }

    public async Task StartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default)
    {
        var validation = Validate(profile);
        if (!validation.IsValid)
        {
            OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Invalid, "Profile is invalid and cannot start."));
            logger.Warning("RuntimeProfiles", $"{profile.Name}: start blocked — invalid profile.");
            return;
        }

        HashSet<string> previouslyOwned = new(StringComparer.OrdinalIgnoreCase);
        OnUi(() =>
        {
            previouslyOwned = new HashSet<string>(profile.OwnedServiceNames, StringComparer.OrdinalIgnoreCase);
            // Do not wipe ownership yet — StartMemberAsync reclaims previously owned running services.
            // Clear only members for a fresh wave UI; OwnedServiceNames is rebuilt during start.
            profile.OwnedServiceNames.Clear();
            profile.Members.Clear();
            foreach (var serviceName in validation.StartupOrder)
            {
                profile.Members.Add(new RuntimeProfileMemberItem { ServiceName = serviceName });
            }

            profile.ApplyStatus(RuntimeProfileStatus.Starting, $"Starting {profile.Name}…");
        });
        logger.Info("RuntimeProfiles", $"Starting profile '{profile.Name}'.");

        var enabled = profile.Services.Where(entry => entry.Enabled).ToList();
        var dependsOn = enabled.ToDictionary(
            entry => entry.ServiceName,
            entry => entry.DependsOn
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            StringComparer.OrdinalIgnoreCase);

        var ready = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var remaining = new HashSet<string>(validation.StartupOrder, StringComparer.OrdinalIgnoreCase);
        var maxParallel = Math.Max(1, profile.MaxParallelStarts);

        try
        {
            while (remaining.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var wave = remaining
                    .Where(name => dependsOn[name].All(dep => ready.Contains(dep)))
                    .Where(name => !dependsOn[name].Any(dep => failed.Contains(dep)))
                    .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                    .Take(maxParallel)
                    .ToList();

                if (wave.Count == 0)
                {
                    foreach (var blocked in remaining.ToList())
                    {
                        var member = FindMember(profile, blocked);
                        var failedDeps = dependsOn[blocked].Where(failed.Contains).ToList();
                        OnUi(() => member.Apply(
                            RuntimeProfileMemberStatus.Blocked,
                            failedDeps.Count == 0
                                ? "Blocked by unresolved dependencies."
                                : $"Dependency {string.Join(", ", failedDeps)} failed."));
                        remaining.Remove(blocked);
                    }

                    break;
                }

                var tasks = wave.Select(name => StartMemberAsync(
                    profile,
                    name,
                    previouslyOwned,
                    ready,
                    failed,
                    remaining,
                    cancellationToken));
                await Task.WhenAll(tasks);
            }

            if (failed.Count == 0 && remaining.Count == 0 && ready.Count == validation.StartupOrder.Count)
            {
                OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Running, $"{profile.Name} is running."));
            }
            else if (ready.Count > 0)
            {
                OnUi(() => profile.ApplyStatus(
                    RuntimeProfileStatus.PartiallyRunning,
                    $"{ready.Count}/{validation.StartupOrder.Count} services ready; {failed.Count} failed."));
            }
            else
            {
                OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Failed, "Profile start failed."));
            }
        }
        catch (OperationCanceledException)
        {
            OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Failed, "Profile start cancelled."));
            throw;
        }
        catch (Exception ex)
        {
            OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Failed, ex.Message));
            logger.Error("RuntimeProfiles", $"Profile '{profile.Name}' start failed: {ex.Message}");
        }
    }

    public async Task StopAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default)
    {
        var owned = profile.OwnedServiceNames.ToList();
        if (owned.Count == 0)
        {
            OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Stopped, "No profile-owned services to stop."));
            return;
        }

        OnUi(() => profile.ApplyStatus(RuntimeProfileStatus.Stopping, $"Stopping {profile.Name}…"));

        var validation = validator.Validate(profile.ToDefinition(), serviceManager.Services);
        var stopOrder = validation.IsValid
            ? validation.StartupOrder.Reverse().Where(owned.Contains).ToList()
            : owned;

        foreach (var serviceName in stopOrder)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var member = FindMember(profile, serviceName);
            OnUi(() => member.Apply(RuntimeProfileMemberStatus.Stopping, "Stopping…"));

            var service = FindService(serviceName);
            if (service is null)
            {
                OnUi(() => member.Apply(RuntimeProfileMemberStatus.Stopped, "Service no longer discovered."));
                continue;
            }

            try
            {
                await serviceManager.StopAsync(service, cancellationToken);
                OnUi(() => member.Apply(RuntimeProfileMemberStatus.Stopped, "Stopped."));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                OnUi(() => member.Apply(RuntimeProfileMemberStatus.Failed, ex.Message));
                logger.Error("RuntimeProfiles", $"Stop failed for {serviceName}: {ex.Message}");
            }
        }

        OnUi(() =>
        {
            profile.OwnedServiceNames.Clear();
            profile.ApplyStatus(RuntimeProfileStatus.Stopped, $"{profile.Name} stopped (owned services only).");
        });
    }

    public async Task RestartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default)
    {
        await StopAsync(profile, cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        await StartAsync(profile, cancellationToken);
    }

    private async Task StartMemberAsync(
        RuntimeProfileItem profile,
        string serviceName,
        HashSet<string> previouslyOwned,
        HashSet<string> ready,
        HashSet<string> failed,
        HashSet<string> remaining,
        CancellationToken cancellationToken)
    {
        var member = FindMember(profile, serviceName);
        remaining.Remove(serviceName);

        var service = FindService(serviceName);
        if (service is null)
        {
            OnUi(() => member.Apply(RuntimeProfileMemberStatus.Failed, "Service not found in discovery."));
            failed.Add(serviceName);
            return;
        }

        if (service.Status == ServiceStatus.Running)
        {
            // Preserve ownership across re-Start so Stop still only affects profile-started processes.
            var reclaimOwned = previouslyOwned.Contains(serviceName);
            OnUi(() =>
            {
                if (reclaimOwned)
                {
                    member.Apply(
                        RuntimeProfileMemberStatus.Ready,
                        "Already running — ownership retained.",
                        owned: true,
                        reused: true);
                    profile.OwnedServiceNames.Add(serviceName);
                    profile.ApplyStatus(RuntimeProfileStatus.Starting, $"✓ {serviceName} ownership retained");
                }
                else
                {
                    member.Apply(RuntimeProfileMemberStatus.Reused, "Already running — reused.", owned: false, reused: true);
                    profile.ApplyStatus(RuntimeProfileStatus.Starting, $"✓ {serviceName} already running");
                }
            });
            ready.Add(serviceName);
            logger.Info(
                "RuntimeProfiles",
                reclaimOwned
                    ? $"{serviceName} already running; ownership retained."
                    : $"{serviceName} already running; reused.");
            return;
        }

        OnUi(() =>
        {
            member.Apply(RuntimeProfileMemberStatus.Starting, "Starting…", owned: true, reused: false);
            profile.ApplyStatus(RuntimeProfileStatus.Starting, $"▶ {serviceName}");
        });
        logger.Info("RuntimeProfiles", $"Starting {serviceName} for profile '{profile.Name}'.");

        try
        {
            await serviceManager.StartAsync(service, cancellationToken);
            if (service.Status is ServiceStatus.Error or ServiceStatus.FailedRepair || service.Process is { HasExited: true })
            {
                OnUi(() => member.Apply(RuntimeProfileMemberStatus.Failed, service.LastAction));
                failed.Add(serviceName);
                return;
            }

            profile.OwnedServiceNames.Add(serviceName);
            var healthy = await WaitForHealthyAsync(service, cancellationToken);
            if (!healthy)
            {
                OnUi(() => member.Apply(RuntimeProfileMemberStatus.Failed, "Started but health checks did not become ready in time."));
                failed.Add(serviceName);
                return;
            }

            OnUi(() =>
            {
                member.Apply(RuntimeProfileMemberStatus.Ready, "Ready.", owned: true);
                profile.ApplyStatus(RuntimeProfileStatus.Starting, $"✓ {serviceName} ready");
            });
            ready.Add(serviceName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnUi(() => member.Apply(RuntimeProfileMemberStatus.Failed, ex.Message, owned: true));
            failed.Add(serviceName);
            logger.Error("RuntimeProfiles", $"{serviceName} failed: {ex.Message}");
        }
    }

    private Dictionary<string, List<string>> SnapshotOwnership()
    {
        lock (gate)
        {
            return profiles.ToDictionary(
                item => item.Id,
                item => item.OwnedServiceNames.ToList(),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    private void RestoreOwnership(Dictionary<string, List<string>> ownershipSnapshot)
    {
        if (ownershipSnapshot.Count == 0)
        {
            return;
        }

        lock (gate)
        {
            foreach (var profile in profiles)
            {
                if (!ownershipSnapshot.TryGetValue(profile.Id, out var owned) || owned.Count == 0)
                {
                    continue;
                }

                profile.OwnedServiceNames.Clear();
                foreach (var name in owned)
                {
                    profile.OwnedServiceNames.Add(name);
                }
            }
        }
    }

    private async Task<bool> WaitForHealthyAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + healthWaitTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (service.Status is ServiceStatus.Error or ServiceStatus.FailedRepair)
            {
                return false;
            }

            if (service.Process is { HasExited: true })
            {
                return false;
            }

            try
            {
                var health = await healthCheckService.CheckAsync(service, cancellationToken);
                service.Health = health;
                if (health == HealthStatus.Healthy)
                {
                    return true;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // Keep polling.
            }

            await Task.Delay(healthPollInterval, cancellationToken);
        }

        return false;
    }

    private static void OnUi(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    private ServiceInfoModel? FindService(string serviceName)
        => serviceManager.Services.FirstOrDefault(service =>
            string.Equals(service.Name, serviceName, StringComparison.OrdinalIgnoreCase));

    private static RuntimeProfileMemberItem FindMember(RuntimeProfileItem profile, string serviceName)
    {
        RuntimeProfileMemberItem? existing = null;
        OnUi(() =>
        {
            existing = profile.Members.FirstOrDefault(member =>
                string.Equals(member.ServiceName, serviceName, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                existing = new RuntimeProfileMemberItem { ServiceName = serviceName };
                profile.Members.Add(existing);
            }
        });
        return existing!;
    }
}

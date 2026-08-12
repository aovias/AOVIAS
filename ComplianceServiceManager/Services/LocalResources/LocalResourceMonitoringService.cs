using ComplianceServiceManager.Models;
using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

/// <summary>
/// Centralized local resource polling and snapshot assembly.
/// Observation only — never mutates runtime profiles or starts/stops services.
/// </summary>
public sealed class LocalResourceMonitoringService : ILocalResourceMonitoringService
{
    private readonly ILocalResourceCollector collector;
    private readonly IServiceRuntimeHost? serviceHost;
    private readonly object gate = new();
    private CancellationTokenSource? pollCts;
    private Task? pollTask;
    private bool disposed;

    public LocalResourceMonitoringService(
        ILocalResourceCollector collector,
        LocalResourceMonitoringOptions? options = null,
        IServiceRuntimeHost? serviceHost = null)
    {
        this.collector = collector;
        this.serviceHost = serviceHost;
        Options = options ?? new LocalResourceMonitoringOptions();
        if (Options.PollInterval < TimeSpan.FromSeconds(1))
        {
            Options.PollInterval = TimeSpan.FromSeconds(1);
        }
    }

    public LocalResourceMonitoringOptions Options { get; }

    public LocalResourceSnapshot? LatestSnapshot { get; private set; }

    public event Action<LocalResourceSnapshot>? SnapshotUpdated;

    public void Start()
    {
        ThrowIfDisposed();
        lock (gate)
        {
            if (pollCts is not null)
            {
                return;
            }

            pollCts = new CancellationTokenSource();
            var token = pollCts.Token;
            // Warm-up sample so the next interval can produce CPU percentages.
            _ = CaptureSnapshot(EnumerateTrackedServices());
            pollTask = Task.Run(() => PollLoopAsync(token), CancellationToken.None);
        }
    }

    public void Stop()
    {
        CancellationTokenSource? cts;
        Task? task;
        lock (gate)
        {
            cts = pollCts;
            task = pollTask;
            pollCts = null;
            pollTask = null;
        }

        if (cts is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
        }
        catch
        {
        }

        try
        {
            task?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
        }

        cts.Dispose();
    }

    public LocalResourceSnapshot CaptureSnapshot(IEnumerable<(string ServiceName, int? ProcessId)>? services = null)
    {
        ThrowIfDisposed();

        var host = collector.CollectHost(Options.DiskProbePath, Options.Thresholds);
        var serviceList = (services ?? EnumerateTrackedServices()).ToList();
        var rows = new List<LocalServiceProcessMetrics>(serviceList.Count);

        foreach (var (serviceName, processId) in serviceList)
        {
            if (processId is null or <= 0)
            {
                rows.Add(new LocalServiceProcessMetrics
                {
                    ServiceName = serviceName,
                    ProcessId = processId,
                    AssociationNote = "No tracked process id (Studio associates a single primary PID per service)."
                });
                continue;
            }

            LocalProcessResourceMetrics processMetrics;
            try
            {
                processMetrics = collector.CollectProcess(processId.Value);
            }
            catch (Exception ex)
            {
                processMetrics = new LocalProcessResourceMetrics
                {
                    ProcessId = processId.Value,
                    IsAccessible = false,
                    Error = ex.GetType().Name
                };
            }

            rows.Add(new LocalServiceProcessMetrics
            {
                ServiceName = serviceName,
                ProcessId = processId,
                Process = processMetrics,
                AssociationNote = processMetrics.IsAccessible
                    ? "Associated via ServiceInfoModel.ProcessId (single primary process)."
                    : processMetrics.Error ?? "Process unavailable."
            });
        }

        var snapshot = new LocalResourceSnapshot
        {
            CapturedAt = DateTimeOffset.UtcNow,
            Host = host,
            Services = rows,
            SampleInterval = Options.PollInterval,
            Source = "LocalHost"
        };

        lock (gate)
        {
            LatestSnapshot = snapshot;
        }

        SnapshotUpdated?.Invoke(snapshot);
        return snapshot;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Stop();
        (collector as IDisposable)?.Dispose();
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(Options.PollInterval);
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                try
                {
                    var tracked = EnumerateTrackedServices().ToList();
                    CaptureSnapshot(tracked);
                }
                catch
                {
                    // Never let monitoring crash Studio.
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private IEnumerable<(string ServiceName, int? ProcessId)> EnumerateTrackedServices()
    {
        if (serviceHost is null)
        {
            return [];
        }

        return serviceHost.Services
            .Where(service => service.Status == ServiceStatus.Running)
            .Select(service => (service.Name, service.ProcessId))
            .ToList();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}

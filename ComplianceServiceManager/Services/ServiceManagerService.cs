using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class ServiceManagerService : IServiceRuntimeHost, IDisposable
{
    private readonly ServiceDiscoveryService discoveryService;
    private readonly BuildService buildService;
    private readonly ProcessService processService;
    private readonly HealthCheckService healthCheckService;
    private readonly ILoggingService logger;
    private readonly object developerCertificateCheckSync = new();
    private readonly DispatcherTimer healthTimer;
    private CancellationTokenSource disposeToken = new();
    private Task<bool>? developerCertificateCheckTask;
    private bool hasCheckedDeveloperCertificate;

    public ServiceManagerService(
        ServiceDiscoveryService discoveryService,
        BuildService buildService,
        ProcessService processService,
        HealthCheckService healthCheckService,
        ILoggingService logger)
    {
        this.discoveryService = discoveryService;
        this.buildService = buildService;
        this.processService = processService;
        this.healthCheckService = healthCheckService;
        this.logger = logger;

        healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        healthTimer.Tick += async (_, _) => await RefreshHealthAsync(disposeToken.Token);
        healthTimer.Start();
    }

    public ObservableCollection<ServiceInfoModel> Services { get; } = new();

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        logger.Info("Discovery", "Refreshing services.");
        var discovered = await discoveryService.DiscoverAsync(cancellationToken);
        var existingByPath = Services.ToDictionary(service => service.ProjectPath, StringComparer.OrdinalIgnoreCase);
        Services.Clear();

        foreach (var service in discovered)
        {
            if (existingByPath.TryGetValue(service.ProjectPath, out var existing) && existing.Status != ServiceStatus.Stopped)
            {
                existing.Name = service.Name;
                existing.Kind = service.Kind;
                existing.LaunchUrl = service.LaunchUrl;
                existing.Port = service.Port;
                existing.LaunchProfileName = service.LaunchProfileName;
                existing.HttpsUrl = service.HttpsUrl;
                existing.HttpsPort = service.HttpsPort;
                existing.HttpUrl = service.HttpUrl;
                existing.HttpPort = service.HttpPort;
                existing.EnvironmentName = service.EnvironmentName;
                existing.LaunchEnvironmentVariables = service.LaunchEnvironmentVariables;
                Services.Add(existing);
            }
            else
            {
                Services.Add(service);
            }
        }

        foreach (var service in Services)
        {
            await ReconcileExternalStateAsync(service, cancellationToken);
        }
    }

    public async Task StartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        try
        {
            if (service.Status is ServiceStatus.Running or ServiceStatus.Starting)
            {
                logger.Info(service.Name, "Service is already running or starting.");
                return;
            }

            service.Status = ServiceStatus.Starting;
            service.Health = HealthStatus.Starting;
            service.LastAction = "Building";

            foreach (var port in new[] { service.HttpsPort, service.HttpPort }.Where(port => port is not null).Cast<int>().Distinct())
            {
                if (IsPortInUse(port, service.Name))
                {
                    if (await TryAttachExternalInstanceAsync(service, cancellationToken))
                    {
                        logger.Info(service.Name, $"Service is already running on port {port}. Attached to existing instance.");
                        return;
                    }

                    MarkError(service, $"Port {port} is already in use", FailureCategory.PortConflict);
                    logger.Error(service.Name, $"Port conflict detected on {port}. Service was not started.");
                    return;
                }
            }

            if (service.HttpsUrl is not null && !await CheckDeveloperCertificateAsync(cancellationToken))
            {
                logger.Warning(service.Name, "Could not verify the ASP.NET Core HTTPS developer certificate. Continuing startup; if HTTPS bind fails, run `dotnet dev-certs https --check --trust`.");
            }

            var buildSucceeded = await buildService.BuildAsync(service, cancellationToken);
            if (!buildSucceeded)
            {
                MarkError(service, "Build failed", FailureCategory.BuildFailure);
                return;
            }

            service.LastAction = "Starting";
            var (fileName, arguments) = BuildStartCommand(service);
            var environmentVariables = BuildEnvironmentVariables(service);
            logger.Info(service.Name, $"Starting {service.Name} using profile {service.DisplayLaunchProfileName}.");
            logger.Info(service.Name, $"URL: {service.LaunchUrl ?? service.HttpsUrl ?? service.HttpUrl ?? "Not configured"}");
            logger.Info(service.Name, $"Environment: {service.EnvironmentName}");

            var process = processService.StartLongRunning(
                fileName,
                arguments,
                service.WorkingDirectory,
                service.Name,
                environmentVariables,
                line => CaptureRuntimeHint(service, line),
                _ => { });

            service.Process = process;
            service.ProcessId = process.Id;
            service.RunningSince = DateTimeOffset.Now;
            service.Status = ServiceStatus.Running;
            service.LastAction = "Started";
            logger.Info(service.Name, $"Started with PID {process.Id}.");
            logger.Info(service.Name, $"Open URL: {service.OpenUrl ?? "Unavailable until runtime URL is known"}");

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            if (process.HasExited)
            {
                MarkError(service, $"Process exited with code {process.ExitCode}", FailureCategory.RuntimeException);
                logger.Error(service.Name, $"Process exited immediately with code {process.ExitCode}. Review the PROCESS/ERROR log lines above for the host startup failure.");
                service.Process = null;
                process.Dispose();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MarkError(service, ex.Message, ClassifyFailure(ex.Message));
            logger.Error(service.Name, $"Start failed: {ex}");
        }
    }

    public async Task StopAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        service.Status = ServiceStatus.Stopping;
        service.LastAction = "Stopping";

        try
        {
            if (service.Process is { HasExited: false })
            {
                await processService.StopAsync(service.Process, service.Name, cancellationToken);
            }
            else if (service.ProcessId is int trackedProcessId)
            {
                PortProcessResolver.TryStopProcessTree(trackedProcessId, service.Name, logger);
            }
            else if (PortProcessResolver.TryGetListeningProcessId(service) is int externalProcessId)
            {
                PortProcessResolver.TryStopProcessTree(externalProcessId, service.Name, logger);
            }

            service.Process = null;
            service.ProcessId = null;
            service.RunningSince = null;
            service.Status = ServiceStatus.Stopped;
            service.Health = HealthStatus.Unknown;
            service.LastAction = "Stopped";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            MarkError(service, ex.Message, FailureCategory.RuntimeException);
        }
    }

    public async Task RestartAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        logger.Info(service.Name, "Restart requested.");
        await StopAsync(service, cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        await StartAsync(service, cancellationToken);
    }

    public async Task StartAllAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(Services.ToList().Select(service => StartAsync(service, cancellationToken)));
    }

    public async Task StopAllAsync(CancellationToken cancellationToken)
    {
        foreach (var service in Services.ToList())
        {
            await StopAsync(service, cancellationToken);
        }
    }

    public async Task RestartAllAsync(CancellationToken cancellationToken)
    {
        foreach (var service in Services.ToList())
        {
            await RestartAsync(service, cancellationToken);
        }
    }

    public async Task RefreshHealthAsync(CancellationToken cancellationToken)
    {
        foreach (var service in Services.Where(service => service.Status == ServiceStatus.Running).ToList())
        {
            try
            {
                if (service.Process is null && !PortProcessResolver.IsAnyPortListening(service))
                {
                    service.Status = ServiceStatus.Stopped;
                    service.Health = HealthStatus.Unknown;
                    service.ProcessId = null;
                    service.RunningSince = null;
                    service.LastAction = "External process no longer detected";
                    continue;
                }

                service.Health = await healthCheckService.CheckAsync(service, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.Warning(service.Name, $"Health check error: {ex.Message}");
                service.Health = HealthStatus.Unreachable;
            }
        }
    }

    public void Dispose()
    {
        healthTimer.Stop();
        disposeToken.Cancel();

        foreach (var service in Services.ToList())
        {
            if (service.Process is { HasExited: false } process)
            {
                logger.Info(service.Name, $"Detaching from PID {process.Id}; service keeps running after AOVIAS Command closes.");
            }

            service.Process?.Dispose();
            service.Process = null;
        }

        disposeToken.Dispose();
        healthCheckService.Dispose();
    }

    private void CaptureRuntimeHint(ServiceInfoModel service, string line)
    {
        const string nowListeningOn = "Now listening on:";
        var index = line.IndexOf(nowListeningOn, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            CaptureRuntimeUrl(service, line[(index + nowListeningOn.Length)..].Trim());
            return;
        }

        if (service.Kind == ServiceKind.AngularWebApp)
        {
            var match = Regex.Match(line, @"https?://(?:localhost|127\.0\.0\.1|\[[^\]]+\]|[^\s/]+):\d+", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                CaptureRuntimeUrl(service, match.Value);
            }
        }
    }

    private static void CaptureRuntimeUrl(ServiceInfoModel service, string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            service.LaunchUrl = uri.ToString().TrimEnd('/');
            service.Port = uri.Port;
            if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                service.HttpsUrl = service.LaunchUrl;
                service.HttpsPort = uri.Port;
            }
            else if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                service.HttpUrl = service.LaunchUrl;
                service.HttpPort = uri.Port;
            }
        }
    }

    private async Task<bool> CheckDeveloperCertificateAsync(CancellationToken cancellationToken)
    {
        if (hasCheckedDeveloperCertificate)
        {
            return true;
        }

        Task<bool> checkTask;
        lock (developerCertificateCheckSync)
        {
            developerCertificateCheckTask ??= CheckDeveloperCertificateCoreAsync(cancellationToken);
            checkTask = developerCertificateCheckTask;
        }

        try
        {
            var isValid = await checkTask;
            if (!isValid)
            {
                ResetDeveloperCertificateCheck(checkTask);
            }

            return isValid;
        }
        catch
        {
            ResetDeveloperCertificateCheck(checkTask);
            throw;
        }
    }

    private async Task<bool> CheckDeveloperCertificateCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await processService.RunCommandAsync(
                "dotnet",
                "dev-certs https --check",
                Environment.CurrentDirectory,
                "HTTPS Certificate",
                LogLevel.Info,
                cancellationToken);

            if (!result.Succeeded)
            {
                logger.Warning("HTTPS Certificate", "ASP.NET Core HTTPS developer certificate was not found or is not trusted. Services may fail to bind HTTPS until `dotnet dev-certs https --trust` is run.");
                return false;
            }

            hasCheckedDeveloperCertificate = true;
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.Warning("HTTPS Certificate", $"Unable to verify ASP.NET Core developer certificate: {ex.Message}");
            return false;
        }
    }

    private void ResetDeveloperCertificateCheck(Task<bool> checkTask)
    {
        lock (developerCertificateCheckSync)
        {
            if (ReferenceEquals(developerCertificateCheckTask, checkTask))
            {
                developerCertificateCheckTask = null;
            }
        }
    }

    private static (string FileName, string Arguments) BuildStartCommand(ServiceInfoModel service)
    {
        if (service.Kind == ServiceKind.AngularWebApp)
        {
            var port = service.Port ?? service.HttpPort ?? 4200;
            return ("cmd.exe", $"/c ng serve --host 0.0.0.0 --port {port}");
        }

        return ("dotnet", BuildRunArguments(service));
    }
    private static string BuildRunArguments(ServiceInfoModel service)
    {
        return !string.IsNullOrWhiteSpace(service.LaunchProfileName)
            ? $"run --project \"{service.ProjectPath}\" --no-build --launch-profile \"{service.LaunchProfileName}\""
            : $"run --project \"{service.ProjectPath}\" --no-build";
    }

    private static Dictionary<string, string> BuildEnvironmentVariables(ServiceInfoModel service)
    {
        var variables = new Dictionary<string, string>(service.LaunchEnvironmentVariables, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(service.EnvironmentName))
        {
            variables["ASPNETCORE_ENVIRONMENT"] = service.EnvironmentName;
        }

        if (string.IsNullOrWhiteSpace(service.LaunchProfileName))
        {
            var urls = string.Join(';', new[] { service.HttpsUrl, service.HttpUrl }.Where(url => !string.IsNullOrWhiteSpace(url)));
            if (!string.IsNullOrWhiteSpace(urls))
            {
                variables["ASPNETCORE_URLS"] = urls;
            }
        }

        return variables;
    }

    private static void MarkError(ServiceInfoModel service, string reason)
    {
        MarkError(service, reason, FailureCategory.Unknown);
    }

    private static void MarkError(ServiceInfoModel service, string reason, FailureCategory category)
    {
        service.Status = ServiceStatus.Error;
        service.Health = HealthStatus.Unreachable;
        service.LastAction = reason;
        service.ProcessId = null;
        service.RunningSince = null;
        service.LastFailureCategory = category;
        service.RepairStage = RepairStage.FailedRepair;
        service.IsRepairing = false;
    }

    public IReadOnlyList<ServiceInfoModel> GetFailedServices()
    {
        return Services
            .Where(service => service.Status is ServiceStatus.Error or ServiceStatus.FailedRepair)
            .ToList();
    }

    private bool IsPortInUse(int port, string source)
        => PortProcessResolver.IsPortListening(port);

    private async Task ReconcileExternalStateAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        if (service.Process is { HasExited: false })
        {
            service.Status = ServiceStatus.Running;
            service.ProcessId = service.Process.Id;
            service.RunningSince ??= DateTimeOffset.Now;
            service.LastAction = "Running";
            return;
        }

        service.Process = null;

        if (!PortProcessResolver.IsAnyPortListening(service))
        {
            if (service.Status == ServiceStatus.Running)
            {
                service.Status = ServiceStatus.Stopped;
                service.ProcessId = null;
                service.RunningSince = null;
                service.Health = HealthStatus.Unknown;
                service.LastAction = "Stopped";
            }

            return;
        }

        var listeningPort = PortProcessResolver.GetListeningPorts(service).FirstOrDefault(PortProcessResolver.IsPortListening);
        service.ProcessId = PortProcessResolver.TryGetListeningProcessId(service);
        service.Status = ServiceStatus.Running;
        service.RunningSince ??= DateTimeOffset.Now;
        service.LastAction = listeningPort > 0
            ? $"Detected running on port {listeningPort}"
            : "Detected running externally";

        try
        {
            service.Health = await healthCheckService.CheckAsync(service, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            service.Health = HealthStatus.Unknown;
        }

        logger.Info(service.Name, $"{service.LastAction}. PID: {service.ProcessId?.ToString() ?? "unknown"}.");
    }

    private async Task<bool> TryAttachExternalInstanceAsync(ServiceInfoModel service, CancellationToken cancellationToken)
    {
        if (!PortProcessResolver.IsAnyPortListening(service))
        {
            return false;
        }

        await ReconcileExternalStateAsync(service, cancellationToken);
        return service.Status == ServiceStatus.Running
               && service.Health is HealthStatus.Healthy or HealthStatus.Starting or HealthStatus.Unknown;
    }

    private static FailureCategory ClassifyFailure(string message)
    {
        if (message.Contains("address already in use", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Port", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.PortConflict;
        }

        if (message.Contains("Unable to resolve service", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.DependencyInjectionFailure;
        }

        if (message.Contains("migration", StringComparison.OrdinalIgnoreCase))
        {
            return FailureCategory.EfMigrationIssue;
        }

        return FailureCategory.RuntimeException;
    }
}

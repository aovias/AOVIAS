using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Registration;

/// <summary>
/// Sprint M4.3 — Environment Registration &amp; Capability Declaration.
///
/// Automatically registers this Runtime Agent with the Operations Command Center's Fleet Registry
/// at startup, and keeps re-verifying that registration for the agent's whole lifetime — no manual
/// registration step, no external bootstrap tool required (contrast with Sprint M4.1's bootstrap
/// orchestrator, which calls the same endpoints from an external process during first-time VPS
/// setup; this hosted service is what makes that automatic on every subsequent start of the agent
/// itself, including ones the bootstrap tool never sees).
///
/// Two-step registration reuses the exact same sequence Sprint M4.1 already established, just
/// triggered from inside the agent process instead of by an external caller:
/// 1. In-process call into <see cref="EnvironmentRegistrationStore"/> (already a DI singleton in
///    this same process) — the existing token-validated self-registration issues a real,
///    middleware-valid API key. No network round-trip needed since both are the same process.
/// 2. <see cref="ICommandCenterRegistrationClient.RegisterAsync"/> submits that key to Command
///    Center's Fleet Registry.
///
/// Idempotency/restart-survival: <see cref="EnvironmentRegistrationRecordStore"/> persists the
/// returned identity. Every loop iteration — including the very first one after a process restart —
/// checks the persisted record first: if Command Center still knows that id (<c>TryGetAsync</c>
/// returns non-null), nothing is re-submitted; only a 404 (or no persisted record at all) triggers
/// step 1/2 again. Combined with <c>FleetRegistry.Register</c>'s own BaseUrl-based upsert (Sprint
/// M4.3, server side), a duplicate Fleet Registry entry cannot result even if both defenses are
/// exercised at once (e.g. the local record file was lost but Command Center still remembers the
/// BaseUrl).
///
/// Retry/offline recovery: Command Center being unreachable never throws out of this loop or blocks
/// agent startup — every attempt is wrapped in try/catch, logged, and retried after
/// <see cref="EnvironmentRegistrationSettings.RetryIntervalSeconds"/> (Final Engineering Rule:
/// "Runtime Agent remains autonomous" — this agent must be fully functional with Command Center
/// offline or never having existed).
/// </summary>
public sealed class EnvironmentRegistrationHostedService(
    IOptionsMonitor<EnvironmentRegistrationSettings> registrationSettings,
    IOptionsMonitor<RuntimeSettings> runtimeSettings,
    IOptionsMonitor<ApiSettings> apiSettings,
    EnvironmentRegistrationStore selfRegistrationStore,
    EnvironmentRegistrationRecordStore recordStore,
    ICommandCenterRegistrationClient commandCenterClient,
    RuntimeCapabilitiesProvider capabilitiesProvider,
    ILogger<EnvironmentRegistrationHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = registrationSettings.CurrentValue;
            if (!settings.Enabled)
            {
                return;
            }

            try
            {
                await EnsureRegisteredAsync(settings, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "Environment registration attempt failed; will retry in {RetryIntervalSeconds}s.",
                    settings.RetryIntervalSeconds);
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(5, settings.RetryIntervalSeconds)), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
        }
    }

    /// <summary>Internal (not private) so <c>Aovias.DeploymentValidation.Tests</c> can drive the registration decision tree deterministically without waiting on the real retry-interval timer in <see cref="ExecuteAsync"/> — no behavior change.</summary>
    internal async Task EnsureRegisteredAsync(EnvironmentRegistrationSettings settings, CancellationToken cancellationToken)
    {
        var record = await recordStore.TryLoadAsync(cancellationToken);
        if (record is not null)
        {
            var known = await commandCenterClient.TryGetAsync(settings.CommandCenterBaseUrl, record.EnvironmentId, cancellationToken);
            if (known is not null)
            {
                logger.LogDebug("Environment {EnvironmentId} is still known to Command Center; no re-registration needed.", record.EnvironmentId);
                return;
            }

            logger.LogInformation(
                "Command Center no longer knows environment {EnvironmentId} (likely a Command Center restart); re-registering.",
                record.EnvironmentId);
        }

        await RegisterAsync(settings, cancellationToken);
    }

    private async Task RegisterAsync(EnvironmentRegistrationSettings settings, CancellationToken cancellationToken)
    {
        var runtime = runtimeSettings.CurrentValue;
        var api = apiSettings.CurrentValue;

        var environmentType = Enum.TryParse<EnvironmentType>(settings.EnvironmentType, ignoreCase: true, out var parsed)
            ? parsed
            : EnvironmentType.LocalDocker;
        var environmentName = string.IsNullOrWhiteSpace(runtime.AgentDisplayName) ? runtime.ServerName : runtime.AgentDisplayName;

        var selfRegistration = selfRegistrationStore.Register(new AgentRegistrationRequest
        {
            RegistrationToken = settings.RegistrationToken,
            EnvironmentName = environmentName,
            EnvironmentType = environmentType,
            RuntimeVersion = runtime.RuntimeVersion,
            Hostname = runtime.Hostname,
            PublicUrl = api.PublicUrl
        });

        var capabilities = capabilitiesProvider.GetCapabilities(runtime.RuntimeVersion).Capabilities;

        var registered = await commandCenterClient.RegisterAsync(settings.CommandCenterBaseUrl, new FleetRegistrationRequest
        {
            Name = environmentName,
            Description = $"Auto-registered from {runtime.Hostname}",
            BaseUrl = api.PublicUrl,
            EnvironmentType = environmentType,
            ApiKey = selfRegistration.ApiKey,
            Capabilities = capabilities
        }, cancellationToken);

        await recordStore.SaveAsync(new EnvironmentRegistrationRecord
        {
            EnvironmentId = registered.Id,
            ApiKey = selfRegistration.ApiKey
        }, cancellationToken);

        logger.LogInformation(
            "Environment '{EnvironmentName}' registered with Command Center as {EnvironmentId}.",
            registered.Name,
            registered.Id);
    }
}

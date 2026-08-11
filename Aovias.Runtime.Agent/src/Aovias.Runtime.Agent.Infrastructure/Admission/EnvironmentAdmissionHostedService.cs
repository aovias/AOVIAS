using Aovias.Configuration.Settings;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Runtime.Agent.Infrastructure.Reconciliation;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Infrastructure.Admission;

/// <summary>
/// Sprint M4.4 — Environment Admission &amp; Deployment Readiness. Continuously recomputes this
/// Runtime Agent's environment-level Deployment Ready state and persists it, so
/// <see cref="RuntimeHeartbeatService"/> always has a fresh
/// <see cref="EnvironmentAdmissionReport"/> to report without computing one inline on every
/// heartbeat request.
///
/// Composes only providers/stores that already exist for other purposes — no new discovery, no new
/// I/O ("No duplicated capability evaluation" — M4.4 rule):
/// <list type="bullet">
/// <item><see cref="LastDeploymentPlanStore"/> (Sprint M2.6 D1) for the authoritative set of
/// requirements this environment has actually been asked to satisfy — one
/// <see cref="Aovias.Deployment.Contracts.Infrastructure.InfrastructureContract.Requirements"/> list
/// per deployed service, an in-memory read.</item>
/// <item><see cref="EnvironmentRegistrationSettings.CapabilityProfileId"/> (Sprint M4.4 addition to
/// the Sprint M4.3 settings class) for the environment's declared capability profile id.</item>
/// <item><see cref="EnvironmentAdmissionEvaluator"/> (Sprint M4.4) for the actual matching — this
/// class contains no matching logic of its own, only wiring and a timer.</item>
/// </list>
///
/// Refresh triggers (M4.4 scope: "admission refresh on capability changes", "after bootstrap",
/// "after Runtime Agent restart"): the first evaluation runs immediately in <see cref="ExecuteAsync"/>
/// before the first delay, covering both a freshly bootstrapped environment (Sprint M4.1) and every
/// ordinary process restart; every subsequent tick re-reads
/// <see cref="IOptionsMonitor{TOptions}.CurrentValue"/> (already live-reloadable, the same mechanism
/// every other settings-driven hosted service in this codebase relies on) and re-reads
/// <see cref="LastDeploymentPlanStore"/>, so a changed capability profile id or a newly completed
/// deployment is picked up on the next tick without requiring a push-based hook. This is a
/// deliberate, low-risk choice: it does not modify <c>ReleaseDeploymentProvider</c>'s already-large
/// deployment execution path to fire a synchronous recompute the instant a deployment completes (see
/// the M4.4 report's disclosed limitation).
/// </summary>
public sealed class EnvironmentAdmissionHostedService(
    IOptionsMonitor<EnvironmentRegistrationSettings> registrationSettings,
    LastDeploymentPlanStore planStore,
    EnvironmentAdmissionStore admissionStore,
    ILogger<EnvironmentAdmissionHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EvaluateOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Environment admission evaluation cycle failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>Internal (not private) so <c>Aovias.DeploymentValidation.Tests</c> can drive one evaluation cycle deterministically instead of waiting on <see cref="Interval"/> — no behavior change.</summary>
    internal async Task EvaluateOnceAsync(CancellationToken cancellationToken)
    {
        var profileId = registrationSettings.CurrentValue.CapabilityProfileId;

        var requirementsByServiceId = planStore.ServiceIds
            .Select(serviceId => (serviceId, record: planStore.TryGet(serviceId)))
            .Where(pair => pair.record is not null)
            .ToDictionary(
                pair => pair.serviceId,
                pair => pair.record!.Contract.Requirements,
                StringComparer.OrdinalIgnoreCase);

        EnvironmentAdmissionReport report;
        try
        {
            report = EnvironmentAdmissionEvaluator.Evaluate(profileId, requirementsByServiceId);
        }
        catch (InvalidOperationException exception)
        {
            // Unknown capability profile id (e.g. an operator typo in EnvironmentRegistration:CapabilityProfileId).
            // Logged, not thrown — an admission misconfiguration must never crash the Runtime Agent
            // or block anything else it does (mirrors EnvironmentRegistrationHostedService's own
            // "never throw out of this loop" discipline, Sprint M4.3).
            logger.LogWarning(exception, "Environment admission evaluation could not resolve capability profile '{ProfileId}'.", profileId);
            return;
        }

        await admissionStore.SaveAsync(report, cancellationToken);

        if (!report.IsReady)
        {
            logger.LogWarning(
                "Environment is not Deployment Ready: {MissingCount} blocking requirement(s) across {ServiceCount} service(s). {Blockers}",
                report.MissingCount,
                report.ServiceIdsEvaluated.Count,
                string.Join(" | ", report.Blockers));
        }
    }
}

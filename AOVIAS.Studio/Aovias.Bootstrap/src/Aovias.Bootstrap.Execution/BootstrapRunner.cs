using Aovias.Bootstrap.Contracts;
using Aovias.Bootstrap.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aovias.Bootstrap.Execution;

/// <summary>
/// The single provisioning entry point. Extracted verbatim (composition only, no behavior change)
/// from <c>Aovias.Bootstrap.Host</c>'s <c>Program.cs</c> <c>RunBootstrapAsync</c> — that method now
/// calls this class instead of building its own service graph, and AOVIAS Studio's Operation Engine
/// calls the exact same class in-process. There is exactly one place a bootstrap run is composed
/// and invoked from; the CLI and Studio are both callers, not alternate implementations.
/// </summary>
public sealed class BootstrapRunner
{
    /// <param name="target">The target VPS and everything needed to reach/register it.</param>
    /// <param name="force">Re-runs every phase even if the state store says it already completed — see <see cref="BootstrapRequest.Force"/>.</param>
    /// <param name="workingDirectory">Where <c>bootstrap-state</c> and <c>bootstrap-reports</c> are written locally. Passing the same value across runs against the same target is what makes resume/retry work — this is unchanged from today's behavior.</param>
    /// <param name="observer">Optional live execution signals — see <see cref="IBootstrapExecutionObserver"/>. Omit for silent, report-only behavior (the CLI's and Studio's prior behavior).</param>
    /// <param name="loggerFactory">Optional; defaults to <see cref="NullLoggerFactory"/>. The CLI supplies a console-backed factory; Studio may omit it (the Operation Engine's own log pane is fed by <paramref name="observer"/>, not <see cref="ILogger"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="elevationHandler">Optional — required only when <c>Preflight</c> detects <see cref="BootstrapPrivilegeMode.PasswordRequired"/>. Omit for a target that is root or has passwordless sudo; a target that needs a password with no handler supplied fails cleanly (see <c>BootstrapOrchestrator.RequestAndCacheSudoPasswordAsync</c>) rather than hanging.</param>
    public async Task<BootstrapReport> RunAsync(
        BootstrapTarget target,
        bool force,
        string workingDirectory,
        IBootstrapExecutionObserver? observer = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default,
        IBootstrapElevationHandler? elevationHandler = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        // Runtime Package Availability — a local-machine file check, deliberately kept out of
        // Aovias.Bootstrap.Core (Guideline #9: Core never touches the local filesystem directly).
        // Failing here, before any SSH connection is opened or the orchestrator's own Preflight
        // phase runs, means an operator who picked a stale/deleted Runtime Agent package sees that
        // as a normal Preflight blocker instead of a confusing failure four phases later.
        if (string.IsNullOrWhiteSpace(target.RuntimeAgentArtifactPath) || !File.Exists(target.RuntimeAgentArtifactPath))
        {
            var failure = new BootstrapPhaseResult
            {
                Phase = BootstrapPhaseName.Preflight,
                Outcome = BootstrapPhaseOutcome.Failed,
                Message = "Preflight failed before contacting the target host.",
                RootCause = $"Runtime Package Availability: no Runtime Agent package was found at '{target.RuntimeAgentArtifactPath}'."
            };
            observer?.OnPhaseStarting(BootstrapPhaseName.Preflight);
            observer?.OnPhaseFinished(failure);
            return new BootstrapReport { TargetIdentity = target.StableIdentity, Phases = [failure] };
        }

        var services = new ServiceCollection();
        services.AddSingleton(loggerFactory ?? NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddBootstrapCore(workingDirectory);
        services.AddHttpClient();
        services.AddSingleton<IBootstrapTargetExecutor>(_ => new SshBootstrapTargetExecutor(target, observer));
        services.AddSingleton<IBootstrapRegistrationClient>(provider =>
            new HttpBootstrapRegistrationClient(provider.GetRequiredService<IHttpClientFactory>().CreateClient()));
        if (elevationHandler is not null)
        {
            services.AddSingleton(elevationHandler);
        }

        await using var provider = services.BuildServiceProvider();
        var orchestrator = provider.GetRequiredService<IBootstrapOrchestrator>();

        return await orchestrator.BootstrapAsync(
            new BootstrapRequest { Target = target, Force = force, WorkingDirectory = workingDirectory },
            observer,
            cancellationToken);
    }
}

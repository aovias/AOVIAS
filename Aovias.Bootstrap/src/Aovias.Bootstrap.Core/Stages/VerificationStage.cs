using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// End-to-end environment verification: polls the agent's heartbeat (API reachability + health)
/// until it reports healthy or attempts are exhausted, then confirms it reports a real Runtime
/// Version and that its SignalR channel actually negotiates — "healthy" means all four signals
/// (API, Heartbeat, SignalR, Runtime Version), not one HTTP 200. Re-runs on every attempt by
/// design: end-to-end health is the one thing that must never be assumed from a checkpoint.
/// </summary>
public sealed class VerificationStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.BootstrapVerification;

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var target = context.Target;
        Aovias.Release.Contracts.RemoteDeployment.RuntimeHeartbeatDto? heartbeat = null;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= context.Tunables.HealthVerificationMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Observer?.OnPhaseProgress(
                Phase,
                $"Polling Runtime Agent heartbeat — attempt {attempt} of {context.Tunables.HealthVerificationMaxAttempts}.",
                (double)attempt / context.Tunables.HealthVerificationMaxAttempts);

            try
            {
                // The heartbeat endpoint sits behind the agent's ApiKeyAuthenticationMiddleware —
                // authenticate with the key this run's registration stage was just issued
                // (real-VPS defect: an anonymous poll got 401 ten times and verification misread a
                // healthy environment as unreachable).
                heartbeat = await context.RegistrationClient.GetHeartbeatAsync(target.PublicUrl, context.IssuedApiKey, cancellationToken);
                lastError = null;
                if (string.Equals(heartbeat.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastError = exception;
                heartbeat = null;
            }

            if (attempt < context.Tunables.HealthVerificationMaxAttempts && context.Tunables.HealthVerificationPollDelayMs > 0)
            {
                await Task.Delay(context.Tunables.HealthVerificationPollDelayMs, cancellationToken);
            }
        }

        if (heartbeat is null || !string.Equals(heartbeat.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase))
        {
            var cause = lastError?.Message ?? $"reported health status '{heartbeat?.HealthStatus ?? "unreachable"}'";
            return StageOutcome.Fail(
                $"Health Check Failed — the environment's API/Heartbeat did not report healthy after {context.Tunables.HealthVerificationMaxAttempts} attempts.",
                cause);
        }

        if (string.IsNullOrWhiteSpace(heartbeat.RuntimeVersion) || string.Equals(heartbeat.RuntimeVersion, "unknown", StringComparison.OrdinalIgnoreCase))
        {
            return StageOutcome.Fail("Health Check Failed — the environment is reachable and healthy, but did not report a Runtime Version.");
        }

        context.Observer?.OnPhaseProgress(Phase, "Verifying real-time (SignalR) channel...", null);
        bool signalRHealthy;
        try
        {
            signalRHealthy = await context.RegistrationClient.VerifySignalRAsync(target.PublicUrl, cancellationToken);
        }
        catch
        {
            signalRHealthy = false;
        }

        if (!signalRHealthy)
        {
            return StageOutcome.Fail("Health Check Failed — the environment's API and heartbeat are healthy, but its real-time (SignalR) channel did not respond to negotiation.");
        }

        return StageOutcome.Ok(
            $"Environment '{heartbeat.EnvironmentId}' is reachable (API + SignalR), healthy, and reports Runtime Version {heartbeat.RuntimeVersion}.");
    }

    /// <summary>The heartbeat/version/SignalR battery IS the verification — this stage exists to verify.</summary>
    public override Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageOutcome.Ok("End-to-end verification is its own proof."));
}

using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// The Registration Engine: token-validated self-registration against the just-started agent
/// (issuing an API key), then Fleet Registry registration at the Command Center — reusing the two
/// existing registration endpoints. Retries transient failures (a container that started but has
/// not bound its port yet answers moments later), but fails FAST on a token rejection: retrying an
/// invalid token five times is noise, and the real-world symptom (an HTTP 500 whose body the old
/// client threw away) burned exactly that lesson in.
/// </summary>
public sealed class RegistrationStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.RuntimeAgentRegistration;

    /// <summary>
    /// Always re-runs, never checkpoint-skipped (M7.x.2 real-VPS lesson): registration is
    /// re-affirmed on every attempt because (a) it is idempotent end-to-end by design — the agent
    /// re-registers itself on every restart via <c>EnvironmentRegistrationHostedService</c>, and
    /// the Command Center's <c>FleetRegistry.Register</c> upserts by BaseUrl rather than
    /// duplicating — and (b) the verification stage needs the API key self-registration issues
    /// (<see cref="BootstrapStageContext.IssuedApiKey"/>) to poll the authenticated heartbeat
    /// endpoint; a checkpoint-skipped registration on a resumed run would leave verification with
    /// no key and a guaranteed 401.
    /// </summary>
    public override Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageDiscovery.NotSatisfied("Registration is re-affirmed on every run — it is idempotent end-to-end, and verification needs the API key it issues."));

    public override Task<StageValidation> ValidateAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var blockers = new List<string>();
        if (!Uri.TryCreate(context.Target.PublicUrl, UriKind.Absolute, out _))
        {
            blockers.Add($"Registration Requirements: '{context.Target.PublicUrl}' is not an absolute URL the agent can be reached at.");
        }

        if (!Uri.TryCreate(context.Target.CommandCenterBaseUrl, UriKind.Absolute, out _))
        {
            blockers.Add($"Registration Requirements: '{context.Target.CommandCenterBaseUrl}' is not an absolute Command Center URL.");
        }

        return Task.FromResult(blockers.Count > 0 ? StageValidation.Blocked(string.Join(" | ", blockers)) : StageValidation.Ok);
    }

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var target = context.Target;
        AgentRegistrationResponse? selfRegistration = null;
        Exception? lastError = null;

        for (var attempt = 1; attempt <= context.Tunables.RegistrationMaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.Observer?.OnPhaseProgress(
                Phase,
                $"Registering with the Runtime Agent — attempt {attempt} of {context.Tunables.RegistrationMaxAttempts}.",
                (double)attempt / context.Tunables.RegistrationMaxAttempts);

            try
            {
                selfRegistration = await context.RegistrationClient.RegisterAgentAsync(
                    target.PublicUrl,
                    new AgentRegistrationRequest
                    {
                        RegistrationToken = target.RegistrationToken,
                        EnvironmentName = target.EnvironmentName,
                        EnvironmentType = target.EnvironmentType,
                        RuntimeVersion = "unknown",
                        Hostname = target.Host,
                        PublicUrl = target.PublicUrl
                    },
                    cancellationToken);
                lastError = null;
                break;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastError = exception;

                // Fail fast on a token the agent affirmatively rejected — that answer will not
                // change on retry, and the old behavior (five identical retries, then a bare
                // "500 Internal Server Error") is exactly what this stage must never reproduce.
                if (IsTokenRejection(exception))
                {
                    return StageOutcome.Fail(
                        "Registration Failed — the Runtime Agent rejected the registration token.",
                        $"{exception.Message}{Environment.NewLine}{Environment.NewLine}"
                        + "The token must be one the platform recognizes for this environment (see EnvironmentRegistrationStore's seeded tokens) — "
                        + "verify the Registration Token field matches the environment's expected token exactly, then Retry Bootstrap.");
                }
            }

            if (attempt < context.Tunables.RegistrationMaxAttempts && context.Tunables.RegistrationRetryDelayMs > 0)
            {
                await Task.Delay(context.Tunables.RegistrationRetryDelayMs, cancellationToken);
            }
        }

        if (selfRegistration is null)
        {
            return StageOutcome.Fail(
                $"Registration Failed — agent self-registration did not succeed after {context.Tunables.RegistrationMaxAttempts} attempts.",
                lastError?.Message);
        }

        // Hand the freshly-issued key to the verification stage — the heartbeat endpoint it polls
        // sits behind the agent's ApiKeyAuthenticationMiddleware.
        context.IssuedApiKey = selfRegistration.ApiKey;

        try
        {
            await context.RegistrationClient.RegisterWithFleetAsync(
                target.CommandCenterBaseUrl,
                new FleetRegistrationRequest
                {
                    Name = target.EnvironmentName,
                    Description = $"Bootstrapped {DateTimeOffset.UtcNow:O}",
                    BaseUrl = target.PublicUrl,
                    EnvironmentType = target.EnvironmentType,
                    ApiKey = selfRegistration.ApiKey
                },
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return StageOutcome.Fail("Registration Failed — Fleet Registry registration failed.", exception.Message);
        }

        return StageOutcome.Ok($"Agent self-registered ({selfRegistration.EnvironmentId}) and was added to the Fleet Registry.");
    }

    /// <summary>A successful registration response IS the verification — there is no side-effect-free way to re-ask.</summary>
    public override Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(StageOutcome.Ok("Registration responses are their own verification."));

    private static bool IsTokenRejection(Exception exception)
        => exception.Message.Contains("Authentication", StringComparison.OrdinalIgnoreCase)
           || exception.Message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase)
           || exception.Message.Contains("registration token", StringComparison.OrdinalIgnoreCase);
}

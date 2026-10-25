using System.Collections.Concurrent;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Reliability;

public interface IMetricsTracker
{
    void TrackJsonValidity(string provider, bool valid, string useCase, string tenantKey, string promptVersion);
    void TrackValidatorOutcome(string provider, AIValidationOutcome outcome, string useCase, string tenantKey, string promptVersion);
    void TrackFallback(string fromProvider, string toProvider, string reason, string useCase, string tenantKey, string promptVersion);
    void TrackLatency(string provider, TimeSpan latency, string useCase, string tenantKey, string promptVersion);
    void TrackTimeout(string provider, string useCase, string tenantKey, string promptVersion);
    void TrackUserAcceptance(string useCase, string tenantKey);
    void TrackUserRejection(string useCase, string tenantKey, bool hallucinationRelated);
    void TrackAnswerMode(string answerMode, int retrievalCount, int contextSize, double modelResponseMs, bool fallbackTriggered, string useCase, string tenantKey);
    void TrackHallucination(string useCase, string tenantKey, int unsupportedClaimCount);
    IReadOnlyDictionary<string, long> GetSnapshot();
}

public sealed class MetricsTracker : IMetricsTracker
{
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<MetricsTracker> _logger;

    public MetricsTracker(ILogger<MetricsTracker> logger)
    {
        _logger = logger;
    }

    public void TrackJsonValidity(string provider, bool valid, string useCase, string tenantKey, string promptVersion)
    {
        Increment($"json.validity.{provider}.{(valid ? "valid" : "invalid")}");
        _logger.LogInformation(
            "AI metric json_validity provider={Provider} valid={Valid} useCase={UseCase} tenant={Tenant} promptVersion={PromptVersion}",
            provider, valid, useCase, tenantKey, promptVersion);
    }

    public void TrackValidatorOutcome(string provider, AIValidationOutcome outcome, string useCase, string tenantKey, string promptVersion)
    {
        Increment($"validator.outcome.{provider}.{outcome}");
        _logger.LogInformation(
            "AI metric validator_outcome provider={Provider} outcome={Outcome} useCase={UseCase} tenant={Tenant} promptVersion={PromptVersion}",
            provider, outcome, useCase, tenantKey, promptVersion);
    }

    public void TrackFallback(string fromProvider, string toProvider, string reason, string useCase, string tenantKey, string promptVersion)
    {
        Increment($"fallback.{fromProvider}.{toProvider}.{reason}");
        _logger.LogWarning(
            "AI metric fallback from={FromProvider} to={ToProvider} reason={Reason} useCase={UseCase} tenant={Tenant} promptVersion={PromptVersion}",
            fromProvider, toProvider, reason, useCase, tenantKey, promptVersion);
    }

    public void TrackLatency(string provider, TimeSpan latency, string useCase, string tenantKey, string promptVersion)
    {
        var bucket = latency.TotalMilliseconds switch
        {
            <= 500 => "p50-ish",
            <= 1500 => "p95-ish",
            _ => "slow"
        };
        Increment($"latency.{provider}.{bucket}");
        _logger.LogInformation(
            "AI metric latency provider={Provider} latencyMs={LatencyMs} useCase={UseCase} tenant={Tenant} promptVersion={PromptVersion}",
            provider, latency.TotalMilliseconds, useCase, tenantKey, promptVersion);
    }

    public void TrackTimeout(string provider, string useCase, string tenantKey, string promptVersion)
    {
        Increment($"timeouts.{provider}");
        _logger.LogWarning(
            "AI metric timeout provider={Provider} useCase={UseCase} tenant={Tenant} promptVersion={PromptVersion}",
            provider, useCase, tenantKey, promptVersion);
    }

    public void TrackUserAcceptance(string useCase, string tenantKey)
    {
        Increment("user.accepted");
        _logger.LogInformation("AI metric user_acceptance useCase={UseCase} tenant={Tenant}", useCase, tenantKey);
    }

    public void TrackUserRejection(string useCase, string tenantKey, bool hallucinationRelated)
    {
        Increment("user.rejected");
        if (hallucinationRelated)
        {
            Increment("user.rejected.hallucination");
        }

        _logger.LogInformation(
            "AI metric user_rejection useCase={UseCase} tenant={Tenant} hallucination={HallucinationRelated}",
            useCase, tenantKey, hallucinationRelated);
    }

    public void TrackAnswerMode(string answerMode, int retrievalCount, int contextSize, double modelResponseMs, bool fallbackTriggered, string useCase, string tenantKey)
    {
        Increment($"answer.mode.{answerMode}");
        if (fallbackTriggered)
        {
            Increment("answer.mode.fallback");
        }

        _logger.LogInformation(
            "AI metric answer_mode mode={AnswerMode} retrievalCount={RetrievalCount} contextSize={ContextSize} modelResponseMs={ModelResponseMs} fallbackTriggered={FallbackTriggered} useCase={UseCase} tenant={Tenant}",
            answerMode, retrievalCount, contextSize, modelResponseMs, fallbackTriggered, useCase, tenantKey);
    }

    public void TrackHallucination(string useCase, string tenantKey, int unsupportedClaimCount)
    {
        Increment("governance.hallucination.flagged");
        if (unsupportedClaimCount > 0)
        {
            Increment($"governance.hallucination.claims.{Math.Clamp(unsupportedClaimCount, 1, 5)}");
        }

        _logger.LogWarning(
            "AI metric hallucination_flagged useCase={UseCase} tenant={Tenant} unsupportedClaims={UnsupportedClaims}",
            useCase, tenantKey, unsupportedClaimCount);
    }

    public IReadOnlyDictionary<string, long> GetSnapshot()
    {
        return _counters.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
    }

    private void Increment(string key)
    {
        _counters.AddOrUpdate(key, 1, (_, existing) => existing + 1);
    }
}

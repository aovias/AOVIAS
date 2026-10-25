using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Identity.Contracts;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Memory;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Presentation;
using AIOrchestratorService.Application.AI.Prompting;
using AIOrchestratorService.Application.AI.Reliability;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Application.AI.Validation;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Orchestration;

public interface IAIOrchestrationService
{
    Task<AIResponse> RunReadOnlyAsync(AIRequest request, CancellationToken cancellationToken);
    Task<AIResponse> RunAssistiveAsync(AIRequest request, CancellationToken cancellationToken);
}

public sealed class AIOrchestrationService : IAIOrchestrationService
{
    private readonly IRetrievalService _retrievalService;
    private readonly IPromptTemplateRegistry _promptRegistry;
    private readonly IAIPromptProfileService _promptProfileService;
    private readonly IConversationMemoryService _conversationMemory;
    private readonly IAIModelAdapter _modelAdapter;
    private readonly IAIMediator _mediator;
    private readonly IConfidenceCalibrationService _confidenceCalibration;
    private readonly IAssistantQueryClassifier _queryClassifier;
    private readonly IResponseSanitizer _responseSanitizer;
    private readonly IResponsePresentationService _presentationService;
    private readonly IMemoryCache _cache;
    private readonly IOptionsMonitor<AIPerformanceOptions> _performance;
    private readonly IOptionsMonitor<AIUsagePolicyOptions> _usagePolicy;
    private readonly IMetricsTracker _metricsTracker;
    private readonly IHallucinationGuard _hallucinationGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AIOrchestrationService> _logger;

    public AIOrchestrationService(
        IRetrievalService retrievalService,
        IPromptTemplateRegistry promptRegistry,
        IAIPromptProfileService promptProfileService,
        IConversationMemoryService conversationMemory,
        IAIModelAdapter modelAdapter,
        IAIMediator mediator,
        IConfidenceCalibrationService confidenceCalibration,
        IAssistantQueryClassifier queryClassifier,
        IResponseSanitizer responseSanitizer,
        IResponsePresentationService presentationService,
        IMemoryCache cache,
        IOptionsMonitor<AIPerformanceOptions> performance,
        IOptionsMonitor<AIUsagePolicyOptions> usagePolicy,
        IMetricsTracker metricsTracker,
        IHallucinationGuard hallucinationGuard,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AIOrchestrationService> logger)
    {
        _retrievalService = retrievalService;
        _promptRegistry = promptRegistry;
        _promptProfileService = promptProfileService;
        _conversationMemory = conversationMemory;
        _modelAdapter = modelAdapter;
        _mediator = mediator;
        _confidenceCalibration = confidenceCalibration;
        _queryClassifier = queryClassifier;
        _responseSanitizer = responseSanitizer;
        _presentationService = presentationService;
        _cache = cache;
        _performance = performance;
        _usagePolicy = usagePolicy;
        _metricsTracker = metricsTracker;
        _hallucinationGuard = hallucinationGuard;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public Task<AIResponse> RunReadOnlyAsync(AIRequest request, CancellationToken cancellationToken)
    {
        return RunCoreAsync(request, includeSuggestedAction: false, cancellationToken);
    }

    public Task<AIResponse> RunAssistiveAsync(AIRequest request, CancellationToken cancellationToken)
    {
        return RunCoreAsync(request, includeSuggestedAction: true, cancellationToken);
    }

    private async Task<AIResponse> RunCoreAsync(AIRequest request, bool includeSuggestedAction, CancellationToken cancellationToken)
    {
        EnforceUsageLimits(request);

        var cacheTtl = TimeSpan.FromSeconds(Math.Max(1, _performance.CurrentValue.CacheTtlSeconds));
        // Tenant isolation: cache keys MUST include HospitalId so AI responses never leak across hospitals.
        // Notifications/storage similarly scope by hospital (see AceNotificationBridge / blob prefixes).
        var cacheKey = $"ai:{request.HospitalId:N}:{request.UseCase}:{request.ContextVersionId}:{request.Prompt}";
        if (_cache.TryGetValue(cacheKey, out AIResponse? cached) && cached != null)
        {
            cached.WasFallback = false;
            return cached;
        }

        var timeoutMs = Math.Max(500, _performance.CurrentValue.RequestTimeoutMs);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(timeoutMs));

        try
        {
            ResolveUserId(request);
            if (string.IsNullOrWhiteSpace(request.ConversationSummary))
            {
                var memorySummary = await _conversationMemory.LoadSummaryAsync(
                    request.HospitalId,
                    request.UserId,
                    maxMessages: 12,
                    timeoutCts.Token);
                if (!string.IsNullOrWhiteSpace(memorySummary))
                {
                    request.ConversationSummary = memorySummary;
                }
            }

            var retrievalContext = await _retrievalService.BuildContextAsync(request, timeoutCts.Token);
            request.ContextVersionId = retrievalContext.ContextVersionId;

            var retrievalCount = retrievalContext.Sources.Count;
            var answerMode = _queryClassifier.Classify(request.Prompt, retrievalCount > 0);

            var template = _promptRegistry.ResolveOrDefault(request);
            request.PromptTemplateId = template.Id;
            request.PromptTemplateVersion = template.Version;

            // Prefer AI Identity prompt profile when AiProfileId is set; else seed/default registry.
            var systemPrompt = template.SystemPrompt;
            if (request.AiProfileId.HasValue)
            {
                try
                {
                    var merged = await _promptProfileService.ResolveMergedPromptAsync(
                        request.AiProfileId,
                        request.PromptTemplateId,
                        timeoutCts.Token);
                    if (!string.IsNullOrWhiteSpace(merged))
                    {
                        systemPrompt = merged;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "AI Identity prompt resolve failed; using PromptTemplateRegistry default.");
                }
            }

            var modelTimer = Stopwatch.StartNew();
            var redactedPrompt = SensitiveDataRedactor.Redact(request.Prompt);
            var modelResponse = await _modelAdapter.GenerateAsync(new AIModelRequest
            {
                TenantKey = request.HospitalId.ToString("N"),
                Prompt = redactedPrompt,
                SystemPrompt = systemPrompt,
                PromptTemplateVersion = template.Version,
                UseCase = request.UseCase,
                Context = retrievalContext,
                AnswerMode = answerMode
            }, timeoutCts.Token);
            modelTimer.Stop();

            // When an organization-specific query genuinely has no retrieved data, do not trust
            // the model's wording (it tends to leak "context is empty / impossible to summarize").
            // Produce a clean, deterministic workspace message instead.
            var orgDataMissing = AssistantQueryClassifier.RequiresOrgData(answerMode)
                && retrievalContext.Sources.Count == 0
                && retrievalContext.Facts.Count == 0;

            string sanitizedHint;
            List<string> reasoningBullets;
            if (orgDataMissing)
            {
                sanitizedHint = "I couldn't find any matching records in your current workspace for that request. "
                    + "Make sure the right hospital and framework are selected, or open the related module (Findings, Tasks, Evidence, Audits) to review the latest details.";
                reasoningBullets = new List<string> { "No matching records were available in the current workspace context." };
            }
            else
            {
                sanitizedHint = _responseSanitizer.Sanitize(modelResponse.Hint, answerMode);
                reasoningBullets = _responseSanitizer.SanitizeReasoning(modelResponse.ReasoningBullets);
            }

            var response = new AIResponse
            {
                ComplianceHint = sanitizedHint,
                ConfidenceScore = modelResponse.Confidence,
                AnswerMode = answerMode,
                Reasoning = reasoningBullets.Select((x, i) => new AIReasoningStep
                {
                    Code = $"LLM_STEP_{i + 1}",
                    Message = x
                }).ToList(),
                SourceReferences = retrievalContext.Sources,
                ContextVersionId = retrievalContext.ContextVersionId,
                PromptTemplateId = template.Id,
                PromptTemplateVersion = template.Version,
                SuggestedMapping = request.ObjectiveElementId.HasValue
                    ? new AISuggestedMapping
                    {
                        ObjectiveElementId = request.ObjectiveElementId,
                        TaskId = request.TaskId,
                        MappingReason = "Mapped to current context objective element."
                    }
                    : null
            };

            if (includeSuggestedAction)
            {
                response.SuggestedAction = new AISuggestedAction
                {
                    ToolName = "create_task",
                    RiskClass = AIActionRiskClass.Low,
                    Parameters = new Dictionary<string, string>
                    {
                        ["description"] = "Create follow-up task from AI assistive suggestion."
                    }
                };
            }

            response = _mediator.Mediate(request, response);
            response.AnswerMode = answerMode;

            // Defensive: scrub any forbidden internal language the validator/model may have left behind.
            response.ComplianceHint = _responseSanitizer.Sanitize(response.ComplianceHint, answerMode);

            // Re-check with retrieval facts when available (validator already did citation-only pass).
            if (retrievalContext.Facts.Count > 0)
            {
                var hallucination = _hallucinationGuard.Evaluate(
                    response.ComplianceHint,
                    response.SourceReferences,
                    retrievalContext.Facts,
                    response.ConfidenceScore);
                if (hallucination.IsFlagged)
                {
                    response.ConfidenceScore = hallucination.AdjustedConfidence;
                    if (response.ValidationOutcome == AIValidationOutcome.Accept)
                    {
                        response.ValidationOutcome = AIValidationOutcome.Correct;
                    }

                    if (!response.Reasoning.Any(r =>
                            string.Equals(r.Code, "HALLUCINATION_GUARD", StringComparison.OrdinalIgnoreCase)))
                    {
                        response.Reasoning.Insert(0, new AIReasoningStep
                        {
                            Code = "HALLUCINATION_GUARD",
                            Message = hallucination.Reason
                        });
                    }

                    _metricsTracker.TrackHallucination(
                        request.UseCase.ToString(),
                        request.HospitalId.ToString("N"),
                        hallucination.UnsupportedClaims.Count);
                }
            }
            else if (response.Reasoning.Any(r =>
                         string.Equals(r.Code, "HALLUCINATION_GUARD", StringComparison.OrdinalIgnoreCase)))
            {
                _metricsTracker.TrackHallucination(
                    request.UseCase.ToString(),
                    request.HospitalId.ToString("N"),
                    unsupportedClaimCount: 1);
            }

            _metricsTracker.TrackValidatorOutcome("active", response.ValidationOutcome, request.UseCase.ToString(), request.HospitalId.ToString("N"), template.Version);
            response.CompositeConfidenceScore = _confidenceCalibration.ComputeComposite(
                response.ConfidenceScore,
                retrievalContext.RelevanceScore,
                response.ValidationOutcome,
                answerMode);

            // A clean "no records found" answer is a confident, correct response - not a 20% failure.
            if (orgDataMissing)
            {
                response.ConfidenceScore = 0.6m;
                response.CompositeConfidenceScore = 0.6m;
            }
            response.HumanApprovalRequired = _confidenceCalibration.RequiresHumanApproval(
                response.CompositeConfidenceScore,
                response.SuggestedAction?.RiskClass);

            // Build the executive Compliance Advisor presentation model the UI renders as cards.
            response.Presentation = _presentationService.Build(request, response, modelResponse, answerMode);

            response.ModelVersion = template.Version;
            response.TraceId = request.IdempotencyToken ?? Guid.NewGuid().ToString("N");
            response.Grounded = new GroundedAnswer
            {
                Answer = response.ComplianceHint,
                Citations = response.SourceReferences,
                Reasoning = response.Reasoning,
                ConfidenceScore = response.CompositeConfidenceScore,
                ModelVersion = response.ModelVersion ?? string.Empty,
                TraceId = response.TraceId ?? string.Empty,
                AnswerMode = answerMode,
                HumanApprovalRequired = response.HumanApprovalRequired,
                Retrieval = retrievalContext,
                Presentation = response.Presentation
            };

            var contextSize = retrievalContext.Facts.Count + retrievalContext.Sources.Count;
            _metricsTracker.TrackAnswerMode(
                answerMode.ToString(),
                retrievalCount,
                contextSize,
                modelTimer.Elapsed.TotalMilliseconds,
                fallbackTriggered: false,
                request.UseCase.ToString(),
                request.HospitalId.ToString("N"));
            _logger.LogInformation(
                "AI assistant turn processed mode={AnswerMode} retrievalCount={RetrievalCount} contextSize={ContextSize} modelResponseMs={ModelResponseMs} confidence={Confidence} validation={Validation} promptLength={PromptLength}",
                answerMode,
                retrievalCount,
                contextSize,
                modelTimer.Elapsed.TotalMilliseconds,
                response.CompositeConfidenceScore,
                response.ValidationOutcome,
                request.Prompt?.Length ?? 0);

            await _conversationMemory.SaveTurnAsync(
                request.HospitalId,
                request.UserId,
                request.AiProfileId,
                request.Prompt ?? string.Empty,
                response.ComplianceHint ?? string.Empty,
                timeoutCts.Token);

            _cache.Set(cacheKey, response, cacheTtl);
            return response;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "AI usage guardrail blocked request.");
            return new AIResponse
            {
                ComplianceHint = "AI request limit reached for current scope. Please retry later.",
                ConfidenceScore = 0.1m,
                CompositeConfidenceScore = 0.1m,
                Reasoning = new List<AIReasoningStep>
                {
                    new() { Code = "USAGE_GUARDRAIL", Message = ex.Message }
                },
                SourceReferences = new List<AISourceReference>(),
                ValidationOutcome = AIValidationOutcome.Reject,
                ContextVersionId = request.ContextVersionId,
                PromptTemplateId = request.PromptTemplateId,
                PromptTemplateVersion = request.PromptTemplateVersion,
                HumanApprovalRequired = true,
                WasFallback = true
            };
        }
        catch (ProviderFailureException ex)
        {
            _logger.LogWarning(ex, "Provider failure without fallback resolution.");
            return new AIResponse
            {
                ComplianceHint = "AI provider unavailable. Please retry after some time.",
                ConfidenceScore = 0.15m,
                CompositeConfidenceScore = 0.15m,
                Reasoning = new List<AIReasoningStep>
                {
                    new() { Code = "PROVIDER_FAILURE", Message = ex.Reason.ToString() }
                },
                SourceReferences = new List<AISourceReference>(),
                ValidationOutcome = AIValidationOutcome.Reject,
                ContextVersionId = request.ContextVersionId,
                PromptTemplateId = request.PromptTemplateId,
                PromptTemplateVersion = request.PromptTemplateVersion,
                HumanApprovalRequired = true,
                WasFallback = true
            };
        }
        catch (OperationCanceledException)
        {
            _metricsTracker.TrackTimeout("active", request.UseCase.ToString(), request.HospitalId.ToString("N"), request.PromptTemplateVersion);
            _metricsTracker.TrackAnswerMode(
                _queryClassifier.Classify(request.Prompt, false).ToString(),
                retrievalCount: 0,
                contextSize: 0,
                modelResponseMs: 0,
                fallbackTriggered: true,
                request.UseCase.ToString(),
                request.HospitalId.ToString("N"));
            _logger.LogWarning("AI orchestration timed out; returning deterministic fallback response.");
            return new AIResponse
            {
                ComplianceHint = "AI response timeout. Please review deterministic compliance dashboard insights.",
                ConfidenceScore = 0.2m,
                CompositeConfidenceScore = 0.2m,
                Reasoning = new List<AIReasoningStep>
                {
                    new() { Code = "TIMEOUT_FALLBACK", Message = "Timeout reached and fallback was returned." }
                },
                SourceReferences = new List<AISourceReference>
                {
                    new() { SourceType = "DeterministicFallback", SourceId = "dashboard", RelevanceScore = 0m }
                },
                ValidationOutcome = AIValidationOutcome.Accept,
                ContextVersionId = request.ContextVersionId,
                PromptTemplateId = request.PromptTemplateId,
                PromptTemplateVersion = request.PromptTemplateVersion,
                HumanApprovalRequired = true,
                WasFallback = true
            };
        }
    }

    private void EnforceUsageLimits(AIRequest request)
    {
        var usage = _usagePolicy.CurrentValue;
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value
            ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Session-Id"].ToString()
            ?? "anonymous";
        var tenantKey = request.HospitalId.ToString("N");
        var sessionWindow = TimeSpan.FromMinutes(Math.Max(1, usage.SessionWindowMinutes));

        var sessionCounterKey = $"ai:usage:session:{tenantKey}:{userId}";
        var tenantMinuteCounterKey = $"ai:usage:tenant-minute:{tenantKey}:{DateTime.UtcNow:yyyyMMddHHmm}";

        var sessionCount = IncrementCounter(sessionCounterKey, sessionWindow);
        if (sessionCount > Math.Max(1, usage.MaxRequestsPerSession))
        {
            throw new InvalidOperationException("AI usage limit reached for this session.");
        }

        var tenantCount = IncrementCounter(tenantMinuteCounterKey, TimeSpan.FromMinutes(2));
        if (tenantCount > Math.Max(1, usage.PerTenantRateLimitPerMinute))
        {
            throw new InvalidOperationException("Tenant AI request rate limit exceeded.");
        }
    }

    private void ResolveUserId(AIRequest request)
    {
        if (request.UserId.HasValue && request.UserId != Guid.Empty)
        {
            return;
        }

        var sub = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (Guid.TryParse(sub, out var userId))
        {
            request.UserId = userId;
        }
    }

    private int IncrementCounter(string key, TimeSpan ttl)
    {
        var current = _cache.TryGetValue(key, out int existing) ? existing : 0;
        current += 1;
        _cache.Set(key, current, ttl);
        return current;
    }
}

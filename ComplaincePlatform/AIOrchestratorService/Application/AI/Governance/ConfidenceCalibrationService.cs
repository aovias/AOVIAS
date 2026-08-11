using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Models;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Governance;

public interface IConfidenceCalibrationService
{
    decimal ComputeComposite(decimal modelConfidence, decimal retrievalRelevance, AIValidationOutcome validationOutcome);
    decimal ComputeComposite(decimal modelConfidence, decimal retrievalRelevance, AIValidationOutcome validationOutcome, AssistantAnswerMode answerMode);
    bool RequiresWarning(decimal confidence);
    bool RequiresHumanApproval(decimal confidence, AIActionRiskClass? riskClass);
}

public sealed class ConfidenceCalibrationService : IConfidenceCalibrationService
{
    private readonly IOptionsMonitor<AIConfidencePolicyOptions> _options;

    public ConfidenceCalibrationService(IOptionsMonitor<AIConfidencePolicyOptions> options)
    {
        _options = options;
    }

    public decimal ComputeComposite(decimal modelConfidence, decimal retrievalRelevance, AIValidationOutcome validationOutcome)
    {
        var validationWeight = validationOutcome == AIValidationOutcome.Accept
            ? 1.0m
            : validationOutcome == AIValidationOutcome.Correct ? 0.75m : 0.15m;

        var composite = (modelConfidence * 0.5m) + (retrievalRelevance * 0.35m) + (validationWeight * 0.15m);
        return Math.Clamp(composite, 0m, 1m);
    }

    public decimal ComputeComposite(decimal modelConfidence, decimal retrievalRelevance, AIValidationOutcome validationOutcome, AssistantAnswerMode answerMode)
    {
        // For general/compliance-knowledge answers, an empty retrieval (relevance ~0.25)
        // must not crush confidence: the answer is expected to come from model knowledge.
        // We weight the model's own confidence heavily and apply a sensible floor when accepted.
        if (answerMode is AssistantAnswerMode.GeneralKnowledge or AssistantAnswerMode.ComplianceKnowledge or AssistantAnswerMode.SmallTalk)
        {
            var validationWeight = validationOutcome == AIValidationOutcome.Accept
                ? 1.0m
                : validationOutcome == AIValidationOutcome.Correct ? 0.85m : 0.3m;

            var knowledgeComposite = (modelConfidence * 0.7m) + (validationWeight * 0.3m);
            if (validationOutcome != AIValidationOutcome.Reject)
            {
                knowledgeComposite = Math.Max(knowledgeComposite, 0.85m);
            }

            return Math.Clamp(knowledgeComposite, 0m, 1m);
        }

        // Hybrid: blend model knowledge with retrieval, but don't let sparse retrieval dominate.
        if (answerMode is AssistantAnswerMode.Hybrid)
        {
            var validationWeight = validationOutcome == AIValidationOutcome.Accept
                ? 1.0m
                : validationOutcome == AIValidationOutcome.Correct ? 0.8m : 0.2m;

            var effectiveRelevance = Math.Max(retrievalRelevance, 0.6m);
            var composite = (modelConfidence * 0.55m) + (effectiveRelevance * 0.3m) + (validationWeight * 0.15m);
            return Math.Clamp(composite, 0m, 1m);
        }

        // OrganizationSpecific: keep the original retrieval-weighted formula (data matters).
        return ComputeComposite(modelConfidence, retrievalRelevance, validationOutcome);
    }

    public bool RequiresWarning(decimal confidence)
    {
        var options = _options.CurrentValue;
        return confidence >= options.SuggestOnlyThreshold && confidence < options.WarnThreshold;
    }

    public bool RequiresHumanApproval(decimal confidence, AIActionRiskClass? riskClass)
    {
        var options = _options.CurrentValue;
        if (riskClass == AIActionRiskClass.High)
        {
            return true;
        }

        return confidence < options.WarnThreshold;
    }
}

using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Services;

public static class OeWizardPayloadValidator
{
    public const int MaxStepIndexInclusive = 4;

    private static readonly HashSet<string> ValidTones = new(StringComparer.OrdinalIgnoreCase)
    {
        "compliant",
        "partial",
        "non"
    };

    public static void ValidateAndNormalize(OeWizardPersistedState p)
    {
        ArgumentNullException.ThrowIfNull(p);

        if (p.StepIndex < 0 || p.StepIndex > MaxStepIndexInclusive)
        {
            throw new ArgumentException("stepIndex is out of range.", "payload.stepIndex");
        }

        // Score is meaningless until the user submits evaluation; 0 is the CLR default when payload
        // fields fail to bind — treat as unset and align with UI default (mid scale).
        if (!p.EvalSubmitted && (p.EvalScore < 1 || p.EvalScore > 5))
        {
            p.EvalScore = 3;
        }

        if (p.EvalSubmitted && (p.EvalScore < 1 || p.EvalScore > 5))
        {
            throw new ArgumentException("evalScore must be between 1 and 5.", "payload.evalScore");
        }

        if (string.IsNullOrWhiteSpace(p.EvalTone) || !ValidTones.Contains(p.EvalTone.Trim()))
        {
            throw new ArgumentException("evalTone must be compliant, partial, or non.", "payload.evalTone");
        }

        p.EvalTone = p.EvalTone.Trim().ToLowerInvariant();

        if (p.AuditChecklistChecked is { Length: > 256 })
        {
            throw new ArgumentException("auditChecklistChecked is too large.", "payload.auditChecklistChecked");
        }
    }

    public static void ValidateForCompletion(OeWizardPersistedState p)
    {
        ValidateAndNormalize(p);

        if (!p.UnderstandAck || !p.ExecuteReady || !p.EvidenceAck)
        {
            throw new InvalidOperationException("Wizard completion requires prior step acknowledgements.");
        }

        if (!p.EvalSubmitted)
        {
            throw new InvalidOperationException("Evaluation must be submitted before completion.");
        }

        var needsImprove = p.EvalTone is "partial" or "non";
        if (needsImprove && !p.ImproveReady)
        {
            throw new InvalidOperationException("Improvement acknowledgement is required before completion.");
        }

        if ((p.EvalTone == "partial" || p.EvalTone == "non") &&
            string.IsNullOrWhiteSpace(p.EvalJustification))
        {
            throw new InvalidOperationException("evalJustification is required for partial or non-compliant tone.");
        }

        if (!p.CompleteAck)
        {
            throw new InvalidOperationException("completeAck must be true to complete the wizard.");
        }
    }
}

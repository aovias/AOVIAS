using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using Xunit;

namespace ComplianceService.Tests;

public class OeWizardContractTests
{
    [Fact]
    public void Canonicalize_is_stable_for_same_logical_payload()
    {
        var a = new OeWizardPersistedState
        {
            StepIndex = 2,
            UnderstandAck = true,
            ExecuteReady = true,
            EvidenceAck = false,
            EvalSubmitted = false,
            EvalScore = 3,
            EvalTone = "compliant",
            EvalJustification = null,
            NeedsImprove = false,
            ImproveReady = false,
            CompleteAck = false,
            AuditChecklistChecked = new[] { true, false },
            SavedAt = "2026-04-19T10:00:00.000Z"
        };

        var b = new OeWizardPersistedState
        {
            StepIndex = 2,
            UnderstandAck = true,
            ExecuteReady = true,
            EvidenceAck = false,
            EvalSubmitted = false,
            EvalScore = 3,
            EvalTone = "compliant",
            EvalJustification = null,
            NeedsImprove = false,
            ImproveReady = false,
            CompleteAck = false,
            AuditChecklistChecked = new[] { true, false },
            SavedAt = "2026-04-19T10:00:00.000Z"
        };

        Assert.Equal(OeWizardPayloadCanonicalizer.Canonicalize(a), OeWizardPayloadCanonicalizer.Canonicalize(b));
    }

    [Fact]
    public void Validator_normalizes_eval_score_zero_before_eval_submitted()
    {
        var p = new OeWizardPersistedState
        {
            StepIndex = 1,
            EvalSubmitted = false,
            EvalScore = 0,
            EvalTone = "compliant"
        };

        OeWizardPayloadValidator.ValidateAndNormalize(p);
        Assert.Equal(3, p.EvalScore);
    }

    [Fact]
    public void Validator_accepts_reported_client_payload_shape()
    {
        var p = new OeWizardPersistedState
        {
            StepIndex = 1,
            UnderstandAck = true,
            ExecuteReady = true,
            EvidenceAck = false,
            EvalSubmitted = false,
            EvalScore = 1,
            EvalTone = "compliant",
            EvalJustification = "",
            NeedsImprove = false,
            ImproveReady = false,
            CompleteAck = false,
            AuditChecklistChecked = new[] { false, false, false, false, false, false, false },
            SavedAt = "2026-04-20T07:35:45.698Z"
        };

        OeWizardPayloadValidator.ValidateAndNormalize(p);
        _ = OeWizardPayloadCanonicalizer.Canonicalize(p);
    }

    [Fact]
    public void CompletionMapper_maps_score_rationale_and_checklist_audit_remarks()
    {
        var p = new OeWizardPersistedState
        {
            EvalScore = 4,
            EvalJustification = " ok ",
            AuditChecklistChecked = new[] { true, true, false }
        };

        var patch = OeWizardCompletionMapper.ToPatchRequest(p);
        Assert.Equal(4, patch.Score);
        Assert.Equal("ok", patch.ScoreRationale);
        Assert.NotNull(patch.AuditRemarks);
        Assert.Contains("2/3 checked", patch.AuditRemarks, StringComparison.Ordinal);
        Assert.Contains("flags=110", patch.AuditRemarks, StringComparison.Ordinal);
    }
}

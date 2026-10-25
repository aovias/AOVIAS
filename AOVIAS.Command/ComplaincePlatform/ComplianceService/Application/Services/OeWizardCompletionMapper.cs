using System.Linq;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Services;

/// <summary>
/// Single place mapping wizard payload → <see cref="PatchObjectiveElementRequest"/> for completion.
/// </summary>
public static class OeWizardCompletionMapper
{
    public static PatchObjectiveElementRequest ToPatchRequest(OeWizardPersistedState payload)
    {
        var checklist = payload.AuditChecklistChecked;
        string? auditRemarks = null;
        if (checklist is { Length: > 0 })
        {
            var done = checklist.Count(b => b);
            var flags = string.Join("", checklist.Select(b => b ? "1" : "0"));
            auditRemarks = $"[oe-wizard:checklist] {done}/{checklist.Length} checked; flags={flags}";
        }

        return new PatchObjectiveElementRequest
        {
            Version = null,
            Score = payload.EvalScore,
            ScoreRationale = payload.EvalJustification?.Trim() ?? string.Empty,
            AuditRemarks = auditRemarks,
            IsNa = null,
            NaJustification = null
        };
    }
}

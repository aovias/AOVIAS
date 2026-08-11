using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class PatchDeficiencyRcaCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyAuditTrailService _auditTrail;

    public PatchDeficiencyRcaCommandHandler(ComplianceDbContext db, IDeficiencyAuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    public async Task HandleAsync(PatchDeficiencyRcaCommand command, CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        Domain.Enums.RootCauseCategory? category = null;
        if (!string.IsNullOrWhiteSpace(command.RootCauseCategory))
        {
            if (!Enum.TryParse<Domain.Enums.RootCauseCategory>(command.RootCauseCategory, ignoreCase: true, out var parsed))
            {
                throw new InvalidOperationException($"Invalid root cause category: {command.RootCauseCategory}");
            }

            category = parsed;
        }

        deficiency.UpdateRca(
            category,
            command.ProblemStatement,
            command.RootCauseSummary,
            command.ImpactDescription,
            command.CorrectiveActionSummary,
            command.PreventiveActionSummary,
            command.Why1,
            command.Why2,
            command.Why3,
            command.Why4,
            command.Why5,
            command.FinalRootCause,
            command.UpdatedBy);

        await _auditTrail.LogAsync(deficiency.Id, "RootCauseUpdated", null, command.UpdatedBy ?? "User", null, ct);
        await _db.SaveChangesAsync(ct);
    }
}

public sealed record PatchDeficiencyRcaCommand(
    Guid DeficiencyId,
    string? RootCauseCategory,
    string? ProblemStatement,
    string? RootCauseSummary,
    string? ImpactDescription,
    string? CorrectiveActionSummary,
    string? PreventiveActionSummary,
    string? Why1,
    string? Why2,
    string? Why3,
    string? Why4,
    string? Why5,
    string? FinalRootCause,
    string? UpdatedBy);

public sealed class PatchDeficiencyRemediationCommandHandler
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyAuditTrailService _auditTrail;

    public PatchDeficiencyRemediationCommandHandler(ComplianceDbContext db, IDeficiencyAuditTrailService auditTrail)
    {
        _db = db;
        _auditTrail = auditTrail;
    }

    public async Task HandleAsync(PatchDeficiencyRemediationCommand command, CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .FirstOrDefaultAsync(d => d.Id == command.DeficiencyId, ct)
            ?? throw new InvalidOperationException($"Deficiency {command.DeficiencyId} not found.");

        if (command.RiskLikelihood.HasValue || command.RiskImpact.HasValue)
        {
            deficiency.UpdateRiskAssessment(command.RiskLikelihood, command.RiskImpact, command.UpdatedBy);
            await _auditTrail.LogAsync(deficiency.Id, "RiskUpdated", null, command.UpdatedBy ?? "User", null, ct);
        }

        if (command.ActionPlan != null || command.ExpectedOutcome != null
            || command.RemediationTargetCompletion.HasValue || command.SuccessCriteria != null)
        {
            deficiency.UpdateRemediationPlan(
                command.ActionPlan,
                command.ExpectedOutcome,
                command.RemediationTargetCompletion,
                command.SuccessCriteria,
                command.UpdatedBy);
        }

        if (command.EffectivenessReviewStatus != null
            && Enum.TryParse<Domain.Enums.EffectivenessReviewStatus>(
                command.EffectivenessReviewStatus, ignoreCase: true, out var effectiveness))
        {
            deficiency.SetEffectivenessReviewStatus(effectiveness, command.UpdatedBy);
        }

        if (command.Department != null || command.TargetResolutionDate.HasValue)
        {
            deficiency.UpdateOwnershipMetadata(command.Department, command.TargetResolutionDate, command.UpdatedBy);
        }

        await _db.SaveChangesAsync(ct);
    }
}

public sealed record PatchDeficiencyRemediationCommand(
    Guid DeficiencyId,
    int? RiskLikelihood,
    int? RiskImpact,
    string? Department,
    DateTime? TargetResolutionDate,
    string? ActionPlan,
    string? ExpectedOutcome,
    DateTime? RemediationTargetCompletion,
    string? SuccessCriteria,
    string? EffectivenessReviewStatus,
    string? UpdatedBy);

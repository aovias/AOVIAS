using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Evaluates which deficiency rules are triggered by an obligation's current state.
/// Rules are deterministic; extensibility via RuleConfig.RulesJson is reserved for future phases.
/// </summary>
public class DeficiencyRuleEngine : IDeficiencyRuleEngine
{
    public Task<IReadOnlyList<DeficiencyRuleResult>> GetApplicableRulesAsync(
        OrganizationComplianceObligation obligation,
        CancellationToken ct = default)
    {
        var results = new List<DeficiencyRuleResult>();

        // Rule 1: Implementation deficiency
        if (obligation.ImplementationStatus == ImplementationStatus.NotImplemented)
        {
            results.Add(new DeficiencyRuleResult
            {
                DeficiencyType = DeficiencyType.Implementation,
                Title          = "Implementation Not Completed",
                Description    = $"Obligation '{obligation.Code}' has not been implemented. " +
                                 "ImplementationStatus is NotImplemented.",
            });
        }

        // Rule 2: Evidence deficiency
        if (obligation.EvidenceStatus == EvidenceStatus.Missing)
        {
            results.Add(new DeficiencyRuleResult
            {
                DeficiencyType = DeficiencyType.Evidence,
                Title          = "Evidence Missing",
                Description    = $"Obligation '{obligation.Code}' has no evidence on record. " +
                                 "Supporting SOP or documentation must be uploaded.",
            });
        }

        // Rule 3: Verification deficiency
        if (obligation.VerificationStatus == VerificationStatus.Rejected)
        {
            results.Add(new DeficiencyRuleResult
            {
                DeficiencyType = DeficiencyType.Verification,
                Title          = "Verification Rejected",
                Description    = $"Obligation '{obligation.Code}' verification was rejected. " +
                                 "Address reviewer feedback and resubmit evidence.",
            });
        }

        // Rule 4: Assessment deficiency
        if (obligation.AssessmentStatus == ObligationAssessmentStatus.Failed)
        {
            results.Add(new DeficiencyRuleResult
            {
                DeficiencyType = DeficiencyType.Assessment,
                Title          = "Assessment Failed",
                Description    = $"Obligation '{obligation.Code}' failed its compliance assessment. " +
                                 "A remediation plan is required.",
            });
        }

        // Rule 5: Maturity deficiency — Core obligations that block stage unlock
        if (obligation.MaturityLevel == ObjectiveElementClassification.Core
         && obligation.AssessmentStatus == ObligationAssessmentStatus.Failed
         && (obligation.ImplementationStatus == ImplementationStatus.Unknown
          || obligation.ImplementationStatus == ImplementationStatus.NotStarted
          || obligation.ImplementationStatus == ImplementationStatus.NotImplemented))
        {
            results.Add(new DeficiencyRuleResult
            {
                DeficiencyType = DeficiencyType.Maturity,
                Title          = "Core Stage Unlock Blocked",
                Description    = $"Core obligation '{obligation.Code}' has not been implemented or assessed. " +
                                 "This blocks progression to the Commitment stage.",
            });
        }

        return Task.FromResult<IReadOnlyList<DeficiencyRuleResult>>(results);
    }
}

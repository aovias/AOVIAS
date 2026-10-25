using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Deterministic next-action engine — no AI, pure priority rules.
/// Returns the highest-priority action label and routing key for a given obligation state.
/// </summary>
public class ObligationNextActionService : IObligationNextActionService
{
    public NextActionResult GetNextAction(
        ObligationAssessmentStatus assessment,
        ImplementationStatus implementation,
        ObligationJourneyStatus journeyStatus,
        EvidenceStatus evidence,
        VerificationStatus verification)
    {
        // Priority 1: Not yet assessed
        if (assessment == ObligationAssessmentStatus.NotAssessed)
            return Result(1, "Complete Self-Assessment", NextActionKey.CompleteAssessment, isUrgent: false);

        // Priority 2: Failed and journey not started
        if (assessment == ObligationAssessmentStatus.Failed && journeyStatus == ObligationJourneyStatus.NotStarted)
            return Result(2, "Start Remediation Plan", NextActionKey.StartRemediation, isUrgent: true);

        // Priority 3: Passed or partial, but evidence missing
        if ((assessment == ObligationAssessmentStatus.Passed || assessment == ObligationAssessmentStatus.PartiallyPassed)
            && evidence == EvidenceStatus.Missing)
            return Result(3, "Upload SOP Evidence", NextActionKey.UploadEvidence, isUrgent: true);

        // Priority 4: Evidence partial
        if (evidence == EvidenceStatus.Partial)
            return Result(4, "Complete Evidence Upload", NextActionKey.UploadEvidence, isUrgent: false);

        // Priority 5: Evidence submitted, verification not started or pending
        if (evidence == EvidenceStatus.Submitted &&
            (verification == VerificationStatus.NotVerified || verification == VerificationStatus.Pending))
            return Result(5, "Request Verification", NextActionKey.RequestVerification, isUrgent: false);

        // Priority 6: Verification rejected
        if (verification == VerificationStatus.Rejected)
            return Result(6, "Address Verification Feedback", NextActionKey.AddressVerificationFeedback, isUrgent: true);

        // Priority 7: Journey in progress
        if (journeyStatus == ObligationJourneyStatus.InProgress)
            return Result(7, "Continue Execution Wizard", NextActionKey.ContinueJourney, isUrgent: false);

        // Priority 8: Journey not started, implementation not begun
        if (journeyStatus == ObligationJourneyStatus.NotStarted &&
            (implementation == ImplementationStatus.Unknown || implementation == ImplementationStatus.NotStarted))
            return Result(8, "Begin Implementation", NextActionKey.BeginImplementation, isUrgent: false);

        // Priority 9: Journey completed, evidence still missing
        if (journeyStatus == ObligationJourneyStatus.Completed && evidence == EvidenceStatus.Missing)
            return Result(9, "Upload Evidence Post-Execution", NextActionKey.UploadEvidence, isUrgent: false);

        // Priority 10: All optimal
        return Result(10, "Maintain Compliance", NextActionKey.MaintainCompliance, isUrgent: false);
    }

    private static NextActionResult Result(int priority, string label, NextActionKey key, bool isUrgent) =>
        new() { Priority = priority, Label = label, ActionKey = key, IsUrgent = isUrgent };
}

public interface IObligationNextActionService
{
    NextActionResult GetNextAction(
        ObligationAssessmentStatus assessment,
        ImplementationStatus implementation,
        ObligationJourneyStatus journeyStatus,
        EvidenceStatus evidence,
        VerificationStatus verification);
}

public class NextActionResult
{
    public string Label { get; set; } = string.Empty;
    public int Priority { get; set; }
    public NextActionKey ActionKey { get; set; }
    public bool IsUrgent { get; set; }
}

public enum NextActionKey
{
    CompleteAssessment,
    StartRemediation,
    UploadEvidence,
    RequestVerification,
    AddressVerificationFeedback,
    ContinueJourney,
    BeginImplementation,
    MaintainCompliance
}

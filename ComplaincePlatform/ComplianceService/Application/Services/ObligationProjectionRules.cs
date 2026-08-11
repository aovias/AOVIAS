using ComplianceService.Domain;

using ComplianceService.Domain.Enums;



namespace ComplianceService.Application.Services;



/// <summary>

/// Deterministic projection rules from <see cref="ObjectiveElementEvidence"/> links

/// to obligation-level <see cref="EvidenceStatus"/> and <see cref="VerificationStatus"/>.

/// </summary>

public static class ObligationProjectionRules

{

    public static EvidenceStatus ComputeEvidenceStatus(IReadOnlyList<ObjectiveElementEvidence> links)

    {

        if (links.Count == 0)

        {

            return EvidenceStatus.Missing;

        }



        if (links.Any(l => IsApproved(l.VerificationStatus)))

        {

            return EvidenceStatus.Verified;

        }



        if (links.All(l => IsAwaitingReview(l.VerificationStatus)))

        {

            return EvidenceStatus.Submitted;

        }



        return EvidenceStatus.Partial;

    }



    public static VerificationStatus ComputeVerificationStatus(IReadOnlyList<ObjectiveElementEvidence> links)

    {

        if (links.Count == 0)

        {

            return VerificationStatus.NotVerified;

        }



        if (links.Any(l => IsApproved(l.VerificationStatus)))

        {

            return VerificationStatus.Verified;

        }



        if (links.Any(l => IsAwaitingReview(l.VerificationStatus)))

        {

            return VerificationStatus.Pending;

        }



        if (links.Any(l => l.VerificationStatus == EvidenceVerificationStatus.Rejected))

        {

            return VerificationStatus.Rejected;

        }



        if (links.Any(l => l.VerificationStatus == EvidenceVerificationStatus.NeedsMoreEvidence))

        {

            return VerificationStatus.Pending;

        }



        return VerificationStatus.NotVerified;

    }



    public static bool IsApproved(EvidenceVerificationStatus status) =>

        status == EvidenceVerificationStatus.Verified;



    public static bool IsAwaitingReview(EvidenceVerificationStatus status) =>

        status is EvidenceVerificationStatus.Pending

            or EvidenceVerificationStatus.Submitted

            or EvidenceVerificationStatus.Draft;



    public static bool BlocksTaskCompletion(EvidenceVerificationStatus status) =>

        IsAwaitingReview(status)

        || status == EvidenceVerificationStatus.Rejected

        || status == EvidenceVerificationStatus.NeedsMoreEvidence;



    public static (ObligationJourneyStatus JourneyStatus, decimal CompletionPercentage) ComputeJourneyProgress(

        ObjectiveJourney? objectiveJourney)

    {

        if (objectiveJourney == null)

        {

            return (ObligationJourneyStatus.NotStarted, 0m);

        }



        return objectiveJourney.Status switch

        {

            ObjectiveJourneyStatus.Completed => (ObligationJourneyStatus.Completed, 100m),

            ObjectiveJourneyStatus.InProgress => (

                ObligationJourneyStatus.InProgress,

                Math.Min(90m, 25m + objectiveJourney.CurrentStepIndex * 10m)),

            ObjectiveJourneyStatus.Draft => (

                ObligationJourneyStatus.InProgress,

                Math.Min(50m, 10m + objectiveJourney.CurrentStepIndex * 5m)),

            _ => (ObligationJourneyStatus.NotStarted, 0m)

        };

    }



    public static ImplementationStatus ComputeImplementationStatus(

        ImplementationStatus current,

        ObjectiveJourney? objectiveJourney,

        bool hasCompletedTask)

    {

        if (current is ImplementationStatus.Implemented or ImplementationStatus.PartiallyImplemented)

        {

            return current;

        }



        if (current == ImplementationStatus.NotImplemented)

        {

            if (objectiveJourney?.Status == ObjectiveJourneyStatus.Completed || hasCompletedTask)

            {

                return ImplementationStatus.PartiallyImplemented;

            }



            return current;

        }



        if (objectiveJourney?.Status == ObjectiveJourneyStatus.Completed)

        {

            return ImplementationStatus.PartiallyImplemented;

        }



        if (hasCompletedTask)

        {

            return ImplementationStatus.PartiallyImplemented;

        }



        if (objectiveJourney?.Status is ObjectiveJourneyStatus.InProgress or ObjectiveJourneyStatus.Draft)

        {

            return current is ImplementationStatus.Unknown or ImplementationStatus.NotStarted

                ? ImplementationStatus.NotStarted

                : current;

        }



        return current is ImplementationStatus.Unknown

            ? ImplementationStatus.NotStarted

            : current;

    }

}



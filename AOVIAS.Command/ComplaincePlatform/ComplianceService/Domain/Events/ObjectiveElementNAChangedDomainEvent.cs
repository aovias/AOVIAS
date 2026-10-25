namespace ComplianceService.Domain.Events
{
    public class ObjectiveElementNAChangedDomainEvent
    {
        public Guid ObjectiveElementId { get; }
        public Guid JourneyId { get; }
        public bool IsApplicable { get; }
        public string? NaReason { get; }
        public Guid? NaApprovedBy { get; }
        public DateTime ChangedAt { get; }

        public ObjectiveElementNAChangedDomainEvent(Guid objectiveElementId, Guid journeyId, bool isApplicable, string? naReason, Guid? naApprovedBy, DateTime changedAt)
        {
            ObjectiveElementId = objectiveElementId;
            JourneyId = journeyId;
            IsApplicable = isApplicable;
            NaReason = naReason;
            NaApprovedBy = naApprovedBy;
            ChangedAt = changedAt;
        }
    }
}


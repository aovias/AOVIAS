namespace ComplianceService.Domain.Events
{
    public class ObjectiveElementScoredDomainEvent
    {
        public Guid ObjectiveElementId { get; }
        public Guid JourneyId { get; }
        public Guid AssessorId { get; }
        public int Score { get; }
        public DateTime AssessedAt { get; }

        public ObjectiveElementScoredDomainEvent(Guid objectiveElementId, Guid journeyId, Guid assessorId, int score, DateTime assessedAt)
        {
            ObjectiveElementId = objectiveElementId;
            JourneyId = journeyId;
            AssessorId = assessorId;
            Score = score;
            AssessedAt = assessedAt;
        }
    }
}


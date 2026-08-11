namespace ComplianceService.Domain
{
    /// <summary>
    /// Domain event indicating that compliance scoring/snapshot inputs for a
    /// hospital+framework have changed and projections should be refreshed.
    /// </summary>
    public record ComplianceScoreUpdatedDomainEvent(
        Guid HospitalId,
        Guid FrameworkId
    ) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}


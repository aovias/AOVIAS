namespace ComplianceService.Domain
{
    /// <summary>
    /// [DEPRECATED] Use ComplianceScoreUpdatedDomainEvent instead.
    /// Kept only for backward compatibility with historical payloads.
    /// </summary>
    [Obsolete("Use ComplianceScoreUpdatedDomainEvent instead.")]
    public record ComplianceUpdatedDomainEvent(
        Guid HospitalId,
        Guid FrameworkId
    ) : IDomainEvent
    {
        public DateTime OccurredOn { get; } = DateTime.UtcNow;
    }
}

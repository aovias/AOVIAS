namespace ComplianceService.Domain
{
    public interface IDomainEvent
    {
        DateTime OccurredOn { get; }
    }
}

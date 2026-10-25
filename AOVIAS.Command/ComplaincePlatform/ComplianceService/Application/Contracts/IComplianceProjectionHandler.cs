using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

public interface IComplianceProjectionHandler
{
    Task HandleAsync(ComplianceScoreUpdatedDomainEvent domainEvent, CancellationToken ct);
}

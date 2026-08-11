namespace ComplianceService.Application.Commands;

/// <summary>
/// Triggers deficiency evaluation for a single obligation after it has been persisted.
/// </summary>
public record EvaluateObligationDeficienciesCommand(
    Guid ObligationId,
    Guid HospitalId,
    Guid FrameworkId,
    string Source,
    bool FlushOutbox = true);

namespace ComplianceService.Application.Commands;

public record PatchDeficiencyStatusCommand(
    Guid DeficiencyId,
    string NewStatus,
    string? UpdatedBy);

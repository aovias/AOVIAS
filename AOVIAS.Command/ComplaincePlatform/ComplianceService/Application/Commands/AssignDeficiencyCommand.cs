namespace ComplianceService.Application.Commands;

public record AssignDeficiencyCommand(
    Guid DeficiencyId,
    string AssignedTo,
    DateTime? DueDate,
    string? UpdatedBy);

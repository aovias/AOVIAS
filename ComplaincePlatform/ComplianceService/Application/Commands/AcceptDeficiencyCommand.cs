namespace ComplianceService.Application.Commands;

public record AcceptDeficiencyCommand(
    Guid DeficiencyId,
    string AcceptedBy);

public record RejectDeficiencyOwnershipCommand(
    Guid DeficiencyId,
    string RejectedBy,
    string? Reason);

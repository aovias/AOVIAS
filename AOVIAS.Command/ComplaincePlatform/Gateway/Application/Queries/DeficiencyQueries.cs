using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Application.Queries;

public record GetDeficienciesQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string? Status,
    string? Severity,
    int Page,
    int PageSize,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficiencyByIdQuery(
    Guid DeficiencyId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficiencySummaryQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficienciesByStageQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyStatusCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record AssignDeficiencyCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record AcceptDeficiencyGatewayCommand(
    Guid DeficiencyId,
    object? Body,
    string ApiVersion) : IRequest<IActionResult>;

public record RejectDeficiencyOwnershipGatewayCommand(
    Guid DeficiencyId,
    object? Body,
    string ApiVersion) : IRequest<IActionResult>;

public record BootstrapDeficienciesFromOnboardingCommand(
    Guid OrganizationId,
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficiencyCommandCenterDashboardQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficiencyWorkspaceQuery(
    Guid HospitalId,
    Guid FrameworkId,
    string? Status,
    string? Severity,
    string? MaturityLevel,
    string? Search,
    Guid? ObjectiveElementId,
    string? SortBy,
    bool SortDesc,
    int Page,
    int PageSize,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficiencyWorkspaceDetailQuery(
    Guid DeficiencyId,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyRootCauseCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyRcaCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyRemediationCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyCorrectiveActionCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyPreventiveActionCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record PatchDeficiencyEffectivenessReviewCommand(
    Guid DeficiencyId,
    object Body,
    string ApiVersion) : IRequest<IActionResult>;

public record GetDeficiencyCapaQuery(
    Guid DeficiencyId,
    string ApiVersion) : IRequest<IActionResult>;

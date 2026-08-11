using WorkforceService.Application.Commands;
using WorkforceService.Application.DTOs;
using MediatR;

namespace WorkforceService.Application.Commands;

public record CreateEmploymentCommand(CreateEmploymentRequest Request) : IRequest<EmploymentDto>;
public record UpdateEmploymentCommand(Guid Id, UpdateEmploymentRequest Request) : IRequest<EmploymentDto?>;
public record DeleteEmploymentCommand(Guid Id, Guid? ActorUserId) : IRequest<bool>;
public record AssignPositionCommand(Guid EmploymentId, AssignPositionRequest Request) : IRequest<PositionAssignmentDto?>;
public record AssignDepartmentCommand(Guid EmploymentId, AssignDepartmentRequest Request) : IRequest<DepartmentAssignmentDto?>;
public record AssignManagerCommand(Guid EmploymentId, AssignManagerRequest Request) : IRequest<ReportingRelationshipDto?>;
public record TransferEmploymentCommand(Guid EmploymentId, TransferEmploymentRequest Request) : IRequest<EmploymentDto?>;
public record PromoteEmploymentCommand(Guid EmploymentId, PromoteEmploymentRequest Request) : IRequest<EmploymentDto?>;

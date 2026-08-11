using WorkforceService.Application.DTOs;
using MediatR;

namespace WorkforceService.Application.Queries;

public record GetEmploymentByIdQuery(Guid Id) : IRequest<EmploymentDto?>;
public record ListEmploymentsQuery(int Page, int PageSize, Guid? OrganizationId, Guid? HospitalId, Guid? StatusValueId) : IRequest<PagedResultDto<EmploymentListItemDto>>;
public record SearchEmploymentsQuery(string Query, int Page, int PageSize) : IRequest<PagedResultDto<EmploymentListItemDto>>;
public record GetEmploymentHistoryQuery(Guid EmploymentId) : IRequest<IReadOnlyList<EmploymentHistoryDto>>;
public record GetEmploymentTimelineQuery(Guid EmploymentId) : IRequest<IReadOnlyList<WorkforceTimelineDto>>;
public record GetWorkforceLookupsQuery(string CategoryCode) : IRequest<IReadOnlyList<LookupValueDto>>;
public record GetPositionsQuery(Guid OrganizationId) : IRequest<IReadOnlyList<PositionDto>>;
public record GetOrgChartQuery(Guid OrganizationId, Guid? HospitalId, Guid? RootEmploymentId) : IRequest<OrgChartNodeDto?>;
public record GetManagerTeamQuery(Guid ManagerEmploymentId) : IRequest<IReadOnlyList<EmploymentListItemDto>>;

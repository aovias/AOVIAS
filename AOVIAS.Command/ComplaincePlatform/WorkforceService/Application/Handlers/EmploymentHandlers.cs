using WorkforceService.Application.Commands;
using WorkforceService.Application.Contracts;
using WorkforceService.Application.DTOs;
using WorkforceService.Application.Queries;
using MediatR;

namespace WorkforceService.Application.Handlers;

public class CreateEmploymentCommandHandler : IRequestHandler<CreateEmploymentCommand, EmploymentDto>
{
    private readonly IEmploymentAppService _service;
    public CreateEmploymentCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<EmploymentDto> Handle(CreateEmploymentCommand request, CancellationToken cancellationToken) => _service.CreateAsync(request.Request, cancellationToken);
}

public class UpdateEmploymentCommandHandler : IRequestHandler<UpdateEmploymentCommand, EmploymentDto?>
{
    private readonly IEmploymentAppService _service;
    public UpdateEmploymentCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<EmploymentDto?> Handle(UpdateEmploymentCommand request, CancellationToken cancellationToken) => _service.UpdateAsync(request.Id, request.Request, cancellationToken);
}

public class DeleteEmploymentCommandHandler : IRequestHandler<DeleteEmploymentCommand, bool>
{
    private readonly IEmploymentAppService _service;
    public DeleteEmploymentCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<bool> Handle(DeleteEmploymentCommand request, CancellationToken cancellationToken) => _service.DeleteAsync(request.Id, request.ActorUserId, cancellationToken);
}

public class GetEmploymentByIdQueryHandler : IRequestHandler<GetEmploymentByIdQuery, EmploymentDto?>
{
    private readonly IEmploymentAppService _service;
    public GetEmploymentByIdQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<EmploymentDto?> Handle(GetEmploymentByIdQuery request, CancellationToken cancellationToken) => _service.GetByIdAsync(request.Id, cancellationToken);
}

public class ListEmploymentsQueryHandler : IRequestHandler<ListEmploymentsQuery, PagedResultDto<EmploymentListItemDto>>
{
    private readonly IEmploymentAppService _service;
    public ListEmploymentsQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<PagedResultDto<EmploymentListItemDto>> Handle(ListEmploymentsQuery request, CancellationToken cancellationToken) => _service.ListAsync(request.Page, request.PageSize, request.OrganizationId, request.HospitalId, request.StatusValueId, cancellationToken);
}

public class SearchEmploymentsQueryHandler : IRequestHandler<SearchEmploymentsQuery, PagedResultDto<EmploymentListItemDto>>
{
    private readonly IEmploymentAppService _service;
    public SearchEmploymentsQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<PagedResultDto<EmploymentListItemDto>> Handle(SearchEmploymentsQuery request, CancellationToken cancellationToken) => _service.SearchAsync(request.Query, request.Page, request.PageSize, cancellationToken);
}

public class AssignPositionCommandHandler : IRequestHandler<AssignPositionCommand, PositionAssignmentDto?>
{
    private readonly IEmploymentAppService _service;
    public AssignPositionCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<PositionAssignmentDto?> Handle(AssignPositionCommand request, CancellationToken cancellationToken) => _service.AssignPositionAsync(request.EmploymentId, request.Request, cancellationToken);
}

public class AssignDepartmentCommandHandler : IRequestHandler<AssignDepartmentCommand, DepartmentAssignmentDto?>
{
    private readonly IEmploymentAppService _service;
    public AssignDepartmentCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<DepartmentAssignmentDto?> Handle(AssignDepartmentCommand request, CancellationToken cancellationToken) => _service.AssignDepartmentAsync(request.EmploymentId, request.Request, cancellationToken);
}

public class AssignManagerCommandHandler : IRequestHandler<AssignManagerCommand, ReportingRelationshipDto?>
{
    private readonly IEmploymentAppService _service;
    public AssignManagerCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<ReportingRelationshipDto?> Handle(AssignManagerCommand request, CancellationToken cancellationToken) => _service.AssignManagerAsync(request.EmploymentId, request.Request, cancellationToken);
}

public class TransferEmploymentCommandHandler : IRequestHandler<TransferEmploymentCommand, EmploymentDto?>
{
    private readonly IEmploymentAppService _service;
    public TransferEmploymentCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<EmploymentDto?> Handle(TransferEmploymentCommand request, CancellationToken cancellationToken) => _service.TransferAsync(request.EmploymentId, request.Request, cancellationToken);
}

public class PromoteEmploymentCommandHandler : IRequestHandler<PromoteEmploymentCommand, EmploymentDto?>
{
    private readonly IEmploymentAppService _service;
    public PromoteEmploymentCommandHandler(IEmploymentAppService service) => _service = service;
    public Task<EmploymentDto?> Handle(PromoteEmploymentCommand request, CancellationToken cancellationToken) => _service.PromoteAsync(request.EmploymentId, request.Request, cancellationToken);
}

public class GetEmploymentHistoryQueryHandler : IRequestHandler<GetEmploymentHistoryQuery, IReadOnlyList<EmploymentHistoryDto>>
{
    private readonly IEmploymentAppService _service;
    public GetEmploymentHistoryQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<IReadOnlyList<EmploymentHistoryDto>> Handle(GetEmploymentHistoryQuery request, CancellationToken cancellationToken) => _service.GetHistoryAsync(request.EmploymentId, cancellationToken);
}

public class GetEmploymentTimelineQueryHandler : IRequestHandler<GetEmploymentTimelineQuery, IReadOnlyList<WorkforceTimelineDto>>
{
    private readonly IEmploymentAppService _service;
    public GetEmploymentTimelineQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<IReadOnlyList<WorkforceTimelineDto>> Handle(GetEmploymentTimelineQuery request, CancellationToken cancellationToken) => _service.GetTimelineAsync(request.EmploymentId, cancellationToken);
}

public class GetWorkforceLookupsQueryHandler : IRequestHandler<GetWorkforceLookupsQuery, IReadOnlyList<LookupValueDto>>
{
    private readonly IEmploymentAppService _service;
    public GetWorkforceLookupsQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<IReadOnlyList<LookupValueDto>> Handle(GetWorkforceLookupsQuery request, CancellationToken cancellationToken) => _service.GetLookupsAsync(request.CategoryCode, cancellationToken);
}

public class GetPositionsQueryHandler : IRequestHandler<GetPositionsQuery, IReadOnlyList<PositionDto>>
{
    private readonly IEmploymentAppService _service;
    public GetPositionsQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<IReadOnlyList<PositionDto>> Handle(GetPositionsQuery request, CancellationToken cancellationToken) => _service.GetPositionsAsync(request.OrganizationId, cancellationToken);
}

public class GetOrgChartQueryHandler : IRequestHandler<GetOrgChartQuery, OrgChartNodeDto?>
{
    private readonly IEmploymentAppService _service;
    public GetOrgChartQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<OrgChartNodeDto?> Handle(GetOrgChartQuery request, CancellationToken cancellationToken) => _service.GetOrgChartAsync(request.OrganizationId, request.HospitalId, request.RootEmploymentId, cancellationToken);
}

public class GetManagerTeamQueryHandler : IRequestHandler<GetManagerTeamQuery, IReadOnlyList<EmploymentListItemDto>>
{
    private readonly IEmploymentAppService _service;
    public GetManagerTeamQueryHandler(IEmploymentAppService service) => _service = service;
    public Task<IReadOnlyList<EmploymentListItemDto>> Handle(GetManagerTeamQuery request, CancellationToken cancellationToken) => _service.GetManagerTeamAsync(request.ManagerEmploymentId, cancellationToken);
}

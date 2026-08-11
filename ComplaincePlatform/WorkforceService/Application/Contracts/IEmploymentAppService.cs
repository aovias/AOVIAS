using Shared.Infrastructure.DTO;
using WorkforceService.Application.DTOs;

namespace WorkforceService.Application.Contracts;

public interface IEmploymentAppService
{
    Task<EmploymentDto> CreateAsync(CreateEmploymentRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationUpsertResponse> UpsertFromIntegrationAsync(IntegrationUpsertRequest request, CancellationToken cancellationToken = default);
    Task<bool> RollbackIntegrationAsync(Guid organizationId, Guid employmentId, CancellationToken cancellationToken = default);
    Task<EmploymentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<EmploymentListItemDto>> ListAsync(int page, int pageSize, Guid? organizationId, Guid? hospitalId, Guid? statusValueId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<EmploymentListItemDto>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<EmploymentDto?> UpdateAsync(Guid id, UpdateEmploymentRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default);
    Task<PositionAssignmentDto?> AssignPositionAsync(Guid employmentId, AssignPositionRequest request, CancellationToken cancellationToken = default);
    Task<DepartmentAssignmentDto?> AssignDepartmentAsync(Guid employmentId, AssignDepartmentRequest request, CancellationToken cancellationToken = default);
    Task<ReportingRelationshipDto?> AssignManagerAsync(Guid employmentId, AssignManagerRequest request, CancellationToken cancellationToken = default);
    Task<EmploymentDto?> TransferAsync(Guid employmentId, TransferEmploymentRequest request, CancellationToken cancellationToken = default);
    Task<EmploymentDto?> PromoteAsync(Guid employmentId, PromoteEmploymentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmploymentHistoryDto>> GetHistoryAsync(Guid employmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkforceTimelineDto>> GetTimelineAsync(Guid employmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupValueDto>> GetLookupsAsync(string categoryCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PositionDto>> GetPositionsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrgChartNodeDto?> GetOrgChartAsync(Guid organizationId, Guid? hospitalId, Guid? rootEmploymentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmploymentListItemDto>> GetManagerTeamAsync(Guid managerEmploymentId, CancellationToken cancellationToken = default);
}

public interface IWorkforceAuditService
{
    Task RecordAsync(Guid employmentId, string eventCode, string action, Guid? actorUserId, string? oldValue, string? newValue, string? correlationId, CancellationToken cancellationToken = default);
    Task RecordTimelineAsync(Guid employmentId, string eventType, string summary, Guid? actorUserId, string? oldJson, string? newJson, string? correlationId, CancellationToken cancellationToken = default);
    Task RecordHistoryAsync(Guid employmentId, string eventType, string? reason, string? oldJson, string? newJson, Guid? actorUserId, CancellationToken cancellationToken = default);
}

public interface IPersonServiceClient
{
    Task<PersonRefDto?> GetPersonAsync(Guid personId, CancellationToken cancellationToken = default);
}

public interface IOrganizationServiceClient
{
    Task<bool> ValidateHospitalAsync(Guid hospitalId, CancellationToken cancellationToken = default);
}

public class PersonRefDto
{
    public Guid PersonId { get; set; }
    public string SearchName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

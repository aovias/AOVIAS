using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IIncidentQueryService
{
    Task<IncidentDashboardDto> GetDashboardAsync(Guid organizationId, CancellationToken ct = default);
    Task<IncidentListDto> GetIncidentsAsync(
        Guid organizationId,
        string? status,
        string? severity,
        string? category,
        string? department,
        string? incidentType,
        string? quickFilter,
        string? search,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<IncidentDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default);
    Task<IncidentAnalyticsDto> GetAnalyticsAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<PatientFeedbackDto>> GetPatientFeedbackAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<LessonLearnedDto>> GetLessonsLearnedAsync(Guid organizationId, string? search, CancellationToken ct = default);
}

public interface IIncidentCommandService
{
    Task<IncidentDetailDto> CreateAsync(Guid organizationId, Guid? hospitalId, Guid userId, CreateIncidentRequestDto request, CancellationToken ct = default);
    Task<IncidentDetailDto> PatchAsync(Guid organizationId, Guid id, Guid userId, PatchIncidentRequestDto request, CancellationToken ct = default);
    Task<IncidentInvestigationDto> SaveInvestigationAsync(Guid organizationId, Guid userId, SaveInvestigationRequestDto request, CancellationToken ct = default);
    Task<IncidentRcaDto> SaveRcaAsync(Guid organizationId, Guid userId, SaveRcaRequestDto request, CancellationToken ct = default);
    Task<PatientFeedbackDto> CreatePatientFeedbackAsync(Guid organizationId, Guid? hospitalId, CreatePatientFeedbackRequestDto request, CancellationToken ct = default);
    Task<LessonLearnedDto> CreateLessonLearnedAsync(Guid organizationId, Guid? hospitalId, Guid userId, CreateLessonLearnedRequestDto request, CancellationToken ct = default);
}

public interface IIncidentPromotionService
{
    Task<PromoteIncidentResultDto> PromoteToFindingAsync(Guid organizationId, Guid? hospitalId, Guid userId, PromoteIncidentFindingRequestDto request, CancellationToken ct = default);
    Task<PromoteIncidentResultDto> PromoteToDeficiencyAsync(Guid organizationId, Guid? hospitalId, Guid userId, PromoteIncidentDeficiencyRequestDto request, CancellationToken ct = default);
}

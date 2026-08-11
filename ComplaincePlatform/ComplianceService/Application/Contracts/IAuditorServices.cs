using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IAuditorWorkspaceQueryService
{
    Task<AuditorDashboardDto> GetDashboardAsync(Guid organizationId, Guid hospitalId, CancellationToken ct = default);
    Task<IReadOnlyList<AuditorSessionSummaryDto>> GetSessionsAsync(Guid organizationId, CancellationToken ct = default);
    Task<AuditSessionDetailDto?> GetSessionByIdAsync(Guid organizationId, Guid sessionId, CancellationToken ct = default);
    Task<AuditSessionDetailDto> CreateSessionAsync(Guid organizationId, Guid hospitalId, Guid userId, CreateAuditSessionRequest request, CancellationToken ct = default);
    Task<AuditSessionDetailDto> CloseSessionAsync(Guid organizationId, Guid sessionId, Guid userId, CloseAuditSessionRequest request, CancellationToken ct = default);
    Task<AuditObservationListItemDto> CreateObservationAsync(Guid organizationId, Guid userId, string? userName, CreateAuditObservationRequest request, CancellationToken ct = default);
    Task<AuditCommentDto> CreateCommentAsync(Guid organizationId, Guid userId, string? userName, bool isAuditor, CreateAuditCommentRequest request, CancellationToken ct = default);
    Task<AuditorAssessmentContextDto?> GetAssessmentContextAsync(Guid organizationId, Guid hospitalId, Guid? frameworkId, CancellationToken ct = default);
}

public interface IAuditorTraceabilityService
{
    Task<EvidenceTraceabilityDto?> GetTraceabilityAsync(Guid organizationId, Guid hospitalId, Guid objectiveElementId, CancellationToken ct = default);
}

public interface IAuditorReportService
{
    Task<AuditorSessionReportDto?> GenerateReportAsync(Guid organizationId, Guid hospitalId, Guid sessionId, CancellationToken ct = default);
}

public interface IAuditScheduleService
{
    Task<IReadOnlyList<ScheduledAuditDto>> ListAsync(Guid organizationId, AuditCalendarQuery query, CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledAuditDto>> GetCalendarAsync(Guid organizationId, AuditCalendarQuery query, CancellationToken ct = default);
    Task<ScheduledAuditDto> CreateAsync(Guid organizationId, Guid hospitalId, CreateScheduledAuditRequest request, CancellationToken ct = default);
}

public interface IAuditGenerateFacadeService
{
    Task<GenerateAuditFacadeResponse> GenerateAsync(GenerateAuditFacadeRequest request, CancellationToken ct = default);
    Task<GenerateAuditFacadeResponse?> GetAsync(Guid sessionId, CancellationToken ct = default);
}

public interface IAuditSimulationAppService
{
    Task<AuditSimulationResultDto> SimulateAsync(Guid organizationId, Guid hospitalId, Guid? frameworkId, CancellationToken ct = default);
}

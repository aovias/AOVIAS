using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

public interface IPersonaExperienceService
{
    Task<PersonaWorkspaceDto> GetWorkspaceAsync(
        CopilotPersona persona,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default);

    IReadOnlyList<PersonaExperienceDefinitionDto> GetExperienceCatalog();
}

public interface ICopilotActivityService
{
    Task RecordAsync(
        Guid organizationId,
        string eventType,
        string? persona,
        Guid? actorId = null,
        string? actorEmail = null,
        string? entityType = null,
        Guid? entityId = null,
        string? notes = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<CopilotActivityEventDto>> GetFeedAsync(
        Guid organizationId,
        int limit = 50,
        CancellationToken ct = default);

    Task<CopilotUsageMetricsDto> GetMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default);
}

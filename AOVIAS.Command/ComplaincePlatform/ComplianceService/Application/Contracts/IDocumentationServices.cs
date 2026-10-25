using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Contracts;

public interface IDocumentationQueryService
{
    Task<DocumentationDashboardDto> GetDashboardAsync(Guid organizationId, Guid hospitalId, CancellationToken ct = default);
    Task<IReadOnlyList<ControlledDocumentListItemDto>> GetDocumentsAsync(
        Guid organizationId,
        ControlledDocumentType? type = null,
        ControlledDocumentStatus? status = null,
        string? search = null,
        CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> GetDocumentByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentVersionSummaryDto>> GetVersionHistoryAsync(Guid organizationId, Guid documentId, CancellationToken ct = default);
    Task<VersionCompareDto?> CompareVersionsAsync(Guid organizationId, Guid documentId, Guid? versionId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentAcknowledgementListItemDto>> GetAcknowledgementsAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<ReviewCalendarItemDto>> GetReviewCalendarAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentTemplateDto>> GetTemplatesAsync(Guid organizationId, CancellationToken ct = default);
    Task<DocumentationAnalyticsDto> GetAnalyticsAsync(Guid organizationId, Guid hospitalId, CancellationToken ct = default);
}

public interface IDocumentationCommandService
{
    Task<ControlledDocumentDetailDto> CreateDocumentAsync(
        Guid organizationId, Guid hospitalId, Guid userId, string? userName,
        CreateControlledDocumentRequest request, CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> UpdateDocumentAsync(
        Guid organizationId, Guid id, Guid userId, string? userName,
        UpdateControlledDocumentRequest request, CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> CreateVersionAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        CreateDocumentVersionRequest request, CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> TransitionStatusAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        TransitionDocumentStatusRequest request, CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> ApproveAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        ApproveDocumentRequest request, CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> PublishAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        PublishDocumentRequest request, CancellationToken ct = default);
    Task<DocumentAcknowledgementListItemDto?> AcknowledgeAsync(
        Guid organizationId, Guid userId, AcknowledgeDocumentRequest request, CancellationToken ct = default);
    Task<ControlledDocumentDetailDto?> CompleteReviewAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        CompleteDocumentReviewRequest request, CancellationToken ct = default);
}

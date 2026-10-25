using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IDocumentationAuthoringService
{
    Task<AuthoringSessionDto> CreateAuthoringAsync(
        Guid organizationId, Guid hospitalId, Guid userId, string? userName,
        CreateAuthoringRequest request, CancellationToken ct = default);

    Task<AuthoringSessionDto?> GetAuthoringAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default);

    Task<AuthoringSessionDto?> PatchAuthoringAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        PatchAuthoringRequest request, CancellationToken ct = default);

    Task<StructuredCompareResultDto?> CompareStructuredAsync(
        Guid organizationId, StructuredCompareRequest request, CancellationToken ct = default);

    Task<AuthoringAiDraftResponse> DraftAiAsync(
        Guid organizationId, Guid userId, AuthoringAiDraftRequest request, CancellationToken ct = default);

    Task<AuthoringCommentDto?> AddCommentAsync(
        Guid organizationId, Guid userId, string? userName,
        CreateAuthoringCommentRequest request, CancellationToken ct = default);

    Task<CrossReferenceDto?> AddCrossReferenceAsync(
        Guid organizationId, AddCrossReferenceRequest request, CancellationToken ct = default);

    Task<DocumentExportResultDto?> ExportAsync(
        Guid organizationId, DocumentExportRequest request, CancellationToken ct = default);
}

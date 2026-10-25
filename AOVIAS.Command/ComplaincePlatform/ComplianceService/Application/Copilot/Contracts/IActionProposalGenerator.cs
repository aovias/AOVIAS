using ComplianceService.Application.Copilot.DTOs;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;

namespace ComplianceService.Application.Copilot.Contracts;

/// <summary>
/// Converts recommendations and drafts into <see cref="CopilotActionProposal"/> objects.
/// Does NOT approve or execute proposals — generation only.
/// </summary>
public interface IActionProposalGenerator
{
    /// <summary>
    /// Creates an action proposal from a Copilot recommendation.
    /// The proposal type is automatically derived from the recommendation category.
    /// </summary>
    Task<CopilotActionProposalDto> GenerateFromRecommendationAsync(
        Guid recommendationId,
        Guid organizationId,
        GenerateProposalRequest? request = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates an action proposal from an AI-generated draft.
    /// The proposal type is derived from the draft type.
    /// </summary>
    Task<CopilotActionProposalDto> GenerateFromDraftAsync(
        Guid draftId,
        Guid organizationId,
        GenerateProposalRequest? request = null,
        CancellationToken ct = default);

    /// <summary>
    /// Creates a custom action proposal with an explicit proposal type and payload.
    /// </summary>
    Task<CopilotActionProposalDto> GenerateCustomAsync(
        CreateProposalRequest request,
        CancellationToken ct = default);
}

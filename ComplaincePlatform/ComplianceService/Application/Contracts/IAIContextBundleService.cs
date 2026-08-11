using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Contracts;

public interface IAIContextBundleService
{
    Task<AIContextBundleDto> BuildAsync(AIContextBundleRequestDto request, CancellationToken cancellationToken);
    Task<AIContextVersionValidationResponseDto> ValidateVersionAsync(AIContextVersionValidationRequestDto request, CancellationToken cancellationToken);
    Task<AICommandPreconditionResponseDto> ValidateCommandPreconditionsAsync(AICommandPreconditionRequestDto request, CancellationToken cancellationToken);
}

using AIOrchestratorService.Domain;
using Shared.Infrastructure.DTO;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Service that orchestrates the AI-assisted draft generation flow.
/// </summary>
public class DraftGenerationService : IDraftGenerationService
{
    private readonly ILLMAdapter _llmAdapter;
    private readonly IDraftArtifactRepository _repository;
    private readonly IDraftLanguageAuditor _languageAuditor;
    private readonly ILogger<DraftGenerationService> _logger;

    public DraftGenerationService(
        ILLMAdapter llmAdapter,
        IDraftArtifactRepository repository,
        IDraftLanguageAuditor languageAuditor,
        ILogger<DraftGenerationService> logger)
    {
        _llmAdapter = llmAdapter;
        _repository = repository;
        _languageAuditor = languageAuditor;
        _logger = logger;
    }

    public async Task<DraftArtifact> GenerateDraftAsync(
        DraftingRequest request,
        string generatedBy,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(generatedBy))
        {
            throw new ArgumentException("GeneratedBy cannot be null or empty.", nameof(generatedBy));
        }

        try
        {
            _logger.LogInformation(
                "Starting draft generation for DraftType: {DraftType}, SourceEntityId: {SourceEntityId}",
                request.DraftType,
                request.SourceEntityId);

            // Step 3: LLMAdapter generates draft content
            var llmResult = await _llmAdapter.GenerateDraftAsync(request, cancellationToken);

            if (!llmResult.IsSuccess)
            {
                _logger.LogError(
                    "LLM adapter failed to generate draft: {ErrorMessage}",
                    llmResult.ErrorMessage);
                throw new InvalidOperationException(
                    $"Failed to generate draft: {llmResult.ErrorMessage}",
                    llmResult.Exception);
            }

            if (string.IsNullOrWhiteSpace(llmResult.DraftText))
            {
                throw new InvalidOperationException("LLM adapter returned empty draft text.");
            }

            // Audit the generated content for unsafe language
            var auditResult = _languageAuditor.Audit(llmResult.DraftText);
            
            // Prepare final content: if unsafe language is found, prepend disclaimer header
            // Do NOT modify content silently - original content is preserved, disclaimer is added
            string finalContent = llmResult.DraftText;
            var rulesReferenced = new List<string>();
            
            if (auditResult.HasUnsafeLanguage)
            {
                _logger.LogWarning(
                    "Unsafe language detected in draft. Prepending disclaimer header. Issues: {IssueCount}",
                    auditResult.Issues.Count);
                
                // Prepend disclaimer header - original content remains unchanged
                finalContent = auditResult.DisclaimerHeader + llmResult.DraftText;
                
                // Track the issues in explainability
                rulesReferenced.AddRange(auditResult.Issues);
            }

            // Step 4: Create DraftArtifact with status = Draft
            var draftArtifact = new DraftArtifact
            {
                Id = Guid.NewGuid(),
                DraftType = request.DraftType,
                SourceEntityId = request.SourceEntityId,
                Content = finalContent, // Contains disclaimer header if unsafe language was found
                GeneratedBy = generatedBy,
                Status = DraftStatus.Draft,
                GeneratedAt = DateTime.UtcNow,
                // Attach Explainability clearly stating: "AI-assisted draft. Requires human review."
                Explainability = new ExplainabilityDto
                {
                    Reasoning = auditResult.HasUnsafeLanguage
                        ? "AI-assisted draft. Requires human review. Unsafe language detected - disclaimer header added."
                        : "AI-assisted draft. Requires human review.",
                    RulesReferenced = rulesReferenced,
                    ConfidenceScore = 0.0, // Will be set by actual LLM implementation
                    HumanReviewRequired = true,
                    GeneratedBy = generatedBy,
                    GeneratedAt = DateTime.UtcNow
                }
            };

            // Step 5: Persist DraftArtifact
            await _repository.AddAsync(draftArtifact, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully generated and persisted draft. DraftArtifactId: {DraftArtifactId}",
                draftArtifact.Id);

            return draftArtifact;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating draft for DraftType: {DraftType}, SourceEntityId: {SourceEntityId}",
                request.DraftType,
                request.SourceEntityId);
            throw;
        }
    }
}

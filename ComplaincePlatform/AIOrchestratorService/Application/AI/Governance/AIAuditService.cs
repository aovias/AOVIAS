using System.Text.Json;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Application.AI.Governance;

public interface IAIAuditService
{
    Task LogAsync(AIRequest request, AIResponse response, string decisionPath, CancellationToken cancellationToken);
}

public sealed class AIAuditService : IAIAuditService
{
    private readonly AIOrchestratorDbContext _dbContext;

    public AIAuditService(AIOrchestratorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(AIRequest request, AIResponse response, string decisionPath, CancellationToken cancellationToken)
    {
        var citations = response.Grounded?.Citations?.Count > 0
            ? response.Grounded.Citations
            : response.SourceReferences;

        _dbContext.AIAuditEntries.Add(new Domain.AIAuditEntry
        {
            HospitalId = request.HospitalId,
            UseCase = request.UseCase.ToString(),
            PromptTemplateId = response.PromptTemplateId,
            PromptTemplateVersion = response.PromptTemplateVersion,
            ContextVersionId = response.ContextVersionId,
            ConfidenceScore = response.ConfidenceScore,
            CompositeConfidenceScore = response.CompositeConfidenceScore,
            ValidationOutcome = response.ValidationOutcome.ToString(),
            DecisionPath = decisionPath,
            SourceReferencesJson = JsonSerializer.Serialize(citations),
            ModelVersion = response.ModelVersion
                ?? response.Grounded?.ModelVersion
                ?? response.PromptTemplateVersion
                ?? string.Empty
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}

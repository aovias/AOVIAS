using System.Net.Http.Json;
using System.Text.Json;

namespace ComplianceService.Infrastructure.Clients;

public interface IAiOrchestratorAssistClient
{
    Task<AiSuggestMappingResponse?> SuggestMappingAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        string? prompt,
        CancellationToken cancellationToken = default);
}

public sealed class AiSuggestMappingResponse
{
    public string? ComplianceHint { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal CompositeConfidenceScore { get; set; }
    public bool HumanApprovalRequired { get; set; } = true;
    public List<AiSuggestMappingReasoning>? Reasoning { get; set; }
    public List<AiSuggestMappingSource>? SourceReferences { get; set; }
    public AiSuggestMappingSuggested? SuggestedMapping { get; set; }
}

public sealed class AiSuggestMappingReasoning
{
    public string? Code { get; set; }
    public string? Message { get; set; }
}

public sealed class AiSuggestMappingSource
{
    public string? SourceType { get; set; }
    public string? SourceId { get; set; }
    public string? Summary { get; set; }
    public decimal RelevanceScore { get; set; }
}

public sealed class AiSuggestMappingSuggested
{
    public Guid? ObjectiveElementId { get; set; }
    public string? MappingReason { get; set; }
}

/// <summary>HTTP bridge to AIOrchestrator assist/suggest for binder AI mapping.</summary>
public sealed class AiOrchestratorAssistClient(HttpClient http, ILogger<AiOrchestratorAssistClient> logger) : IAiOrchestratorAssistClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<AiSuggestMappingResponse?> SuggestMappingAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        string? prompt,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1.0/ai/assist/suggest");
            request.Headers.TryAddWithoutValidation("X-Hospital-Id", hospitalId.ToString());
            request.Content = JsonContent.Create(new
            {
                prompt = prompt ?? $"Suggest evidence mapping for objective element {objectiveElementId}",
                promptTemplateId = "SuggestMapping",
                promptTemplateVersion = "1.0",
                hospitalId,
                frameworkId,
                objectiveElementId,
                useCase = 3
            });

            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("AI assist suggest returned {Status} for OE {OeId}", (int)response.StatusCode, objectiveElementId);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<AiSuggestMappingResponse>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "AIOrchestrator SuggestMapping bridge failed for OE {OeId}", objectiveElementId);
            return null;
        }
    }
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AIOrchestratorService.Application.AI.Retrieval;

public sealed class ComplianceContextBundleDto
{
    public string ContextVersionId { get; set; } = string.Empty;
    public Dictionary<string, string> Facts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ComplianceContextSourceDto> Sources { get; set; } = new();
}

public sealed class ComplianceContextSourceDto
{
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public decimal RelevanceScore { get; set; }
}

public sealed class ComplianceContextBundleRequest
{
    [JsonPropertyName("hospitalId")]
    public Guid HospitalId { get; set; }

    [JsonPropertyName("frameworkId")]
    public Guid? FrameworkId { get; set; }

    [JsonPropertyName("objectiveElementId")]
    public Guid? ObjectiveElementId { get; set; }

    [JsonPropertyName("taskId")]
    public Guid? TaskId { get; set; }

    [JsonPropertyName("evidenceId")]
    public Guid? EvidenceId { get; set; }

    [JsonPropertyName("organizationId")]
    public Guid? OrganizationId { get; set; }

    [JsonPropertyName("pageContext")]
    public string? PageContext { get; set; }

    [JsonPropertyName("entityType")]
    public string? EntityType { get; set; }

    [JsonPropertyName("entityId")]
    public string? EntityId { get; set; }

    [JsonPropertyName("conversationSummary")]
    public string? ConversationSummary { get; set; }

    [JsonPropertyName("recentEntityRefs")]
    public List<string> RecentEntityRefs { get; set; } = new();
}

public sealed class ComplianceCommandPreconditionRequest
{
    [JsonPropertyName("hospitalId")]
    public Guid HospitalId { get; set; }

    [JsonPropertyName("objectiveElementId")]
    public Guid? ObjectiveElementId { get; set; }

    [JsonPropertyName("taskId")]
    public Guid? TaskId { get; set; }

    [JsonPropertyName("evidenceId")]
    public Guid? EvidenceId { get; set; }

    [JsonPropertyName("expectedContextVersionId")]
    public string ExpectedContextVersionId { get; set; } = string.Empty;

    [JsonPropertyName("idempotencyToken")]
    public string? IdempotencyToken { get; set; }

    [JsonPropertyName("approvalAttested")]
    public bool ApprovalAttested { get; set; }
}

public sealed class ComplianceCommandPreconditionResponse
{
    [JsonPropertyName("isAllowed")]
    public bool IsAllowed { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("currentContextVersionId")]
    public string CurrentContextVersionId { get; set; } = string.Empty;
}

public interface IComplianceContextClient
{
    Task<ComplianceContextBundleDto?> GetContextBundleAsync(ComplianceContextBundleRequest request, CancellationToken cancellationToken);
    Task<ComplianceCommandPreconditionResponse?> ValidateCommandPreconditionsAsync(ComplianceCommandPreconditionRequest request, CancellationToken cancellationToken);
}

public sealed class ComplianceContextClient : IComplianceContextClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ComplianceContextClient> _logger;

    public ComplianceContextClient(IHttpClientFactory httpClientFactory, ILogger<ComplianceContextClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("ComplianceServiceClient");
        _logger = logger;
    }

    public async Task<ComplianceContextBundleDto?> GetContextBundleAsync(ComplianceContextBundleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/internal/ai/context-bundle", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await SafeReadBodyAsync(response, cancellationToken);
                _logger.LogWarning(
                    "Compliance context bundle request failed with status {Status}. BaseAddress={BaseAddress}, HospitalId={HospitalId}, FrameworkId={FrameworkId}, Body={Body}",
                    (int)response.StatusCode,
                    _httpClient.BaseAddress,
                    request.HospitalId,
                    request.FrameworkId,
                    body);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ComplianceContextBundleDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(
                ex,
                "Compliance context bundle request transport failed. BaseAddress={BaseAddress}, HospitalId={HospitalId}, FrameworkId={FrameworkId}",
                _httpClient.BaseAddress,
                request.HospitalId,
                request.FrameworkId);
            return null;
        }
    }

    private static async Task<string> SafeReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return content.Length > 500 ? content[..500] : content;
        }
        catch
        {
            return "<unreadable>";
        }
    }

    public async Task<ComplianceCommandPreconditionResponse?> ValidateCommandPreconditionsAsync(ComplianceCommandPreconditionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/internal/ai/command-preconditions/validate", request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Compliance command precondition validation failed with status {Status}. BaseAddress={BaseAddress}",
                    (int)response.StatusCode,
                    _httpClient.BaseAddress);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ComplianceCommandPreconditionResponse>(cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Compliance command precondition transport failed. BaseAddress={BaseAddress}",
                _httpClient.BaseAddress);
            return null;
        }
    }
}

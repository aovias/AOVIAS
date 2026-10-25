using System.Net.Http.Json;

namespace AIOrchestratorService.Infrastructure.Clients;

public class ComplianceClient : IComplianceClient
{
    private readonly HttpClient _httpClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ComplianceClient> _logger;

    public ComplianceClient(IHttpClientFactory httpClientFactory, ILogger<ComplianceClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = httpClientFactory.CreateClient("InternalClient");
        _logger = logger;
    }

    public async Task<Guid> CreateProgramAsync(string regulation, Guid organizationId)
    {
        var request = new
        {
            Regulation = regulation,
            OrganizationId = organizationId
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v1.0/compliance/programs", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CreateProgramResponse>();
        return result?.ProgramId ?? Guid.Empty;
    }

    public async Task LoadControlsAsync(Guid programId, string regulation)
    {
        var request = new
        {
            Regulation = regulation
        };

        var response = await _httpClient.PostAsJsonAsync($"/api/v1.0/compliance/programs/{programId}/controls", request);
        response.EnsureSuccessStatusCode();
    }

    public async Task AssignTasksAsync(Guid programId, Guid organizationId)
    {
        var request = new
        {
            OrganizationId = organizationId
        };

        var response = await _httpClient.PostAsJsonAsync($"/api/v1.0/compliance/programs/{programId}/tasks", request);
        // Endpoint is deprecated (410 Gone); orchestration no longer relies on program-based task assignment.
        if (response.StatusCode != System.Net.HttpStatusCode.Gone)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async Task<IReadOnlyDictionary<string, string>> ExecuteReadOnlyToolAsync(
        Guid hospitalId,
        Guid? organizationId,
        Guid? frameworkId,
        string toolName,
        IReadOnlyDictionary<string, string>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var client = _httpClientFactory.CreateClient("ComplianceServiceClient");
        var payload = new
        {
            hospitalId,
            organizationId = organizationId ?? hospitalId,
            frameworkId,
            toolName,
            parameters = parameters ?? new Dictionary<string, string>()
        };

        using var response = await client.PostAsJsonAsync(
            "/api/v1.0/internal/ai/readonly-tools/execute",
            payload,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Read-only compliance tool {Tool} failed with {Status}",
                toolName,
                (int)response.StatusCode);
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["status"] = "failed",
                ["tool"] = toolName,
                ["httpStatus"] = ((int)response.StatusCode).ToString()
            };
        }

        var result = await response.Content.ReadFromJsonAsync<ReadOnlyToolWireResponse>(cancellationToken: cancellationToken);
        var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "executed",
            ["mode"] = "readonly",
            ["tool"] = toolName,
            ["summary"] = result?.Summary ?? string.Empty
        };

        if (result?.Facts != null)
        {
            foreach (var (key, value) in result.Facts)
            {
                output[$"fact_{key}"] = value;
            }
        }

        return output;
    }

    private sealed class CreateProgramResponse
    {
        public Guid ProgramId { get; set; }
    }

    private sealed class ReadOnlyToolWireResponse
    {
        public string ToolName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Summary { get; set; } = string.Empty;
        public Dictionary<string, string>? Facts { get; set; }
    }
}

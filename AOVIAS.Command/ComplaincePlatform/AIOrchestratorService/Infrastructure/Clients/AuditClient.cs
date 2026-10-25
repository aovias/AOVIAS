using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIOrchestratorService.Infrastructure.Clients;

/// <summary>
/// Calls ComplianceService audit generate facade (SSOT). Legacy AuditService is not used.
/// </summary>
public class AuditClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuditClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public AuditClient(IHttpClientFactory httpClientFactory, ILogger<AuditClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient("InternalClient");
        _logger = logger;
    }

    public async Task<Guid> GenerateAudit(Guid orgId, Guid programId)
    {
        var request = new
        {
            OrganizationId = orgId,
            ComplianceProgramId = programId
        };

        var response = await _httpClient.PostAsJsonAsync("/api/v1.0/audit/generate", request);
        response.EnsureSuccessStatusCode();

        var raw = await response.Content.ReadAsStringAsync();
        var id = TryExtractId(raw);
        if (id == Guid.Empty)
        {
            _logger.LogWarning("GenerateAudit returned no Id for organization {OrgId} and program {ProgramId}", orgId, programId);
        }

        return id;
    }

    private static Guid TryExtractId(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Guid.Empty;

        // ApiResponse envelope: { success, data: { id } }
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            {
                if (TryGetGuid(data, "id", out var fromData) || TryGetGuid(data, "Id", out fromData))
                    return fromData;
            }

            if (TryGetGuid(root, "id", out var flat) || TryGetGuid(root, "Id", out flat))
                return flat;
        }
        catch
        {
            // fall through
        }

        var envelope = JsonSerializer.Deserialize<ApiEnvelope<GenerateAuditResponse>>(raw, JsonOptions);
        if (envelope?.Data?.Id is { } envelopeId && envelopeId != Guid.Empty)
            return envelopeId;

        var direct = JsonSerializer.Deserialize<GenerateAuditResponse>(raw, JsonOptions);
        return direct?.Id ?? Guid.Empty;
    }

    private static bool TryGetGuid(JsonElement el, string name, out Guid id)
    {
        id = Guid.Empty;
        if (!el.TryGetProperty(name, out var prop))
            return false;
        if (prop.ValueKind == JsonValueKind.String && Guid.TryParse(prop.GetString(), out id))
            return true;
        return false;
    }

    private sealed class ApiEnvelope<T>
    {
        [JsonPropertyName("data")]
        public T? Data { get; set; }
    }

    private sealed class GenerateAuditResponse
    {
        public Guid Id { get; set; }
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Inference;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Infrastructure.LLM;

public sealed class AzureOpenAIModelAdapter : IAIModelAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<AzureOpenAIOptions> _options;
    private readonly ILogger<AzureOpenAIModelAdapter> _logger;

    public AzureOpenAIModelAdapter(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<AzureOpenAIOptions> options,
        ILogger<AzureOpenAIModelAdapter> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AzureOpenAI");
        _options = options;
        _logger = logger;
    }

    public async Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken)
    {
        var cfg = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(cfg.Endpoint) ||
            string.IsNullOrWhiteSpace(cfg.ApiKey) ||
            string.IsNullOrWhiteSpace(cfg.DefaultDeployment))
        {
            _logger.LogWarning("AzureOpenAI not configured; returning deterministic fallback");
            return BuildFallback(request, "AzureOpenAI not configured");
        }

        var deployment = ResolveDeployment(cfg, request.TenantKey);
        var endpoint = cfg.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={cfg.ApiVersion}";
        var redactedPrompt = SensitiveDataRedactor.Redact(request.Prompt);

        var payload = new
        {
            messages = new object[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = $"Reference context (may be empty): {JsonSerializer.Serialize(request.Context.Facts)}\nUser Prompt: {request.Prompt}" }
            },
            temperature = 0.1,
            max_tokens = 500
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(payload)
        };
        message.Headers.Add("api-key", cfg.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "AzureOpenAI call failed with status {StatusCode} for tenant {TenantKey}. Prompt (redacted): {Prompt}",
                    (int)response.StatusCode,
                    request.TenantKey,
                    redactedPrompt);
                return BuildFallback(request, $"AzureOpenAI HTTP {(int)response.StatusCode}");
            }

            using var doc = JsonDocument.Parse(body);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            _logger.LogInformation(
                "AzureOpenAI response generated for tenant {TenantKey}. Prompt template applied.",
                request.TenantKey);

            var modelResponse = new AIModelResponse
            {
                Hint = content,
                Confidence = 0.72m,
                ReasoningBullets = new List<string>
                {
                    "Response generated from tenant-aware Azure OpenAI deployment.",
                    "Grounded using retrieval facts and bounded prompt policy."
                }
            };

            // The advisor prompt requests strict JSON; when the deployment honors it, lift the
            // hint and structured Executive Compliance Advisor fields out of the JSON object.
            if (TryParseJsonObject(content, out var root))
            {
                var hint = ReadString(root, "hint", "complianceHint", "answer", "executiveSummary");
                if (!string.IsNullOrWhiteSpace(hint))
                {
                    modelResponse.Hint = hint;
                }

                StructuredModelOutputParser.Populate(root, modelResponse);
            }

            return modelResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "AzureOpenAI execution error for tenant {TenantKey}. Prompt (redacted): {Prompt}",
                request.TenantKey,
                redactedPrompt);
            return BuildFallback(request, ex.Message);
        }
    }

    private static bool TryParseJsonObject(string content, out JsonElement root)
    {
        root = default;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = trimmed
                .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return false;
        }

        var json = trimmed.Substring(start, end - start + 1);
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            // Clone so the element remains valid after the document is disposed.
            root = doc.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                var content = value.GetString();
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content.Trim();
                }
            }
        }

        return string.Empty;
    }

    private static AIModelResponse BuildFallback(AIModelRequest request, string reason)
    {
        return new AIModelResponse
        {
            Hint = $"AI advisory fallback: {reason}. Use deterministic compliance reports while AI recovers.",
            Confidence = 0.35m,
            ReasoningBullets = new List<string>
            {
                "Fallback path used to preserve deterministic workflow availability.",
                "No compliance status changes were attempted."
            }
        };
    }

    private static string ResolveDeployment(AzureOpenAIOptions cfg, string tenantKey)
    {
        if (!string.IsNullOrWhiteSpace(tenantKey) &&
            cfg.TenantDeploymentOverrides.TryGetValue(tenantKey, out var tenantDeployment) &&
            !string.IsNullOrWhiteSpace(tenantDeployment))
        {
            return tenantDeployment;
        }

        return cfg.DefaultDeployment;
    }

}


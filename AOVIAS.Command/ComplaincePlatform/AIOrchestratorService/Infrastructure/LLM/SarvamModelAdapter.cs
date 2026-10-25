using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AIOrchestratorService.Application.AI.Classification;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Reliability;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Infrastructure.LLM;

public sealed class SarvamModelAdapter : IAIModelAdapter
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<SarvamOptions> _options;
    private readonly IJSONRepairLayer _jsonRepairLayer;
    private readonly IMetricsTracker _metrics;
    private readonly ILogger<SarvamModelAdapter> _logger;

    public SarvamModelAdapter(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<SarvamOptions> options,
        IJSONRepairLayer jsonRepairLayer,
        IMetricsTracker metrics,
        ILogger<SarvamModelAdapter> logger)
    {
        _httpClient = httpClientFactory.CreateClient("Sarvam");
        _options = options;
        _jsonRepairLayer = jsonRepairLayer;
        _metrics = metrics;
        _logger = logger;
    }

    public async Task<AIModelResponse> GenerateAsync(AIModelRequest request, CancellationToken cancellationToken)
    {
        var options = _options.CurrentValue;
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ProviderFailureException(
                "Sarvam API key is not configured.",
                ProviderFailureReason.TransportError);
        }

        var timeoutSeconds = Math.Max(2, options.TimeoutSeconds);
        _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

        var rawContent = await ExecuteCallAsync(request, options, strongerFormatterPrompt: false, cancellationToken);
        if (TryParseModelResponse(rawContent, out var parsed))
        {
            _metrics.TrackJsonValidity("Sarvam", valid: true, request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);
            return parsed;
        }

        _metrics.TrackJsonValidity("Sarvam", valid: false, request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);
        if (options.RetryCount > 0)
        {
            rawContent = await ExecuteCallAsync(request, options, strongerFormatterPrompt: true, cancellationToken);
            if (TryParseModelResponse(rawContent, out parsed))
            {
                _metrics.TrackJsonValidity("Sarvam", valid: true, request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);
                return parsed;
            }
        }

        if (TryBuildUnstructuredFallback(rawContent, out parsed))
        {
            _logger.LogWarning("Sarvam returned non-JSON content; normalized to unstructured assistive response.");
            return parsed;
        }

        _metrics.TrackJsonValidity("Sarvam", valid: false, request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);
        throw new ProviderFailureException(
            "Sarvam returned invalid JSON after repair and retry.",
            ProviderFailureReason.InvalidJson,
            retryable: false);
    }

    private async Task<string> ExecuteCallAsync(
        AIModelRequest request,
        SarvamOptions options,
        bool strongerFormatterPrompt,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = options.Model,
            stream = false,
            temperature = 0.05,
            top_p = 1.0,
            max_tokens = Math.Max(128, options.MaxTokensPerRequest),
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = BuildSystemInstruction(request.SystemPrompt, request.AnswerMode, strongerFormatterPrompt)
                },
                new
                {
                    role = "user",
                    content = $"Reference context JSON (may be empty if nothing was retrieved): {JsonSerializer.Serialize(request.Context)}. User question: {request.Prompt}"
                }
            }
        };

        using var message = new HttpRequestMessage(
            HttpMethod.Post,
            $"{options.BaseUrl.TrimEnd('/')}/v1/chat/completions")
        {
            Content = JsonContent.Create(payload)
        };

        ApplyAuth(message, options);
        var started = DateTime.UtcNow;

        try
        {
            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            var latency = DateTime.UtcNow - started;
            _metrics.TrackLatency("Sarvam", latency, request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);

            if (!response.IsSuccessStatusCode)
            {
                var reason = response.StatusCode switch
                {
                    HttpStatusCode.TooManyRequests => ProviderFailureReason.RateLimited,
                    >= HttpStatusCode.InternalServerError => ProviderFailureReason.ServerError,
                    _ => ProviderFailureReason.TransportError
                };

                throw new ProviderFailureException(
                    $"Sarvam request failed with status {(int)response.StatusCode}.",
                    reason,
                    statusCode: (int)response.StatusCode,
                    retryable: reason is ProviderFailureReason.RateLimited or ProviderFailureReason.ServerError);
            }

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
                ?? string.Empty;
        }
        catch (TaskCanceledException ex)
        {
            _metrics.TrackTimeout("Sarvam", request.UseCase.ToString(), request.TenantKey, request.PromptTemplateVersion);
            throw new ProviderFailureException(
                "Sarvam request timed out.",
                ProviderFailureReason.Timeout,
                retryable: true,
                innerException: ex);
        }
    }

    private bool TryParseModelResponse(string rawContent, out AIModelResponse modelResponse)
    {
        modelResponse = new AIModelResponse();
        if (!_jsonRepairLayer.TryRepair(rawContent, out var repairedRoot, out _))
        {
            return false;
        }

        if (repairedRoot.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var hint = ReadString(repairedRoot, "hint", "complianceHint", "answer");
        if (string.IsNullOrWhiteSpace(hint))
        {
            return false;
        }

        var confidence = ReadDecimal(repairedRoot, "confidence", "confidenceScore");
        var reasoning = ReadStringList(repairedRoot, "reasoningBullets", "reasoning");
        if (reasoning.Count == 0)
        {
            reasoning.Add("Response parsed from Sarvam structured output.");
        }

        modelResponse = new AIModelResponse
        {
            Hint = hint,
            Confidence = Math.Clamp(confidence ?? 0.5m, 0m, 1m),
            ReasoningBullets = reasoning
        };
        StructuredModelOutputParser.Populate(repairedRoot, modelResponse);
        return true;
    }

    private static string BuildSystemInstruction(string baseSystemPrompt, AssistantAnswerMode answerMode, bool strongerFormatterPrompt)
    {
        // The grounding rule is mode-aware: organization-data queries stay strictly grounded,
        // while general/compliance-knowledge and hybrid queries may use the model's own knowledge
        // when retrieval is empty so the assistant never refuses a general question.
        var groundingRule = AssistantQueryClassifier.PermitsGeneralKnowledge(answerMode)
            ? "Rules: ground findings in the supplied workspace data when relevant; for a general healthcare/compliance question without workspace data, answer from your expert knowledge as professional guidance. Do not invent organization-specific objective ids, findings, or numbers. Never mention context, facts, retrieval, confidence, source ids, or AI internals; never say the context or facts are empty."
            : "Rules: answer from the supplied organization workspace data only; do not invent objective ids; if the requested records are not present, say \"Available compliance evidence does not currently indicate...\" and suggest what to check. Never mention context, facts, retrieval, confidence, source ids, or AI internals.";

        var strictJson = $$"""
Respond ONLY in valid JSON object format as a Senior Healthcare Compliance Advisor briefing:
{
  "executiveSummary": "string",
  "insightType": "ComplianceScore|Risk|Evidence|Standard|General",
  "findings": [{"title": "string", "severity": "Critical|High|Medium|Low", "impact": "string", "detail": "string"}],
  "risks": [{"title": "string", "severity": "Critical|High|Medium|Low", "affectedControls": ["string"]}],
  "recommendations": [{"priority": 1, "action": "string", "expectedOutcome": "string"}],
  "expectedOutcome": "string",
  "hint": "string",
  "confidence": 0.0,
  "reasoningBullets": ["string"]
}
The "hint" field must duplicate the executiveSummary text. Use empty arrays for sections that do not apply.
{{groundingRule}}
""";

        if (strongerFormatterPrompt)
        {
            strictJson += "\nDO NOT include markdown, prose, or any text before/after JSON.";
        }

        return $"{baseSystemPrompt}\n{strictJson}";
    }

    private static void ApplyAuth(HttpRequestMessage message, SarvamOptions options)
    {
        if (string.Equals(options.AuthMode, "ApiSubscriptionKey", StringComparison.OrdinalIgnoreCase))
        {
            message.Headers.Add("api-subscription-key", options.ApiKey);
            return;
        }

        message.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    private static string ReadString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var content = value.GetString();
            if (!string.IsNullOrWhiteSpace(content))
            {
                return content;
            }
        }

        return string.Empty;
    }

    private static decimal? ReadDecimal(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
            {
                continue;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var direct))
            {
                return direct;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), out var fromString))
            {
                return fromString;
            }
        }

        return null;
    }

    private static List<string> ReadStringList(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            var items = new List<string>();
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var text = item.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    items.Add(text);
                }
            }

            return items;
        }

        return new List<string>();
    }

    private static bool TryBuildUnstructuredFallback(string rawContent, out AIModelResponse modelResponse)
    {
        modelResponse = new AIModelResponse();
        var normalized = (rawContent ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (normalized.StartsWith("```", StringComparison.Ordinal))
        {
            normalized = normalized
                .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        if (normalized.Length > 1500)
        {
            normalized = normalized[..1500];
        }

        modelResponse = new AIModelResponse
        {
            Hint = normalized,
            Confidence = 0.52m,
            ReasoningBullets = new List<string>
            {
                "Unstructured provider output normalized for assistive response.",
                "Strict JSON schema was not met; proceeding with conservative confidence."
            }
        };
        return true;
    }
}

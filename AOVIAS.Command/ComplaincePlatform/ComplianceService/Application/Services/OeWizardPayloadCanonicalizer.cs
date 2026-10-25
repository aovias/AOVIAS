using System.Text.Json;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.Services;

/// <summary>
/// Builds a stable JSON string for byte-identical comparison (server + client use the same key order).
/// </summary>
public static class OeWizardPayloadCanonicalizer
{
    public const int MaxPayloadUtf8Bytes = 64 * 1024;

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    /// <summary>Fixed key order — must match <c>canonicalizeOeWizardPayload</c> in WebApp.</summary>
    private static readonly string[] KeyOrder =
    [
        "auditChecklistChecked",
        "completeAck",
        "evalJustification",
        "evalScore",
        "evalSubmitted",
        "evalTone",
        "evidenceAck",
        "executeReady",
        "improveReady",
        "needsImprove",
        "savedAt",
        "stepIndex",
        "understandAck"
    ];

    public static string Canonicalize(OeWizardPersistedState state)
    {
        var dict = new Dictionary<string, object?>(KeyOrder.Length, StringComparer.Ordinal);
        foreach (var key in KeyOrder)
        {
            dict[key] = key switch
            {
                "auditChecklistChecked" => state.AuditChecklistChecked,
                "completeAck" => state.CompleteAck,
                "evalJustification" => state.EvalJustification,
                "evalScore" => state.EvalScore,
                "evalSubmitted" => state.EvalSubmitted,
                "evalTone" => state.EvalTone,
                "evidenceAck" => state.EvidenceAck,
                "executeReady" => state.ExecuteReady,
                "improveReady" => state.ImproveReady,
                "needsImprove" => state.NeedsImprove,
                "savedAt" => state.SavedAt,
                "stepIndex" => state.StepIndex,
                "understandAck" => state.UnderstandAck,
                _ => throw new InvalidOperationException($"Unknown canonical key {key}.")
            };
        }

        return JsonSerializer.Serialize(dict, SerializeOptions);
    }

    public static void EnsurePayloadSizeWithinLimit(string rawJsonUtf8, int maxBytes = MaxPayloadUtf8Bytes)
    {
        var byteCount = System.Text.Encoding.UTF8.GetByteCount(rawJsonUtf8);
        if (byteCount > maxBytes)
        {
            throw new ArgumentException($"Wizard payload exceeds maximum size of {maxBytes} bytes.", nameof(rawJsonUtf8));
        }
    }
}

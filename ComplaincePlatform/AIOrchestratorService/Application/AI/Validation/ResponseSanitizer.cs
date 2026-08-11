using System.Text.RegularExpressions;
using AIOrchestratorService.Application.AI.Classification;

namespace AIOrchestratorService.Application.AI.Validation;

public interface IResponseSanitizer
{
    /// <summary>
    /// Removes any internal RAG/retrieval failure language a model may have emitted and,
    /// when the whole answer was a refusal, replaces it with a mode-appropriate message.
    /// </summary>
    string Sanitize(string? hint, AssistantAnswerMode answerMode);

    /// <summary>
    /// Drops reasoning bullets that leak internal RAG/retrieval state. Returns clean bullets only.
    /// </summary>
    List<string> SanitizeReasoning(IEnumerable<string>? bullets);

    bool ContainsForbiddenPhrase(string? text);

    /// <summary>
    /// Removes sentences containing forbidden internal/RAG language from an inline string
    /// (card titles, impacts, actions). Unlike <see cref="Sanitize"/> it never substitutes a
    /// fallback message; returns an empty string when nothing usable remains.
    /// </summary>
    string ScrubInline(string? text);

    /// <summary>
    /// Business-friendly reasoning note when org/hybrid answers lack grounded citations.
    /// </summary>
    string MissingCitationReasoningNote(AssistantAnswerMode answerMode);
}

public sealed class ResponseSanitizer : IResponseSanitizer
{
    private static readonly string[] ForbiddenPhrases =
    {
        "context does not contain",
        "the context does not",
        "context provided does not",
        "no information about",
        "facts object is empty",
        "facts object",
        "facts are empty",
        "empty facts",
        "no facts or sources",
        "no facts",
        "vector search",
        "retrieval returned",
        "retrieval failed",
        "retrieved context is empty",
        "retrieved context",
        "no relevant facts",
        "no context found",
        "context is empty",
        "context missing",
        "prompt not found",
        "source unavailable",
        "no sources were found",
        "no sources",
        "knowledge base returned no",
        "i do not have any context",
        "based on the provided context, there is no",
        "without organization-specific data",
        "without organization specific data",
        "impossible to summarize",
        "no data was retrieved",
        "is empty, with no"
    };

    private static readonly Regex SentenceSplit = new(@"(?<=[\.!?])\s+", RegexOptions.Compiled);

    public bool ContainsForbiddenPhrase(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var lower = text.ToLowerInvariant();
        foreach (var phrase in ForbiddenPhrases)
        {
            if (lower.Contains(phrase, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public List<string> SanitizeReasoning(IEnumerable<string>? bullets)
    {
        if (bullets is null)
        {
            return new List<string>();
        }

        return bullets
            .Where(b => !string.IsNullOrWhiteSpace(b) && !ContainsForbiddenPhrase(b))
            .Select(b => b.Trim())
            .ToList();
    }

    public string Sanitize(string? hint, AssistantAnswerMode answerMode)
    {
        var text = (hint ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return Fallback(answerMode);
        }

        if (!ContainsForbiddenPhrase(text))
        {
            return text;
        }

        // Drop only the offending sentences; keep any genuinely useful content.
        var kept = SentenceSplit
            .Split(text)
            .Where(sentence => !ContainsForbiddenPhrase(sentence))
            .Select(sentence => sentence.Trim())
            .Where(sentence => sentence.Length > 0)
            .ToList();

        var cleaned = string.Join(" ", kept).Trim();
        return cleaned.Length >= 24 ? cleaned : Fallback(answerMode);
    }

    public string ScrubInline(string? text)
    {
        var value = (text ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return string.Empty;
        }

        if (!ContainsForbiddenPhrase(value))
        {
            return value;
        }

        var kept = SentenceSplit
            .Split(value)
            .Where(sentence => !ContainsForbiddenPhrase(sentence))
            .Select(sentence => sentence.Trim())
            .Where(sentence => sentence.Length > 0)
            .ToList();

        return string.Join(" ", kept).Trim();
    }

    public string MissingCitationReasoningNote(AssistantAnswerMode answerMode) => answerMode switch
    {
        AssistantAnswerMode.OrganizationSpecific =>
            "Confidence reduced: no workspace citations were available to ground this organization-specific answer. Treat as advisory until evidence is linked.",
        AssistantAnswerMode.Hybrid =>
            "Confidence reduced: limited workspace citations. Blend of general guidance and partial organization data — verify against primary records before acting.",
        _ =>
            "Further evidence would improve assessment accuracy."
    };

    private static string Fallback(AssistantAnswerMode answerMode) => answerMode switch
    {
        AssistantAnswerMode.OrganizationSpecific =>
            "I couldn't find matching records in your current workspace for that request. Try adjusting the framework or hospital context, or check the relevant dashboard.",
        AssistantAnswerMode.SmallTalk =>
            "Hello! I'm your PRAMAN compliance copilot. Ask me about NABH, audits, CAPA, evidence, or your organization's compliance status.",
        _ =>
            "Here is general compliance guidance: I can explain healthcare accreditation and compliance concepts such as NABH, JCI, ISO, HIPAA, CAPA, and FMS. Could you share a bit more detail so I can give you a precise answer?"
    };
}

namespace AIOrchestratorService.Application.AI.Classification;

/// <summary>
/// How the assistant should answer a given user turn. Drives prompt selection,
/// confidence calibration, and source labelling.
/// </summary>
public enum AssistantAnswerMode
{
    /// <summary>General healthcare/compliance concept answered from model knowledge.</summary>
    GeneralKnowledge = 0,

    /// <summary>Domain explanation (framework/standard) answered from model knowledge, no org data needed.</summary>
    ComplianceKnowledge = 1,

    /// <summary>Requires organization-specific PRAMAN data (tasks, findings, evidence, audits).</summary>
    OrganizationSpecific = 2,

    /// <summary>Needs both a general explanation and the organization's live posture.</summary>
    Hybrid = 3,

    /// <summary>Greeting / chit-chat / acknowledgement.</summary>
    SmallTalk = 4
}

public interface IAssistantQueryClassifier
{
    AssistantAnswerMode Classify(string? prompt, bool hasRetrievedData);
}

/// <summary>
/// Deterministic, rule-based query classifier. Intentionally avoids an extra LLM
/// round-trip so it stays within the orchestration timeout budget and is unit-testable.
/// </summary>
public sealed class AssistantQueryClassifier : IAssistantQueryClassifier
{
    private static readonly string[] SmallTalkPhrases =
    {
        "hello", "hi ", "hey", "good morning", "good afternoon", "good evening",
        "thank you", "thanks", "how are you", "who are you", "what can you do",
        "good night", "bye", "goodbye"
    };

    private static readonly string[] OrgSignals =
    {
        "show ", "list ", "our ", " we ", "we ", " us ", "my ", "do we", "are we",
        "how compliant", "overdue", "upcoming", "missing evidence", "missing ",
        "which chapter", "which chapters", "below ", "audit readiness", "readiness",
        "current ", "pending task", "open finding", "this hospital", "this org",
        "in our", "our organization", "our hospital", "this workspace", "right now",
        "how many", "status of", "are there any"
    };

    private static readonly string[] DefinitionalSignals =
    {
        "what is", "what are", "what's", "whats ", "explain", "define", "definition",
        "tell me about", "meaning of", "stands for", "describe", "overview of",
        "how does", "purpose of"
    };

    private static readonly string[] DomainTerms =
    {
        "nabh", "jci", "iso", "hipaa", "capa", "fms", "acc", "hrm", "bme", "icn",
        "cqi", "accreditation", "accredit", "regulation", "regulatory", "framework",
        "chapter", "audit", "quality", "fire safety", "biomedical", "infection control",
        "compliance", "objective element", "standard", "policy", "evidence", "finding",
        "patient safety", "clause", "gap"
    };

    public AssistantAnswerMode Classify(string? prompt, bool hasRetrievedData)
    {
        var text = (prompt ?? string.Empty).Trim().ToLowerInvariant();
        if (text.Length == 0)
        {
            return AssistantAnswerMode.GeneralKnowledge;
        }

        if (IsSmallTalk(text))
        {
            return AssistantAnswerMode.SmallTalk;
        }

        var hasOrgSignal = ContainsAny(text, OrgSignals);
        var isDefinitional = ContainsAny(text, DefinitionalSignals);
        var hasDomainTerm = ContainsAny(text, DomainTerms);

        // Asks to explain a concept AND for the organization's posture -> blend both.
        // (A bare operational query also mentions domain nouns like "audit"/"evidence",
        // so Hybrid requires an explicit definitional/explanatory ask, not just a domain term.)
        if (hasOrgSignal && isDefinitional)
        {
            return AssistantAnswerMode.Hybrid;
        }

        // Pure operational/data query -> must use PRAMAN data.
        if (hasOrgSignal)
        {
            return AssistantAnswerMode.OrganizationSpecific;
        }

        // Definitional question about a domain concept -> general knowledge.
        if (isDefinitional && hasDomainTerm)
        {
            return Upgrade(AssistantAnswerMode.GeneralKnowledge, hasRetrievedData);
        }

        // Mentions a domain concept without asking for org data -> compliance knowledge.
        if (hasDomainTerm)
        {
            return Upgrade(AssistantAnswerMode.ComplianceKnowledge, hasRetrievedData);
        }

        // Anything else: let the model answer from general knowledge rather than refuse.
        return Upgrade(AssistantAnswerMode.GeneralKnowledge, hasRetrievedData);
    }

    /// <summary>True when the mode is allowed to answer from the model's own knowledge.</summary>
    public static bool PermitsGeneralKnowledge(AssistantAnswerMode mode) =>
        mode is AssistantAnswerMode.GeneralKnowledge
            or AssistantAnswerMode.ComplianceKnowledge
            or AssistantAnswerMode.Hybrid
            or AssistantAnswerMode.SmallTalk;

    /// <summary>True when the answer fundamentally needs organization data to be useful.</summary>
    public static bool RequiresOrgData(AssistantAnswerMode mode) =>
        mode is AssistantAnswerMode.OrganizationSpecific;

    private static AssistantAnswerMode Upgrade(AssistantAnswerMode baseMode, bool hasRetrievedData)
    {
        // If PRAMAN data is available, blend it with the general explanation.
        return hasRetrievedData ? AssistantAnswerMode.Hybrid : baseMode;
    }

    private static bool IsSmallTalk(string text)
    {
        if (text.Length > 40)
        {
            return false;
        }

        foreach (var phrase in SmallTalkPhrases)
        {
            if (text.StartsWith(phrase, StringComparison.Ordinal) ||
                text.Equals(phrase.Trim(), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAny(string text, string[] needles)
    {
        foreach (var needle in needles)
        {
            if (text.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

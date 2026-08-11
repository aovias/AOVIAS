using System.Globalization;
using System.Text.RegularExpressions;
using AIOrchestratorService.Application.AI.Models;

namespace AIOrchestratorService.Application.AI.Governance;

public sealed class HallucinationGuardResult
{
    public bool IsFlagged { get; init; }
    public IReadOnlyList<string> UnsupportedClaims { get; init; } = Array.Empty<string>();
    public decimal AdjustedConfidence { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public interface IHallucinationGuard
{
    /// <summary>
    /// Heuristic grounding check: numbers/IDs asserted in the answer must appear in citations or facts.
    /// When unsupported claims are found, confidence is reduced.
    /// </summary>
    HallucinationGuardResult Evaluate(
        string answer,
        IEnumerable<AISourceReference>? citations,
        IDictionary<string, string>? facts,
        decimal currentConfidence);
}

public sealed class HallucinationGuard : IHallucinationGuard
{
    private static readonly Regex NumberOrIdPattern = new(
        @"\b(?:[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}|\d{1,3}(?:,\d{3})+(?:\.\d+)?%?|\d+\.\d+%?|\d{2,}(?:\.\d+)?%?|[A-Z]{2,}[\.\-]?\d{1,6})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public HallucinationGuardResult Evaluate(
        string answer,
        IEnumerable<AISourceReference>? citations,
        IDictionary<string, string>? facts,
        decimal currentConfidence)
    {
        var clamped = Math.Clamp(currentConfidence, 0m, 1m);
        if (string.IsNullOrWhiteSpace(answer))
        {
            return new HallucinationGuardResult { AdjustedConfidence = clamped };
        }

        var citationList = citations?.ToList() ?? new List<AISourceReference>();
        var factMap = facts ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (citationList.Count == 0 && factMap.Count == 0)
        {
            // Nothing to ground against — skip (general-knowledge / empty-context answers).
            return new HallucinationGuardResult { AdjustedConfidence = clamped };
        }

        var allowed = BuildAllowedCorpus(citationList, factMap);
        var claims = ExtractClaims(answer);
        var unsupported = claims
            .Where(claim => !IsAllowed(claim, allowed))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (unsupported.Count == 0)
        {
            return new HallucinationGuardResult { AdjustedConfidence = clamped };
        }

        // Each unsupported claim reduces confidence; floor at 0.15 so the signal remains visible.
        var penalty = Math.Min(0.55m, unsupported.Count * 0.18m);
        var adjusted = Math.Max(0.15m, clamped - penalty);

        return new HallucinationGuardResult
        {
            IsFlagged = true,
            UnsupportedClaims = unsupported,
            AdjustedConfidence = adjusted,
            Reason = $"Answer asserts {unsupported.Count} number/ID claim(s) not present in citations or facts."
        };
    }

    private static HashSet<string> BuildAllowedCorpus(
        IReadOnlyList<AISourceReference> citations,
        IDictionary<string, string> facts)
    {
        var corpus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var citation in citations)
        {
            AddFragments(corpus, citation.SourceId);
            AddFragments(corpus, citation.DocumentId);
            AddFragments(corpus, citation.Version);
            AddFragments(corpus, citation.Summary);
            AddFragments(corpus, citation.Excerpt);
        }

        foreach (var pair in facts)
        {
            AddFragments(corpus, pair.Key);
            AddFragments(corpus, pair.Value);
        }

        return corpus;
    }

    private static void AddFragments(HashSet<string> corpus, string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        corpus.Add(text.Trim());
        foreach (Match match in NumberOrIdPattern.Matches(text))
        {
            corpus.Add(Normalize(match.Value));
        }
    }

    private static List<string> ExtractClaims(string answer)
    {
        return NumberOrIdPattern.Matches(answer)
            .Select(m => Normalize(m.Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsAllowed(string claim, HashSet<string> allowed)
    {
        if (allowed.Contains(claim))
        {
            return true;
        }

        // Allow substring containment for IDs embedded in longer fact strings.
        foreach (var token in allowed)
        {
            if (token.Contains(claim, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Ignore lone small integers that are typically prose (counts of sentences, "step 2", etc.).
        if (int.TryParse(claim, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n is >= 0 and <= 10)
        {
            return true;
        }

        return false;
    }

    private static string Normalize(string value)
    {
        return value.Trim().Replace(",", string.Empty, StringComparison.Ordinal);
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;

namespace AIOrchestratorService.Application.AI.Reliability;

public interface IJSONRepairLayer
{
    bool TryRepair(string raw, out JsonElement repairedRoot, out string normalizedJson);
}

public sealed class JSONRepairLayer : IJSONRepairLayer
{
    public bool TryRepair(string raw, out JsonElement repairedRoot, out string normalizedJson)
    {
        repairedRoot = default;
        normalizedJson = "{}";
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var candidate = ExtractLikelyJson(raw);
        candidate = StripMarkdownFences(candidate);
        candidate = FixTrailingCommas(candidate);
        candidate = CloseOpenBraces(candidate);

        try
        {
            using var doc = JsonDocument.Parse(candidate);
            repairedRoot = doc.RootElement.Clone();
            normalizedJson = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string ExtractLikelyJson(string raw)
    {
        var firstBrace = raw.IndexOf('{');
        var firstBracket = raw.IndexOf('[');
        var start = new[] { firstBrace, firstBracket }.Where(x => x >= 0).DefaultIfEmpty(-1).Min();
        if (start < 0)
        {
            return raw.Trim();
        }

        var sliced = raw[start..].Trim();
        var lastBrace = sliced.LastIndexOf('}');
        var lastBracket = sliced.LastIndexOf(']');
        var end = Math.Max(lastBrace, lastBracket);
        if (end >= 0)
        {
            sliced = sliced[..(end + 1)];
        }

        return sliced.Trim();
    }

    private static string StripMarkdownFences(string input)
    {
        var trimmed = input.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            trimmed = Regex.Replace(trimmed, "^```[a-zA-Z]*\\s*", string.Empty);
            trimmed = Regex.Replace(trimmed, "\\s*```$", string.Empty);
        }

        return trimmed.Trim();
    }

    private static string FixTrailingCommas(string input)
    {
        return Regex.Replace(input, ",\\s*([}\\]])", "$1");
    }

    private static string CloseOpenBraces(string input)
    {
        var openCurly = input.Count(c => c == '{');
        var closeCurly = input.Count(c => c == '}');
        var openSquare = input.Count(c => c == '[');
        var closeSquare = input.Count(c => c == ']');

        if (closeSquare < openSquare)
        {
            input += new string(']', openSquare - closeSquare);
        }

        if (closeCurly < openCurly)
        {
            input += new string('}', openCurly - closeCurly);
        }

        return input;
    }
}

using System.Text.RegularExpressions;

namespace AIOrchestratorService.Application.Services;

/// <summary>
/// Default implementation of draft language auditor.
/// Checks for unsafe language patterns that claim approval, certification, or regulatory authority.
/// </summary>
public class DraftLanguageAuditor : IDraftLanguageAuditor
{
    private readonly ILogger<DraftLanguageAuditor> _logger;

    // Patterns that indicate claims of approval or certification
    private static readonly string[] ApprovalCertificationPatterns = new[]
    {
        @"\b(approved|approval|certified|certification|compliant|compliance|validated|validation)\b",
        @"\b(meets?\s+(all\s+)?requirements?|satisfies?\s+(all\s+)?requirements?)\b",
        @"\b(passed|passes?|fulfills?|fulfillment)\b",
        @"\b(conforms?\s+to|conformity|conformance)\b",
        @"\b(accredited|accreditation)\b",
        @"\b(guaranteed|guarantee|ensured|ensures?)\b"
    };

    // Patterns that imply regulatory authority
    private static readonly string[] RegulatoryAuthorityPatterns = new[]
    {
        @"\b(NABH|ISO|FDA|regulatory\s+authority|regulatory\s+body)\s+(requires?|mandates?|demands?|enforces?)\b",
        @"\b(required\s+by\s+(NABH|ISO|FDA|the\s+regulator|regulatory\s+authority))\b",
        @"\b(must\s+be\s+(approved|certified|validated)\s+by)\b",
        @"\b(official\s+(approval|certification|endorsement))\b",
        @"\b(regulatory\s+(mandate|requirement|standard))\b"
    };

    // Patterns that are too definitive (should be assistive/suggestive)
    private static readonly string[] TooDefinitivePatterns = new[]
    {
        @"\b(is\s+(required|mandatory|necessary|essential))\b",
        @"\b(must\s+(be|have|do|comply))\b",
        @"\b(shall\s+(be|have|do|comply))\b",
        @"\b(will\s+(be|have|do|comply))\b",
        @"\b(always|never|all\s+must|everyone\s+must)\b"
    };

    public DraftLanguageAuditor(ILogger<DraftLanguageAuditor> logger)
    {
        _logger = logger;
    }

    public DraftLanguageAuditResult Audit(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return DraftLanguageAuditResult.Safe();
        }

        var issues = new List<string>();
        var contentLower = content.ToLowerInvariant();

        // Check for approval/certification claims
        foreach (var pattern in ApprovalCertificationPatterns)
        {
            var matches = Regex.Matches(contentLower, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var context = ExtractContext(content, matches[0].Index, 50);
                issues.Add($"Possible approval/certification claim detected: \"{context}\"");
                _logger.LogWarning("Detected approval/certification language in draft");
            }
        }

        // Check for regulatory authority implications
        foreach (var pattern in RegulatoryAuthorityPatterns)
        {
            var matches = Regex.Matches(contentLower, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var context = ExtractContext(content, matches[0].Index, 50);
                issues.Add($"Possible regulatory authority implication: \"{context}\"");
                _logger.LogWarning("Detected regulatory authority language in draft");
            }
        }

        // Check for overly definitive language (should be assistive/suggestive)
        foreach (var pattern in TooDefinitivePatterns)
        {
            var matches = Regex.Matches(contentLower, pattern, RegexOptions.IgnoreCase);
            if (matches.Count > 0)
            {
                var context = ExtractContext(content, matches[0].Index, 50);
                issues.Add($"Overly definitive language (should be assistive/suggestive): \"{context}\"");
                _logger.LogWarning("Detected overly definitive language in draft");
            }
        }

        if (issues.Count > 0)
        {
            _logger.LogInformation("Unsafe language detected in draft. {IssueCount} issue(s) found.", issues.Count);
            return DraftLanguageAuditResult.Unsafe(issues);
        }

        return DraftLanguageAuditResult.Safe();
    }

    /// <summary>
    /// Extracts context around a match position for better error reporting.
    /// </summary>
    private static string ExtractContext(string content, int position, int contextLength)
    {
        var start = Math.Max(0, position - contextLength);
        var end = Math.Min(content.Length, position + contextLength);
        var context = content.Substring(start, end - start);
        
        // Clean up the context
        context = context.Replace('\n', ' ').Replace('\r', ' ');
        context = Regex.Replace(context, @"\s+", " ").Trim();
        
        if (start > 0) context = "..." + context;
        if (end < content.Length) context = context + "...";
        
        return context;
    }
}

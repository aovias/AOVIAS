using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using System.Text.RegularExpressions;

namespace ComplianceService.Application.Services;

/// <summary>
/// Validates Copilot responses for safety compliance.
/// Detects unsafe language and generates warning banners without modifying content.
/// </summary>
public class CopilotSafetyValidator : ICopilotSafetyValidator
{
    private readonly ILogger<CopilotSafetyValidator> _logger;

    // Patterns for detecting unsafe language
    private static readonly Regex ApprovalPatterns = new(
        @"\b(approved|certified|compliant|meets requirements|passes audit|fully compliant|in compliance|compliance certified)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AuditPredictionPatterns = new(
        @"\b(will pass|will fail|will be approved|will be rejected|audit will|auditor will approve|auditor will reject|guaranteed to pass|guaranteed to fail)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AuthorityPatterns = new(
        @"\b(we certify|we approve|we authorize|as a regulator|on behalf of|official certification|official approval|regulatory authority|we declare)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex NonAssistivePatterns = new(
        @"\b(must|shall|required by law|mandatory|legally required|you must|you shall|it is required|compliance is mandatory)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public CopilotSafetyValidator(ILogger<CopilotSafetyValidator> logger)
    {
        _logger = logger;
    }

    public CopilotSafetyResult Validate(CopilotResponse response)
    {
        if (response == null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        var result = new CopilotSafetyResult
        {
            IsSafe = true
        };

        // Validate summary text
        ValidateText(response.Summary, "Summary", result);

        // Validate suggested next steps
        foreach (var step in response.SuggestedNextSteps)
        {
            ValidateText(step.Description, "SuggestedNextStep", result);
        }

        // Validate findings descriptions
        foreach (var finding in response.Findings)
        {
            ValidateText(finding.Description, "Finding", result);
        }

        // Generate warning banner if violations found
        if (result.Violations.Count > 0)
        {
            result.IsSafe = false;
            result.WarningBanner = GenerateWarningBanner(result.Violations);
            _logger.LogWarning("Safety violations detected in Copilot response: {ViolationCount} violations", 
                result.Violations.Count);
        }

        return result;
    }

    private void ValidateText(string text, string context, CopilotSafetyResult result)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        // Check for approval/compliance implications
        var approvalMatches = ApprovalPatterns.Matches(text);
        if (approvalMatches.Count > 0)
        {
            foreach (Match match in approvalMatches)
            {
                result.Violations.Add(new SafetyViolation
                {
                    ViolationType = SafetyViolationType.ImpliesApprovalOrCompliance,
                    Description = $"Language in {context} may imply approval or compliance certification",
                    TriggeringText = match.Value,
                    Severity = SafetyViolationSeverity.High
                });
            }
        }

        // Check for audit outcome predictions
        var predictionMatches = AuditPredictionPatterns.Matches(text);
        if (predictionMatches.Count > 0)
        {
            foreach (Match match in predictionMatches)
            {
                result.Violations.Add(new SafetyViolation
                {
                    ViolationType = SafetyViolationType.PredictsAuditOutcome,
                    Description = $"Language in {context} may predict audit outcomes",
                    TriggeringText = match.Value,
                    Severity = SafetyViolationSeverity.High
                });
            }
        }

        // Check for regulator authority claims
        var authorityMatches = AuthorityPatterns.Matches(text);
        if (authorityMatches.Count > 0)
        {
            foreach (Match match in authorityMatches)
            {
                result.Violations.Add(new SafetyViolation
                {
                    ViolationType = SafetyViolationType.ClaimsRegulatorAuthority,
                    Description = $"Language in {context} may claim regulator authority",
                    TriggeringText = match.Value,
                    Severity = SafetyViolationSeverity.High
                });
            }
        }

        // Check for non-assistive language (too definitive)
        // Note: This is more lenient - only flags if combined with other indicators
        var nonAssistiveMatches = NonAssistivePatterns.Matches(text);
        if (nonAssistiveMatches.Count > 3) // Multiple instances suggest non-assistive tone
        {
            result.Violations.Add(new SafetyViolation
            {
                ViolationType = SafetyViolationType.NonAssistiveLanguage,
                Description = $"Language in {context} may be too definitive or non-assistive",
                TriggeringText = nonAssistiveMatches[0].Value,
                Severity = SafetyViolationSeverity.Medium
            });
        }
    }

    private string GenerateWarningBanner(List<SafetyViolation> violations)
    {
        var bannerParts = new List<string>();
        
        bannerParts.Add("⚠️ SAFETY WARNING");
        bannerParts.Add("");
        bannerParts.Add("This response contains language that may be unsafe or inappropriate:");
        bannerParts.Add("");

        // Group violations by type
        var violationsByType = violations.GroupBy(v => v.ViolationType);

        foreach (var group in violationsByType)
        {
            var violationType = group.Key switch
            {
                SafetyViolationType.ImpliesApprovalOrCompliance => "• Implies approval or compliance certification",
                SafetyViolationType.PredictsAuditOutcome => "• Predicts audit outcomes",
                SafetyViolationType.ClaimsRegulatorAuthority => "• Claims regulator authority",
                SafetyViolationType.NonAssistiveLanguage => "• Uses non-assistive or overly definitive language",
                _ => "• Safety concern"
            };

            bannerParts.Add(violationType);
            bannerParts.Add($"  ({group.Count()} instance(s) detected)");
        }

        bannerParts.Add("");
        bannerParts.Add("IMPORTANT:");
        bannerParts.Add("• This response does NOT constitute approval, certification, or regulatory authority");
        bannerParts.Add("• No audit outcomes are predicted or guaranteed");
        bannerParts.Add("• Human review is required before taking any action");
        bannerParts.Add("• The content has NOT been modified - review carefully");

        return string.Join("\n", bannerParts);
    }
}

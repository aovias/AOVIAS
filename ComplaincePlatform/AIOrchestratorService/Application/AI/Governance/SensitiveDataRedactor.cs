using System.Text.RegularExpressions;

namespace AIOrchestratorService.Application.AI.Governance;

/// <summary>
/// Masks common PII (email / phone) in prompts before they are sent to model providers.
/// </summary>
public static class SensitiveDataRedactor
{
    private static readonly Regex EmailPattern = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex PhonePattern = new(
        @"\b(?:\+?\d{1,3}[-.\s]?)?(?:\(?\d{2,4}\)?[-.\s]?)?\d{3,4}[-.\s]?\d{4}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex TenDigitPhonePattern = new(
        @"\b\d{10}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string Redact(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var redacted = EmailPattern.Replace(input, "[REDACTED_EMAIL]");
        redacted = PhonePattern.Replace(redacted, "[REDACTED_PHONE]");
        redacted = TenDigitPhonePattern.Replace(redacted, "[REDACTED_PHONE]");
        return redacted;
    }
}

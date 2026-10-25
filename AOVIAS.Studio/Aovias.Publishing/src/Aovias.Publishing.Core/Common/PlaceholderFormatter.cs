using System.Text.RegularExpressions;

namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Produces deterministic <c>${PLACEHOLDER}</c> tokens from configuration keys.
/// Shared by the configuration template generator and artifact sanitizer.
/// </summary>
public static partial class PlaceholderFormatter
{
    /// <summary>
    /// Converts a (possibly colon-delimited) configuration key into an
    /// upper-friendly placeholder name containing only alphanumeric characters.
    /// </summary>
    public static string ToPlaceholder(string key)
        => NonAlphanumeric().Replace(key.Split(':').Last(), string.Empty);

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex NonAlphanumeric();
}

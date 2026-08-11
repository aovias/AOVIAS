namespace ComplianceService.Application.Copilot.Models;

/// <summary>
/// Represents a single named section within a draft.
/// Each template defines the ordered list of sections it produces.
/// </summary>
public sealed class DraftSection
{
    /// <summary>Display heading for this section (e.g., "Problem Statement").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Markdown-formatted body content generated for this section.</summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>Indicates whether this section is required; empty required sections flag a validation issue.</summary>
    public bool IsRequired { get; init; } = true;

    /// <summary>Ordering within the draft (lower = earlier).</summary>
    public int Order { get; init; }
}

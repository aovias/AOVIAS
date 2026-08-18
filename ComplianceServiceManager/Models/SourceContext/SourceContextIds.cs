namespace ComplianceServiceManager.Models.SourceContext;

/// <summary>Canonical Studio source context identifiers.</summary>
public static class SourceContextIds
{
    public const string Shared = "shared";
    public const string Cursor = "cursor";
    public const string Claude = "claude";

    public static IReadOnlyList<string> All { get; } = [Shared, Cursor, Claude];
}

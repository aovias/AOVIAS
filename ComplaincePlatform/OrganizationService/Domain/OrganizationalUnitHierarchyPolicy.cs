namespace OrganizationService.Domain;

/// <summary>
/// Allowed physical facility hierarchy edges for OrganizationalUnit types.
/// Hospital/Department remain first-class entities outside this tree.
/// </summary>
public static class OrganizationalUnitHierarchyPolicy
{
    public static readonly IReadOnlyList<string> AllUnitTypeCodes =
    [
        "Campus", "Facility", "Building", "Floor", "Wing", "Unit", "Location", "WorkArea", "Team"
    ];

    private const string RootKey = "__ROOT__";

    /// <summary>Parent type code → allowed child type codes. RootKey = under organization.</summary>
    private static readonly IReadOnlyDictionary<string, HashSet<string>> AllowedChildren =
        new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [RootKey] = new(StringComparer.OrdinalIgnoreCase) { "Campus", "Facility", "Building", "Team" },
            ["Campus"] = new(StringComparer.OrdinalIgnoreCase) { "Facility", "Building", "Team" },
            ["Facility"] = new(StringComparer.OrdinalIgnoreCase) { "Building", "Unit", "Location", "WorkArea", "Team" },
            ["Building"] = new(StringComparer.OrdinalIgnoreCase) { "Floor", "Wing", "Unit", "Location", "WorkArea", "Team" },
            ["Floor"] = new(StringComparer.OrdinalIgnoreCase) { "Wing", "Unit", "Location", "WorkArea", "Team" },
            ["Wing"] = new(StringComparer.OrdinalIgnoreCase) { "Unit", "Location", "WorkArea", "Team" },
            ["Unit"] = new(StringComparer.OrdinalIgnoreCase) { "Location", "WorkArea", "Team" },
            ["Location"] = new(StringComparer.OrdinalIgnoreCase) { "WorkArea", "Team" },
            ["WorkArea"] = new(StringComparer.OrdinalIgnoreCase) { "Team" },
            ["Team"] = new(StringComparer.OrdinalIgnoreCase) { "Team" },
        };

    public static void EnsureAllowedParentChild(string? parentTypeCode, string childTypeCode)
    {
        if (string.IsNullOrWhiteSpace(childTypeCode))
            throw new InvalidOperationException("Unit type is required.");

        var parentKey = string.IsNullOrWhiteSpace(parentTypeCode) ? RootKey : parentTypeCode;
        if (!AllowedChildren.TryGetValue(parentKey, out var allowed) ||
            !allowed.Contains(childTypeCode))
        {
            var parentLabel = string.IsNullOrWhiteSpace(parentTypeCode) ? "Organization (root)" : parentTypeCode;
            throw new InvalidOperationException(
                $"Hierarchy rule violation: '{childTypeCode}' cannot be placed under '{parentLabel}'.");
        }
    }

    public static bool IsKnownType(string typeCode)
        => AllUnitTypeCodes.Contains(typeCode, StringComparer.OrdinalIgnoreCase);
}

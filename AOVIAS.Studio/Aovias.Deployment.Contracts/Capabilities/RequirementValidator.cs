namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// Validates a set of requirements before compilation or matching (Sprint M2.3 D9): unknown
/// requirement categories, duplicate requirement ids, and conflicting requirements (two
/// mandatory requirements in the same category demanding different exact versions).
/// </summary>
public static class RequirementValidator
{
    public static void Validate(IReadOnlyList<Requirement> requirements)
    {
        ArgumentNullException.ThrowIfNull(requirements);
        var errors = new List<string>();

        foreach (var requirement in requirements)
        {
            if (string.IsNullOrWhiteSpace(requirement.Id))
            {
                errors.Add("Requirement id is required.");
            }

            if (string.IsNullOrWhiteSpace(requirement.Category))
            {
                errors.Add($"Requirement '{requirement.Id}' must declare a category.");
            }
            else if (!CapabilityRegistry.IsKnownCategory(requirement.Category)
                     && !requirement.Compatibility.AcceptedCategories.Any(CapabilityRegistry.IsKnownCategory))
            {
                errors.Add($"Requirement '{requirement.Id}' declares unknown category '{requirement.Category}' — no registered capability could ever satisfy it.");
            }
        }

        foreach (var duplicate in requirements.Select(r => r.Id)
                     .Where(id => !string.IsNullOrWhiteSpace(id))
                     .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                     .Where(group => group.Count() > 1)
                     .Select(group => group.Key))
        {
            errors.Add($"Duplicate requirement id '{duplicate}'.");
        }

        errors.AddRange(DetectConflicts(requirements));

        if (errors.Count > 0)
        {
            throw new RequirementValidationException(errors);
        }
    }

    /// <summary>
    /// Two mandatory requirements in the same category with different exact ("==" or bare)
    /// version constraints can never both be satisfied by a single capability instance —
    /// a deterministic conflict, distinct from a capability simply being absent.
    /// </summary>
    private static IEnumerable<string> DetectConflicts(IReadOnlyList<Requirement> requirements)
    {
        var conflicts = new List<string>();
        var byCategory = requirements
            .Where(r => r.Mandatory && !string.IsNullOrWhiteSpace(r.Category))
            .GroupBy(r => r.Category, StringComparer.OrdinalIgnoreCase);

        foreach (var group in byCategory)
        {
            var exactVersions = group
                .Where(r => IsExactConstraint(r.VersionConstraint))
                .Select(r => (Requirement: r, Version: NormalizeExact(r.VersionConstraint!)))
                .ToList();

            for (var i = 0; i < exactVersions.Count; i++)
            {
                for (var j = i + 1; j < exactVersions.Count; j++)
                {
                    if (!string.Equals(exactVersions[i].Version, exactVersions[j].Version, StringComparison.OrdinalIgnoreCase))
                    {
                        conflicts.Add(
                            $"Requirements '{exactVersions[i].Requirement.Id}' and '{exactVersions[j].Requirement.Id}' both target category " +
                            $"'{group.Key}' but demand incompatible exact versions ('{exactVersions[i].Version}' vs '{exactVersions[j].Version}').");
                    }
                }
            }
        }

        return conflicts;
    }

    private static bool IsExactConstraint(string? constraint)
        => !string.IsNullOrWhiteSpace(constraint) && (constraint.StartsWith("==", StringComparison.Ordinal) || !HasOperatorPrefix(constraint));

    private static bool HasOperatorPrefix(string constraint)
        => constraint.StartsWith(">=", StringComparison.Ordinal) || constraint.StartsWith("<=", StringComparison.Ordinal)
           || constraint.StartsWith('>') || constraint.StartsWith('<') || constraint.StartsWith('^');

    private static string NormalizeExact(string constraint)
        => constraint.StartsWith("==", StringComparison.Ordinal) ? constraint[2..].Trim() : constraint.Trim();
}

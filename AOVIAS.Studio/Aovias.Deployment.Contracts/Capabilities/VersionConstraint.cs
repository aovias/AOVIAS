namespace Aovias.Deployment.Contracts.Capabilities;

/// <summary>
/// Evaluates the minimal version-constraint grammar <see cref="Requirement.VersionConstraint"/>
/// supports (Sprint M2.3 D1/D4): <c>&gt;=</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&lt;</c>, <c>==</c>,
/// <c>^</c> (same major version, greater-or-equal minor.patch), or a bare version treated as
/// <c>==</c>. Comparisons are numeric on the leading <c>major.minor.patch</c> components; a
/// version that cannot be parsed this way never satisfies a constraint, deterministically.
/// </summary>
public static class VersionConstraint
{
    public static bool IsSatisfiedBy(string? constraint, string candidateVersion)
    {
        if (string.IsNullOrWhiteSpace(constraint))
        {
            return true;
        }

        var trimmed = constraint.Trim();
        var (op, versionText) = ParseOperator(trimmed);

        if (!TryParse(versionText, out var required) || !TryParse(candidateVersion, out var candidate))
        {
            return false;
        }

        return op switch
        {
            ">=" => candidate.CompareTo(required) >= 0,
            "<=" => candidate.CompareTo(required) <= 0,
            ">" => candidate.CompareTo(required) > 0,
            "<" => candidate.CompareTo(required) < 0,
            "==" => candidate.CompareTo(required) == 0,
            "^" => candidate.Major == required.Major && candidate.CompareTo(required) >= 0,
            _ => false
        };
    }

    private static (string Operator, string Version) ParseOperator(string constraint)
    {
        foreach (var op in new[] { ">=", "<=", "==", "^", ">", "<" })
        {
            if (constraint.StartsWith(op, StringComparison.Ordinal))
            {
                return (op, constraint[op.Length..].Trim());
            }
        }

        return ("==", constraint);
    }

    private static bool TryParse(string version, out (int Major, int Minor, int Patch) parsed)
    {
        parsed = (0, 0, 0);
        var parts = version.Trim().Split('.', '-', '+');
        if (parts.Length == 0 || !int.TryParse(parts[0], out var major))
        {
            return false;
        }

        var minor = parts.Length > 1 && int.TryParse(parts[1], out var minorValue) ? minorValue : 0;
        var patch = parts.Length > 2 && int.TryParse(parts[2], out var patchValue) ? patchValue : 0;
        parsed = (major, minor, patch);
        return true;
    }

    private static int CompareTo(this (int Major, int Minor, int Patch) left, (int Major, int Minor, int Patch) right)
    {
        var majorCompare = left.Major.CompareTo(right.Major);
        if (majorCompare != 0)
        {
            return majorCompare;
        }

        var minorCompare = left.Minor.CompareTo(right.Minor);
        return minorCompare != 0 ? minorCompare : left.Patch.CompareTo(right.Patch);
    }
}

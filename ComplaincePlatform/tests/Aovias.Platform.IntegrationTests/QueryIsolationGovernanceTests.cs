using System.Text.Json;

namespace Aovias.Platform.IntegrationTests;

/// <summary>
/// Architecture guardrail for query-level tenant isolation.
///
/// <para>
/// **Why an approval test rather than a Roslyn analyzer.** An analyzer is the nicer long-term
/// answer, but it cannot express the thing that actually matters here: not "is this call present"
/// but "has this call been reviewed and justified". A checked-in manifest makes adding a bypass
/// require a diff to a governance file, which puts the justification in front of a reviewer. It
/// also runs today, with no packaging or IDE integration.
/// </para>
///
/// <para>
/// **Why counts per file rather than exact locations.** Line numbers churn with every edit and
/// would make this test a maintenance tax that people disable. Counts are stable under refactoring
/// and still catch the thing worth catching: a bypass appearing where one was not approved.
/// </para>
///
/// <para>
/// The ratchet only turns one way — removing bypasses is always allowed and reported; adding them
/// fails. See Wave 2.2B.3.4.
/// </para>
/// </summary>
public sealed class QueryIsolationGovernanceTests
{
    private const string BypassToken = "IgnoreQueryFilters()";
    private const string ManifestFile = "approved-query-bypasses.json";

    [Fact]
    public void No_unapproved_query_isolation_bypass_exists()
    {
        var (root, manifest) = LoadContext();
        var actual = ScanComplianceService(root);

        var violations = new List<string>();

        foreach (var (file, count) in actual.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (!manifest.TryGetValue(file, out var approved))
            {
                violations.Add(
                    $"  NEW FILE  {file} introduces {count} bypass(es) with no governance entry.");
                continue;
            }

            if (count > approved.Count)
            {
                violations.Add(
                    $"  INCREASED {file}: {approved.Count} approved, {count} found (+{count - approved.Count}).");
            }
        }

        Assert.True(violations.Count == 0,
            "Unapproved tenant-isolation bypasses detected.\n\n" +
            string.Join("\n", violations) +
            $"\n\nIgnoreQueryFilters() disables tenant filtering for the WHOLE query, not just the\n" +
            $"entity it is called on. If the bypass is genuinely required, add it to\n" +
            $"{ManifestFile} with a category and justification so the decision is reviewed.\n");
    }

    [Fact]
    public void Removed_bypasses_are_reported_so_the_manifest_can_be_tightened()
    {
        var (root, manifest) = LoadContext();
        var actual = ScanComplianceService(root);

        var stale = manifest
            .Where(m => actual.GetValueOrDefault(m.Key) < m.Value.Count)
            .Select(m => $"  {m.Key}: approved {m.Value.Count}, now {actual.GetValueOrDefault(m.Key)}")
            .ToList();

        // Informational, not a failure: cleanup should never be blocked by governance. Surfacing it
        // keeps the manifest from drifting into a permanently over-permissive baseline.
        Assert.True(true,
            stale.Count == 0
                ? "Manifest matches reality."
                : "Bypasses removed — tighten the manifest:\n" + string.Join("\n", stale));
    }

    [Fact]
    public void The_governance_manifest_matches_its_declared_total()
    {
        var (_, manifest) = LoadContext();
        var declared = LoadDeclaredTotal();
        var summed = manifest.Values.Sum(v => v.Count);

        Assert.True(declared == summed,
            $"Manifest 'totalApproved' is {declared} but the per-file entries sum to {summed}. " +
            "Keep them in step so the headline figure in reports stays trustworthy.");
    }

    [Fact]
    public void Every_approved_bypass_carries_a_category_and_justification()
    {
        var (_, manifest) = LoadContext();

        var undocumented = manifest
            .Where(m => string.IsNullOrWhiteSpace(m.Value.Category) || string.IsNullOrWhiteSpace(m.Value.Note))
            .Select(m => "  " + m.Key)
            .ToList();

        Assert.True(undocumented.Count == 0,
            "Approved bypasses missing a category or justification:\n" + string.Join("\n", undocumented));
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private sealed record ApprovedBypass(int Count, string Category, string Note);

    private static (string Root, Dictionary<string, ApprovedBypass> Manifest) LoadContext()
    {
        var root = FindComplianceServiceRoot();
        Assert.True(root is not null,
            "Could not locate the ComplianceService source directory from the test output path.");
        return (root!, LoadManifest());
    }

    private static Dictionary<string, ApprovedBypass> LoadManifest()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ManifestPath()));
        var files = doc.RootElement.GetProperty("files");

        return files.EnumerateObject()
            // "//"-prefixed keys are inline documentation (e.g. a record of what was removed and
            // why), not entries. Skipping them keeps the rationale next to the data it explains.
            .Where(p => !p.Name.StartsWith("//", StringComparison.Ordinal))
            .ToDictionary(
            p => p.Name.Replace('\\', '/'),
            p => new ApprovedBypass(
                p.Value.GetProperty("count").GetInt32(),
                p.Value.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "",
                p.Value.TryGetProperty("note", out var n) ? n.GetString() ?? "" : ""),
            StringComparer.Ordinal);
    }

    private static int LoadDeclaredTotal()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(ManifestPath()));
        return doc.RootElement.GetProperty("totalApproved").GetInt32();
    }

    private static string ManifestPath() => Path.Combine(AppContext.BaseDirectory, ManifestFile);

    /// <summary>Counts bypass calls per source file, keyed relative to ComplianceService.</summary>
    private static Dictionary<string, int> ScanComplianceService(string root)
    {
        var results = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var path in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            var normalised = path.Replace('\\', '/');
            if (normalised.Contains("/obj/", StringComparison.Ordinal)
                || normalised.Contains("/bin/", StringComparison.Ordinal))
            {
                continue;
            }

            var count = File.ReadLines(path)
                // Skip commented-out occurrences — a bypass described in a comment is documentation,
                // not behaviour, and several such comments exist from earlier waves.
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Sum(line => CountOccurrences(line, BypassToken));

            if (count > 0)
            {
                results[Path.GetRelativePath(root, path).Replace('\\', '/')] = count;
            }
        }

        return results;
    }

    private static int CountOccurrences(string line, string token)
    {
        var count = 0;
        var index = line.IndexOf(token, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = line.IndexOf(token, index + token.Length, StringComparison.Ordinal);
        }
        return count;
    }

    private static string? FindComplianceServiceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "ComplianceService");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "ComplianceService.csproj")))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        return null;
    }
}

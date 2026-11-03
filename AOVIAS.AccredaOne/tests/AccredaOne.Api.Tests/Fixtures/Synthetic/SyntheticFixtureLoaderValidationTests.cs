namespace AccredaOne.Api.Tests.Fixtures.Synthetic;

public class SyntheticFixtureLoaderValidationTests
{
    [Fact]
    public void LoadFromDirectory_throws_when_manifest_is_missing()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateEmpty();
        var ex = Assert.Throws<FileNotFoundException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("manifest.json", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_duplicate_ids()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var requirementsPath = Path.Combine(temp.Path, "requirements.json");
        var json = File.ReadAllText(requirementsPath);
        json = json.Replace("\"id\": \"SYN-REQ-NOAF-002\"", "\"id\": \"SYN-REQ-NOAF-001\"", StringComparison.Ordinal);
        File.WriteAllText(requirementsPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("Duplicate id", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_invalid_framework_reference_on_requirement()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var requirementsPath = Path.Combine(temp.Path, "requirements.json");
        var json = File.ReadAllText(requirementsPath);
        json = json.Replace("\"frameworkId\": \"SYN-FW-NOAF-001\"", "\"frameworkId\": \"SYN-FW-MISSING-001\"", StringComparison.Ordinal);
        File.WriteAllText(requirementsPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("references unknown framework", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("frameworks.json", "\"classification\": \"synthetic\"", "\"classification\": \"production\"")]
    [InlineData("sources.json", "\"classification\": \"synthetic\"", "\"classification\": \"production\"")]
    [InlineData("requirements.json", "\"classification\": \"synthetic\"", "\"classification\": \"production\"")]
    [InlineData("framework-versions.json", "\"classification\": \"synthetic\"", "\"classification\": \"production\"")]
    [InlineData("explanations.json", "\"classification\": \"synthetic\"", "\"classification\": \"production\"")]
    [InlineData("query-scenarios.json", "\"classification\": \"synthetic\"", "\"classification\": \"production\"")]
    public void LoadFromDirectory_rejects_non_synthetic_classification(string fileName, string from, string to)
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var path = Path.Combine(temp.Path, fileName);
        var json = File.ReadAllText(path);
        json = json.Replace(from, to, StringComparison.Ordinal);
        File.WriteAllText(path, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("must have classification 'synthetic'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_source_attached_to_disabled_framework()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var sourcesPath = Path.Combine(temp.Path, "sources.json");
        var json = File.ReadAllText(sourcesPath);
        json = json.Replace("\"frameworkId\": \"SYN-FW-MCQF-001\"", "\"frameworkId\": \"SYN-FW-LCF-001\"", StringComparison.Ordinal);
        File.WriteAllText(sourcesPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("must not attach to disabled framework", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_requirement_attached_to_disabled_framework()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var requirementsPath = Path.Combine(temp.Path, "requirements.json");
        var json = File.ReadAllText(requirementsPath);
        json = json.Replace("\"frameworkId\": \"SYN-FW-MCQF-001\"", "\"frameworkId\": \"SYN-FW-LCF-001\"", StringComparison.Ordinal);
        File.WriteAllText(requirementsPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("must not attach to disabled framework", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_version_attached_to_disabled_framework()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var versionsPath = Path.Combine(temp.Path, "framework-versions.json");
        var json = File.ReadAllText(versionsPath);
        json = json.Replace(
            """
            "id": "SYN-VER-NOAF-HIST-001",
                "frameworkId": "SYN-FW-NOAF-001",
            """,
            """
            "id": "SYN-VER-NOAF-HIST-001",
                "frameworkId": "SYN-FW-LCF-001",
            """,
            StringComparison.Ordinal);
        File.WriteAllText(versionsPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("must not attach to disabled framework", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_explanation_attached_to_disabled_framework()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var explanationsPath = Path.Combine(temp.Path, "explanations.json");
        var json = File.ReadAllText(explanationsPath);
        json = json.Replace("\"frameworkId\": \"SYN-FW-MCQF-001\"", "\"frameworkId\": \"SYN-FW-LCF-001\"", StringComparison.Ordinal);
        File.WriteAllText(explanationsPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("must not attach to disabled framework", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_invalid_query_scenario_outcome_mismatch()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var scenariosPath = Path.Combine(temp.Path, "query-scenarios.json");
        var json = File.ReadAllText(scenariosPath);
        json = json.Replace(
            """
            "frameworkCode": "NOAF",
                "classification": "synthetic",
                "provenance": "Fictional query scenario for boundary verification",
                "expectedOutcome": "bounded_results"
              },
              {
                "id": "SYN-QRY-002"
            """,
            """
            "frameworkCode": "NOAF",
                "classification": "synthetic",
                "provenance": "Fictional query scenario for boundary verification",
                "expectedOutcome": "no_results"
              },
              {
                "id": "SYN-QRY-002"
            """,
            StringComparison.Ordinal);
        File.WriteAllText(scenariosPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("does not match scenarioType/frameworkCode contract", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_invalid_manifest_classification()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var manifestPath = Path.Combine(temp.Path, "manifest.json");
        var json = File.ReadAllText(manifestPath);
        json = json.Replace("\"classification\": \"synthetic\"", "\"classification\": \"production\"", StringComparison.Ordinal);
        File.WriteAllText(manifestPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("must have classification 'synthetic'", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadFromDirectory_rejects_no_framework_selection_with_framework_code()
    {
        using var temp = SyntheticFixtureTestDirectory.CreateFromCanonical();
        var scenariosPath = Path.Combine(temp.Path, "query-scenarios.json");
        var json = File.ReadAllText(scenariosPath);
        json = json.Replace(
            """
            "id": "SYN-QRY-003",
                "scenarioType": "no_framework_selection",
                "frameworkCode": null
            """,
            """
            "id": "SYN-QRY-003",
                "scenarioType": "no_framework_selection",
                "frameworkCode": "NOAF"
            """,
            StringComparison.Ordinal);
        File.WriteAllText(scenariosPath, json);

        var ex = Assert.Throws<InvalidOperationException>(() => SyntheticFixtureLoader.LoadFromDirectory(temp.Path));
        Assert.Contains("no_framework_selection must not include frameworkCode", ex.Message, StringComparison.Ordinal);
    }
}

internal sealed class SyntheticFixtureTestDirectory : IDisposable
{
    public string Path { get; }

    private SyntheticFixtureTestDirectory(string path) => Path = path;

    public static SyntheticFixtureTestDirectory CreateEmpty()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "accredaone-dev06a-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return new SyntheticFixtureTestDirectory(path);
    }

    public static SyntheticFixtureTestDirectory CreateFromCanonical()
    {
        var canonical = ResolveCanonicalFixtureDirectory();
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "accredaone-dev06a-" + Guid.NewGuid().ToString("N"));
        CopyDirectory(canonical, path);
        return new SyntheticFixtureTestDirectory(path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Best-effort temp cleanup for test runs.
        }
    }

    private static string ResolveCanonicalFixtureDirectory()
    {
        var fromOutput = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "Synthetic");
        if (Directory.Exists(fromOutput))
        {
            return fromOutput;
        }

        var fromProject = System.IO.Path.GetFullPath(
            System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Fixtures", "Synthetic"));
        if (Directory.Exists(fromProject))
        {
            return fromProject;
        }

        throw new DirectoryNotFoundException("Canonical synthetic fixture directory was not found for tests.");
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            var name = System.IO.Path.GetFileName(file);
            if (name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(file, System.IO.Path.Combine(destination, name), overwrite: true);
        }
    }
}

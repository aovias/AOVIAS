namespace ComplianceServiceManager.Operations.Tests;

internal static class TestPaths
{
    /// <summary>A fresh, unique temp directory per call, so <see cref="OperationHistoryStore"/> tests never share state across test methods/runs.</summary>
    public static string NewTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "operation-engine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}

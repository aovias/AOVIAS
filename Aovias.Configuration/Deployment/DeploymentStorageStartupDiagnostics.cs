namespace Aovias.Configuration.Deployment;

/// <summary>
/// Writes deployment storage startup diagnostics to the bootstrap console.
/// </summary>
public static class DeploymentStorageStartupDiagnostics
{
    public static void WriteReport(TextWriter writer, DeploymentStorageValidationResult result)
    {
        writer.WriteLine();
        writer.WriteLine("Deployment Storage Layout");
        writer.WriteLine(new string('-', 40));

        foreach (var check in result.Checks)
        {
            writer.WriteLine(check.Label);
            writer.WriteLine(check.Status);
            writer.WriteLine(check.ResolvedPath);
            if (!string.IsNullOrWhiteSpace(check.Detail))
            {
                writer.WriteLine(check.Detail);
            }

            writer.WriteLine();
        }

        if (!result.IsValid)
        {
            writer.WriteLine($"Startup blocked: {result.FailureMessage}");
        }
    }
}

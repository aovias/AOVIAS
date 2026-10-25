using System.Text;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.AI;

public sealed class CursorPromptBuilder : ICursorPromptBuilder
{
    private const int MaxFailuresInPrompt = 12;
    private const int MaxAlreadyFixedInPrompt = 10;

    public string BuildPrompt(ServiceInfoModel service, DiagnosticBundle bundle)
    {
        var builder = new StringBuilder();

        builder.AppendLine("SYSTEM:");
        builder.AppendLine("You are a Senior .NET Distributed Systems Engineer working on the PRAMAN Healthcare Compliance Platform.");
        builder.AppendLine("Diagnose and FIX the runtime errors below for a single microservice. Make minimal, targeted code/config changes.");
        builder.AppendLine();

        builder.AppendLine("FAILED SERVICE:");
        builder.AppendLine(service.Name);
        builder.AppendLine();

        builder.AppendLine("ERROR CATEGORY:");
        builder.AppendLine(bundle.FailureCategory.ToString());
        builder.AppendLine();

        builder.AppendLine("PRIMARY LOG FILE (target for analysis):");
        builder.AppendLine(bundle.PrimaryLogFile ?? "(no structured .clef log file found)");
        builder.AppendLine($"Last successful AI fix for this service: {(bundle.LastFixedAt is null ? "never" : bundle.LastFixedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))}");
        builder.AppendLine();

        AppendUnresolvedFailures(builder, bundle);
        AppendAlreadyFixed(builder, bundle);

        builder.AppendLine("FULL DIAGNOSTICS (files):");
        builder.AppendLine($"- Captured logs: {bundle.LogsPath}");
        builder.AppendLine($"- Failure summary: {bundle.SummaryPath}");
        builder.AppendLine($"- Git context: {bundle.GitDiffPath}");
        builder.AppendLine($"- Environment: {bundle.EnvironmentPath}");
        builder.AppendLine();

        builder.AppendLine("SOLUTION STRUCTURE:");
        builder.AppendLine($"- Service project path: {service.ProjectPath}");
        builder.AppendLine($"- Working directory: {service.WorkingDirectory}");
        builder.AppendLine();

        builder.AppendLine("TASK:");
        builder.AppendLine("1. Focus ONLY on the errors listed under 'ERRORS THAT STILL NEED FIXING'.");
        builder.AppendLine("2. Do NOT re-fix anything under 'ERRORS ALREADY FIXED' unless it reappears with a newer timestamp.");
        builder.AppendLine("3. Identify the root cause for each unresolved error using the primary log file and timestamps.");
        builder.AppendLine("4. Apply a minimal, correct fix while preserving architecture and not breaking other services.");
        builder.AppendLine("5. Rebuild the solution and validate the service health endpoint.");
        builder.AppendLine("6. Return a concise summary of each change and which error it resolves.");
        builder.AppendLine();

        builder.AppendLine("CONSTRAINTS:");
        builder.AppendLine("- .NET 10 / ASP.NET Core (platform roadmap)");
        builder.AppendLine("- Clean Architecture, DDD, MediatR, microservices");
        builder.AppendLine("- SQL Server / Cosmos DB, YARP Gateway, Serilog structured logging");

        return builder.ToString();
    }

    private static void AppendUnresolvedFailures(StringBuilder builder, DiagnosticBundle bundle)
    {
        builder.AppendLine("ERRORS THAT STILL NEED FIXING (most recent first):");
        if (bundle.UnresolvedFailures.Count == 0)
        {
            builder.AppendLine("(No unresolved structured errors detected. Use the captured logs/last action below to diagnose.)");
            builder.AppendLine($"Last action observed by manager: {bundle.FailureSummary}");
            builder.AppendLine();
            return;
        }

        var index = 1;
        foreach (var failure in bundle.UnresolvedFailures.Take(MaxFailuresInPrompt))
        {
            builder.AppendLine($"[{index++}] {failure.DisplayTime} | {failure.LevelText} | EventCode={failure.EventCode ?? "-"} | Category={failure.Category ?? "-"}");
            if (!string.IsNullOrWhiteSpace(failure.CorrelationId))
            {
                builder.AppendLine($"    CorrelationId: {failure.CorrelationId}");
            }

            builder.AppendLine($"    Message: {failure.Message}");
            if (!string.IsNullOrWhiteSpace(failure.Exception))
            {
                builder.AppendLine("    Exception:");
                foreach (var line in failure.Exception!.Split('\n').Take(12))
                {
                    builder.AppendLine($"      {line.TrimEnd('\r')}");
                }
            }

            builder.AppendLine($"    Fingerprint: {failure.Fingerprint}");
        }

        if (bundle.UnresolvedFailures.Count > MaxFailuresInPrompt)
        {
            builder.AppendLine($"... and {bundle.UnresolvedFailures.Count - MaxFailuresInPrompt} more in {bundle.LogsPath}");
        }

        builder.AppendLine();
    }

    private static void AppendAlreadyFixed(StringBuilder builder, DiagnosticBundle bundle)
    {
        if (bundle.PreviouslyFixedFailures.Count == 0)
        {
            return;
        }

        builder.AppendLine("ERRORS ALREADY FIXED (do not repeat work unless they recur with a newer timestamp):");
        foreach (var failure in bundle.PreviouslyFixedFailures.Take(MaxAlreadyFixedInPrompt))
        {
            builder.AppendLine($"- {failure.DisplayTime} | EventCode={failure.EventCode ?? "-"} | {failure.Message}");
        }

        builder.AppendLine();
    }
}

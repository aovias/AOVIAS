using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Reporting;

/// <summary>
/// Derives the explicit quality gate set (D13) from the publishing context,
/// pipeline timings and deployment readiness report. Publishing only reaches
/// Deployment Ready when every non-skipped gate passes.
/// </summary>
public static class QualityGateEvaluator
{
    public static QualityGateReport Evaluate(PublishingContext context)
    {
        var report = context.ReadinessReport;
        var stageByName = report?.StageResults
            .ToDictionary(stage => stage.StageName, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ValidationStageResult>(StringComparer.OrdinalIgnoreCase);

        var gates = new List<QualityGate>
        {
            FromTiming(context, "Build", "Build"),
            FromTiming(context, "Docker", "Build Docker Image", "Export Docker Image"),
            PackageGate(context),
            FromStage("Compose", stageByName, "Docker Compose"),
            FromStage("Environment", stageByName, "Environment Neutral"),
            ManifestGate(stageByName),
            ChecksumsGate(context),
            FromStage("Simulation", stageByName, "Runtime"),
            FromStage("Health", stageByName, "Health"),
            ApiGate(context),
            FromStage("Bootstrap", stageByName, "Bootstrap")
        };

        return new QualityGateReport { Gates = gates };
    }

    private static QualityGate FromTiming(PublishingContext context, string gateName, params string[] stepNames)
    {
        var timings = context.StageTimings
            .Where(timing => stepNames.Any(name => timing.StageName.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (timings.Count == 0)
        {
            return new QualityGate(gateName, QualityGateStatus.Skipped, "Stage not executed.");
        }

        var failed = timings.Where(timing => !timing.Succeeded).ToList();
        return failed.Count == 0
            ? new QualityGate(gateName, QualityGateStatus.Passed, $"Completed in {timings.Sum(t => t.DurationMs):0} ms.")
            : new QualityGate(gateName, QualityGateStatus.Failed, $"{failed.Count} step(s) failed.");
    }

    private static QualityGate FromStage(string gateName, IReadOnlyDictionary<string, ValidationStageResult> stages, string stageName)
    {
        if (!stages.TryGetValue(stageName, out var stage))
        {
            return new QualityGate(gateName, QualityGateStatus.Skipped, "Stage not evaluated.");
        }

        return stage.Passed
            ? new QualityGate(gateName, QualityGateStatus.Passed, stage.Warnings.Count > 0 ? $"Passed with {stage.Warnings.Count} warning(s)." : "Passed.")
            : new QualityGate(gateName, QualityGateStatus.Failed, stage.DiagnosticsSummary);
    }

    private static QualityGate ManifestGate(IReadOnlyDictionary<string, ValidationStageResult> stages)
        => FromStage("Manifest", stages, "Package");

    private static QualityGate PackageGate(PublishingContext context)
    {
        var verification = context.PackageVerification;
        if (verification is null)
        {
            return new QualityGate("Package", QualityGateStatus.Skipped, "Package not verified.");
        }

        return verification.IsValid
            ? new QualityGate("Package", QualityGateStatus.Passed, $"{verification.VerifiedFiles.Count} file(s) verified.")
            : new QualityGate("Package", QualityGateStatus.Failed, string.Join("; ", verification.Errors));
    }

    private static QualityGate ChecksumsGate(PublishingContext context)
    {
        var verification = context.PackageVerification;
        if (verification is null)
        {
            return new QualityGate("Checksums", QualityGateStatus.Skipped, "Checksums not verified.");
        }

        return verification.IsValid
            ? new QualityGate("Checksums", QualityGateStatus.Passed, $"{verification.ChecksumCount} checksum(s) consistent.")
            : new QualityGate("Checksums", QualityGateStatus.Failed, "Checksum verification failed.");
    }

    private static QualityGate ApiGate(PublishingContext context)
    {
        var probes = context.ReadinessReport?.ApiProbes ?? [];
        if (probes.Count == 0)
        {
            return new QualityGate("API", QualityGateStatus.Skipped, "No API probes executed.");
        }

        var failed = probes.Where(probe => !probe.Succeeded).ToList();
        return failed.Count == 0
            ? new QualityGate("API", QualityGateStatus.Passed, $"{probes.Count} endpoint(s) verified.")
            : new QualityGate("API", QualityGateStatus.Failed, string.Join("; ", failed.Select(probe => $"{probe.Method} {probe.Endpoint} -> {probe.StatusCode}")));
    }
}

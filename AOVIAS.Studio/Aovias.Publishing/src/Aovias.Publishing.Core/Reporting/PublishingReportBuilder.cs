using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Common;

namespace Aovias.Publishing.Core.Reporting;

/// <summary>
/// Builds the comprehensive <see cref="PublishingReport"/> (D11) from the
/// publishing context and the deployment readiness report.
/// </summary>
public static class PublishingReportBuilder
{
    public static PublishingReport Build(
        PublishingContext context,
        string profile,
        DeploymentReadinessReport? validation,
        int checksumCount)
    {
        var manifest = context.Manifest;
        var metadata = context.Metadata;
        var totalDuration = context.StageTimings.Aggregate(TimeSpan.Zero, (sum, stage) => sum + stage.Duration);

        return new PublishingReport
        {
            Service = manifest?.Service ?? context.Request.ServiceId ?? "unknown",
            Version = manifest?.Version ?? context.Request.Version ?? PublishingVersionPolicy.DefaultPackageVersion,
            Profile = profile,
            GeneratedUtc = DateTimeOffset.UtcNow,
            PublishingEngineVersion = metadata?.PublishingEngineVersion ?? PublishingVersionPolicy.EngineVersion,
            DeploymentReady = validation?.IsReady ?? false,
            TotalDuration = totalDuration,
            StageTimings = context.StageTimings.ToList(),
            ChecksumCount = checksumCount,
            PackageVerified = context.PackageVerification?.IsValid ?? false,
            Validation = validation,
            QualityGates = QualityGateEvaluator.Evaluate(context),
            ApiProbes = validation?.ApiProbes ?? [],
            Warnings = context.Warnings.ToList(),
            Recommendations = validation?.Recommendations ?? [],
            SourceRevision = context.BuildIdentity?.SourceRevision ?? metadata?.SourceRevision,
            PublishingInputFingerprint = context.BuildIdentity?.PublishingInputFingerprint ?? metadata?.PublishingInputFingerprint,
            PackageFingerprint = context.BuildIdentity?.PackageFingerprint ?? metadata?.PackageFingerprint,
            DockerImageIdentity = context.BuildIdentity?.DockerImageIdentity ?? metadata?.DockerImageIdentity,
            FingerprintPolicyVersion = context.BuildIdentity?.FingerprintPolicyVersion ?? metadata?.FingerprintPolicyVersion ?? DeployPackageMetadata.FingerprintPolicyVersionDefault
        };
    }
}

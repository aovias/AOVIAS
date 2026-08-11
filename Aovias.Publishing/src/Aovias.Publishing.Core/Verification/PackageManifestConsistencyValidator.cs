using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;

namespace Aovias.Publishing.Core.Verification;

/// <summary>
/// Sprint M3.3 — cross-checks a published package's declared identity (manifest.json) against
/// the identity the platform intended to publish. Nothing in the existing pipeline previously
/// proved this post-hoc: <see cref="Pipeline.PublishingStageExecutor"/> generates the manifest
/// from the same in-memory <c>PublishingContext</c> it later packages, so a manifest/package
/// mismatch could only occur from a bug in that shared state — but no test read the manifest
/// back out of the finished archive and compared it, which is exactly the class of defect a
/// "manifest consistency" verification stage exists to catch.
/// </summary>
public static class PackageManifestConsistencyValidator
{
    public static VerificationStageStatus Validate(
        DeployPackageContents contents,
        string expectedServiceId,
        string expectedVersion)
    {
        var errors = new List<string>();

        if (!string.Equals(contents.Manifest.Service, expectedServiceId, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Manifest service '{contents.Manifest.Service}' does not match expected service '{expectedServiceId}'.");
        }

        if (!string.Equals(contents.Manifest.Version, expectedVersion, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Manifest version '{contents.Manifest.Version}' does not match expected version '{expectedVersion}'.");
        }

        if (contents.Checksums.Count == 0)
        {
            errors.Add("Manifest/package has zero checksum entries; package contents cannot be verified.");
        }

        if (string.IsNullOrWhiteSpace(contents.Manifest.EntryPoint))
        {
            errors.Add("Manifest is missing an entry point.");
        }

        return errors.Count == 0
            ? VerificationStageStatus.Passed(
                $"Manifest identity consistent ({contents.Manifest.Service} v{contents.Manifest.Version}).",
                VerificationEvidenceTier.StaticVerification)
            : VerificationStageStatus.Failed(
                "Manifest consistency check failed.",
                string.Join("; ", errors),
                VerificationEvidenceTier.StaticVerification,
                errors);
    }
}

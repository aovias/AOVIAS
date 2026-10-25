using System.Collections.Concurrent;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Core.Packaging;

namespace Aovias.Publishing.Core.ReleaseLibrary;

/// <summary>
/// The single authoritative Release Library implementation (Sprint M1.5). See
/// <see cref="IReleaseLibraryService"/> for the ownership contract of each method.
/// </summary>
public sealed class ReleaseLibraryService : IReleaseLibraryService
{
    private readonly IDeployPackageReader packageReader;
    private readonly IDeployPackageVerifier packageVerifier;

    private readonly ConcurrentDictionary<string, SemaphoreSlim> rootGates = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ReleaseIndexDocument> cachedIndexes = new(StringComparer.OrdinalIgnoreCase);

    public ReleaseLibraryService(IDeployPackageReader packageReader, IDeployPackageVerifier packageVerifier)
    {
        this.packageReader = packageReader;
        this.packageVerifier = packageVerifier;
    }

    public static string BuildArtifactId(string serviceId, string version)
        => $"{serviceId.ToLowerInvariant()}-{version}";

    public Task LoadAsync(string releaseLibraryRoot, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoot(releaseLibraryRoot);
        cachedIndexes[normalized] = ReleaseIndexStore.Load(normalized);
        return Task.CompletedTask;
    }

    // -----------------------------------------------------------------
    // Registration and lifecycle transitions
    // -----------------------------------------------------------------

    public async Task<ReleaseArtifactRecord> RegisterPublishedAsync(
        string releaseLibraryRoot,
        ReleaseArtifactRegistration registration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(registration);
        var normalized = NormalizeRoot(releaseLibraryRoot);
        var gate = GetGate(normalized);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var index = ReleaseIndexStore.Load(normalized);
            var artifactId = BuildArtifactId(registration.ServiceId, registration.Version);

            var existing = FindInIndex(index, artifactId);
            if (existing is not null && !string.Equals(existing.PublishingFingerprint, registration.PublishingFingerprint, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Sprint M1.5 D10 violation: duplicate artifact ID '{artifactId}' with a different publishing fingerprint. "
                    + $"Existing='{existing.PublishingFingerprint}', new='{registration.PublishingFingerprint}'. "
                    + "A version must never be re-registered with different content; publish a new version instead.");
            }

            var collision = index.Artifacts.FirstOrDefault(candidate =>
                string.Equals(candidate.PublishingFingerprint, registration.PublishingFingerprint, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(candidate.ServiceId, registration.ServiceId, StringComparison.OrdinalIgnoreCase));
            if (collision is not null)
            {
                throw new InvalidOperationException(
                    $"Sprint M1.5 D10 violation: publishing fingerprint '{registration.PublishingFingerprint}' is already registered "
                    + $"under service '{collision.ServiceId}' ({collision.ArtifactId}), but this registration is for '{registration.ServiceId}'.");
            }

            if (!File.Exists(registration.PackagePath))
            {
                throw new FileNotFoundException("Published package was not found.", registration.PackagePath);
            }

            var categoryDirectory = Path.Combine(normalized, registration.ReleaseLibraryCategory);
            Directory.CreateDirectory(categoryDirectory);
            var fileName = $"{registration.ServiceId.ToLowerInvariant()}-{registration.Version}{DeployPackageLayout.Extension}";
            var targetPath = Path.Combine(categoryDirectory, fileName);
            File.Copy(registration.PackagePath, targetPath, overwrite: true);

            var packageHash = PublishingContentHasher.HashFile(targetPath);
            var packageSize = new FileInfo(targetPath).Length;
            var relativePath = Path.GetRelativePath(normalized, targetPath).Replace('\\', '/');

            ReleaseArtifactRecord record;
            if (existing is not null)
            {
                // Idempotent re-registration of identical content: refresh storage
                // facts only, never regress lifecycle/promotion state.
                record = existing with
                {
                    PackageHash = packageHash,
                    PackageSizeBytes = packageSize,
                    RelativePackagePath = relativePath
                };
            }
            else
            {
                record = new ReleaseArtifactRecord
                {
                    ArtifactId = artifactId,
                    ServiceId = registration.ServiceId,
                    Service = registration.ServiceDisplayName,
                    Version = registration.Version,
                    BuildIdentity = $"{registration.SourceRevision}@{registration.BuildUtc:O}",
                    SourceRevision = registration.SourceRevision,
                    PublishingFingerprint = registration.PublishingFingerprint,
                    PackageHash = packageHash,
                    PackageSizeBytes = packageSize,
                    GeneratedUtc = DateTimeOffset.UtcNow,
                    Solution = registration.Solution ?? "AOVIAS-Platform",
                    PlatformVersion = registration.PlatformVersion,
                    ContractVersion = registration.ContractVersion,
                    PublishingEngineVersion = registration.PublishingEngineVersion,
                    SupportedRuntimeVersion = registration.SupportedRuntimeVersion ?? "net10.0",
                    DeploymentCompatibility =
                        $"contract:{registration.ContractVersion};engine:{registration.PublishingEngineVersion};fingerprint-policy:{registration.FingerprintPolicyVersion}",
                    ReleaseLibraryCategory = registration.ReleaseLibraryCategory,
                    RelativePackagePath = relativePath,
                    Dependencies = registration.Dependencies,
                    LifecycleState = ArtifactLifecycleState.Building
                }.WithTransition(ArtifactLifecycleState.Published, "Publishing");
            }

            return Persist(normalized, index, record);
        }
        finally
        {
            gate.Release();
        }
    }

    public Task<ReleaseArtifactRecord> AdvanceToVerifiedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Verified, "PackageVerification", cancellationToken,
            record => record with { PromotionStatus = MaxPromotion(record.PromotionStatus, ArtifactPromotionStatus.Verified) });

    public Task<ReleaseArtifactRecord> AdvanceToCertifiedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Certified, "DeploymentCertification", cancellationToken,
            record => record with
            {
                IsCertified = true,
                CertifiedUtc = DateTimeOffset.UtcNow,
                IsProtected = true,
                PromotionStatus = MaxPromotion(record.PromotionStatus, ArtifactPromotionStatus.Certified)
            });

    public Task<ReleaseArtifactRecord> AdvanceToReadyAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Ready, "ReleaseLibrary", cancellationToken);

    public Task<ReleaseArtifactRecord> MarkDeployedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Deployed, "RuntimeAgent", cancellationToken,
            record => record with { IsProtected = true });

    public Task<ReleaseArtifactRecord> MarkPromotedAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Promoted, "PromotionWorkflow", cancellationToken,
            record => record with
            {
                IsProtected = true,
                PromotionStatus = MaxPromotion(record.PromotionStatus, ArtifactPromotionStatus.ProductionApproved)
            });

    public Task<ReleaseArtifactRecord> MarkDeprecatedAsync(string releaseLibraryRoot, string artifactId, string reason, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Deprecated, "ReleaseLibrary", cancellationToken,
            record => record with { PromotionStatus = ArtifactPromotionStatus.Deprecated }, reason);

    public Task<ReleaseArtifactRecord> ArchiveAsync(string releaseLibraryRoot, string artifactId, string reason, CancellationToken cancellationToken = default)
        => TransitionAsync(releaseLibraryRoot, artifactId, ArtifactLifecycleState.Archived, "RetentionPolicy", cancellationToken, reason: reason);

    private static ArtifactPromotionStatus MaxPromotion(ArtifactPromotionStatus current, ArtifactPromotionStatus candidate)
        => candidate > current ? candidate : current;

    private async Task<ReleaseArtifactRecord> TransitionAsync(
        string releaseLibraryRoot,
        string artifactId,
        ArtifactLifecycleState toState,
        string owner,
        CancellationToken cancellationToken,
        Func<ReleaseArtifactRecord, ReleaseArtifactRecord>? additionalChanges = null,
        string? reason = null)
    {
        var normalized = NormalizeRoot(releaseLibraryRoot);
        var gate = GetGate(normalized);
        await gate.WaitAsync(cancellationToken);
        try
        {
            var index = ReleaseIndexStore.Load(normalized);
            var existing = FindInIndex(index, artifactId)
                ?? throw new InvalidOperationException(
                    $"Artifact '{artifactId}' is not registered in the Release Library at '{normalized}'.");

            ArtifactLifecycleTransitions.EnsureValidTransition(existing.LifecycleState, toState);

            var updated = existing.WithTransition(toState, owner, reason);
            if (additionalChanges is not null)
            {
                updated = additionalChanges(updated);
            }

            return Persist(normalized, index, updated);
        }
        finally
        {
            gate.Release();
        }
    }

    // -----------------------------------------------------------------
    // Compatibility facade for the pre-M1.5 organizer / CSM copy call sites
    // -----------------------------------------------------------------

    public async Task<string> RegisterOrganizedCopyAsync(
        string releaseLibraryRoot,
        DeployableProject project,
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        var contents = await ReadPackageOrThrowAsync(packagePath, cancellationToken);
        return await RegisterOrganizedCopyCoreAsync(
            releaseLibraryRoot,
            contents,
            packagePath,
            serviceId: project.ServiceId,
            serviceDisplayName: project.Name,
            releaseLibraryCategory: project.ReleaseLibraryCategory,
            cancellationToken);
    }

    public async Task<string> RegisterOrganizedCopyAsync(
        string releaseLibraryRoot,
        string packagePath,
        CancellationToken cancellationToken = default)
    {
        var contents = await ReadPackageOrThrowAsync(packagePath, cancellationToken);

        // No discovered DeployableProject is available at this call site (e.g. the
        // CSM ad hoc "add finished package to Release Library" action). Every value
        // below is read from the package's own manifest — nothing is invented and
        // nothing duplicates a value the Publishing pipeline already computed.
        return await RegisterOrganizedCopyCoreAsync(
            releaseLibraryRoot,
            contents,
            packagePath,
            serviceId: contents.Manifest.Service,
            serviceDisplayName: contents.Manifest.Service,
            releaseLibraryCategory: "Services",
            cancellationToken);
    }

    private async Task<DeployPackageContents> ReadPackageOrThrowAsync(string packagePath, CancellationToken cancellationToken)
        => await packageReader.ReadAsync(packagePath, cancellationToken)
            ?? throw new InvalidOperationException($"Unable to read deployment package '{packagePath}' for Release Library registration.");

    private async Task<string> RegisterOrganizedCopyCoreAsync(
        string releaseLibraryRoot,
        DeployPackageContents contents,
        string packagePath,
        string serviceId,
        string serviceDisplayName,
        string releaseLibraryCategory,
        CancellationToken cancellationToken)
    {
        var registration = new ReleaseArtifactRegistration
        {
            ServiceId = serviceId,
            ServiceDisplayName = serviceDisplayName,
            Version = contents.Manifest.Version,
            PackagePath = packagePath,
            SourceRevision = contents.Metadata.SourceRevision ?? "unknown",
            BuildUtc = contents.Metadata.CreatedUtc,
            PublishingFingerprint = contents.Metadata.PackageFingerprint ?? contents.Metadata.PublishingInputFingerprint ?? contents.Metadata.CreatedUtc.ToString("O"),
            ContractVersion = Aovias.Deployment.Contracts.Building.ServiceContractBuilder.ContractVersion,
            PublishingEngineVersion = contents.Metadata.PublishingEngineVersion,
            FingerprintPolicyVersion = contents.Metadata.FingerprintPolicyVersion,
            ReleaseLibraryCategory = releaseLibraryCategory,
            Dependencies = contents.Manifest.Dependencies
        };

        var record = await RegisterPublishedAsync(releaseLibraryRoot, registration, cancellationToken);

        if (contents.ReadinessCertificate is not null && record.LifecycleState < ArtifactLifecycleState.Certified)
        {
            record = await AdvanceToCertifiedAsync(releaseLibraryRoot, record.ArtifactId, cancellationToken);
        }

        return Path.Combine(NormalizeRoot(releaseLibraryRoot), record.RelativePackagePath);
    }

    // -----------------------------------------------------------------
    // Integrity (D5)
    // -----------------------------------------------------------------

    public async Task<ReleaseIntegrityReport> VerifyIntegrityAsync(string releaseLibraryRoot, string artifactId, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoot(releaseLibraryRoot);
        var index = GetOrLoad(normalized);
        var record = FindInIndex(index, artifactId);
        if (record is null)
        {
            return ReleaseIntegrityReport.Failure(artifactId, [$"Artifact '{artifactId}' is not present in the Release Index."]);
        }

        var packagePath = Path.Combine(normalized, record.RelativePackagePath);
        var errors = new List<string>();

        if (!File.Exists(packagePath))
        {
            errors.Add($"Stored package is missing: '{packagePath}'.");
            return ReleaseIntegrityReport.Failure(artifactId, errors);
        }

        var currentHash = PublishingContentHasher.HashFile(packagePath);
        if (!string.Equals(currentHash, record.PackageHash, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(
                $"Stored package hash no longer matches the recorded hash at registration time. "
                + $"Recorded='{record.PackageHash}', actual='{currentHash}'. The stored artifact may be corrupted or was modified after registration.");
        }

        var verification = await packageVerifier.VerifyAsync(packagePath, record.IsCertified, cancellationToken);
        if (!verification.IsValid)
        {
            errors.AddRange(verification.Errors);
        }

        return errors.Count == 0
            ? ReleaseIntegrityReport.Success(artifactId)
            : ReleaseIntegrityReport.Failure(artifactId, errors);
    }

    public async Task<IReadOnlyList<ReleaseIntegrityReport>> VerifyAllIntegrityAsync(string releaseLibraryRoot, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRoot(releaseLibraryRoot);
        var index = GetOrLoad(normalized);
        var reports = new List<ReleaseIntegrityReport>(index.Artifacts.Count);
        foreach (var record in index.Artifacts)
        {
            reports.Add(await VerifyIntegrityAsync(normalized, record.ArtifactId, cancellationToken));
        }

        return reports;
    }

    // -----------------------------------------------------------------
    // Retention (D7)
    // -----------------------------------------------------------------

    public ReleaseRetentionPlan EvaluateRetention(string releaseLibraryRoot, ReleaseRetentionPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var normalized = NormalizeRoot(releaseLibraryRoot);
        var index = GetOrLoad(normalized);

        var archiveCandidates = new List<string>();
        var deleteCandidates = new List<string>();
        var retainedProtected = new List<string>();

        foreach (var group in index.Artifacts.GroupBy(record => record.ServiceId, StringComparer.OrdinalIgnoreCase))
        {
            var ordered = group
                .OrderByDescending(record => record.GeneratedUtc)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var record = ordered[i];
                var isNeverDeleted = record.IsProtected
                    || record.LifecycleState is ArtifactLifecycleState.Deployed or ArtifactLifecycleState.Promoted
                    || record.IsCertified
                    || policy.ProtectedServiceIds.Contains(record.ServiceId, StringComparer.OrdinalIgnoreCase);

                if (isNeverDeleted)
                {
                    retainedProtected.Add(record.ArtifactId);
                    continue;
                }

                var withinKeepWindow = i < policy.KeepLatestVersionsPerService;
                var isStale = policy.ArchiveAfter is { } maxAge
                    && DateTimeOffset.UtcNow - record.GeneratedUtc > maxAge;

                if (!withinKeepWindow && record.LifecycleState != ArtifactLifecycleState.Archived)
                {
                    archiveCandidates.Add(record.ArtifactId);
                }
                else if (!withinKeepWindow && isStale)
                {
                    deleteCandidates.Add(record.ArtifactId);
                }
            }
        }

        return new ReleaseRetentionPlan
        {
            ArchiveCandidateArtifactIds = archiveCandidates,
            DeleteCandidateArtifactIds = deleteCandidates,
            RetainedProtectedArtifactIds = retainedProtected
        };
    }

    public async Task ApplyRetentionAsync(string releaseLibraryRoot, ReleaseRetentionPlan plan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var normalized = NormalizeRoot(releaseLibraryRoot);

        foreach (var artifactId in plan.ArchiveCandidateArtifactIds)
        {
            await ArchiveAsync(normalized, artifactId, "Retention policy: outside keep-latest-N window.", cancellationToken);
        }

        // Deletion only ever applies to non-protected, already-archived-eligible
        // artifacts identified by EvaluateRetention (never Deployed/Certified/
        // Promoted/protected — see the guard in EvaluateRetention).
        foreach (var artifactId in plan.DeleteCandidateArtifactIds)
        {
            var gate = GetGate(normalized);
            await gate.WaitAsync(cancellationToken);
            try
            {
                var index = ReleaseIndexStore.Load(normalized);
                var record = FindInIndex(index, artifactId);
                if (record is null || record.IsProtected || record.IsCertified)
                {
                    continue;
                }

                var packagePath = Path.Combine(normalized, record.RelativePackagePath);
                if (File.Exists(packagePath))
                {
                    File.Delete(packagePath);
                }

                var remaining = index.Artifacts.Where(candidate => !string.Equals(candidate.ArtifactId, artifactId, StringComparison.OrdinalIgnoreCase)).ToList();
                var newIndex = index with { Artifacts = remaining };
                ReleaseIndexStore.Save(normalized, newIndex);
                cachedIndexes[normalized] = newIndex;
            }
            finally
            {
                gate.Release();
            }
        }
    }

    // -----------------------------------------------------------------
    // Dependency / solution tracking (D8)
    // -----------------------------------------------------------------

    public IReadOnlyList<ReleaseArtifactRecord> GetSolutionComposition(string releaseLibraryRoot, string solution, string platformVersion)
    {
        var normalized = NormalizeRoot(releaseLibraryRoot);
        var index = GetOrLoad(normalized);
        return index.Artifacts
            .Where(record => string.Equals(record.Solution, solution, StringComparison.OrdinalIgnoreCase)
                              && string.Equals(record.PlatformVersion, platformVersion, StringComparison.OrdinalIgnoreCase))
            .OrderBy(record => record.ServiceId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // -----------------------------------------------------------------
    // Queries (D9)
    // -----------------------------------------------------------------

    public ReleaseArtifactRecord? FindByArtifactId(string releaseLibraryRoot, string artifactId)
        => FindInIndex(GetOrLoad(NormalizeRoot(releaseLibraryRoot)), artifactId);

    public ReleaseArtifactRecord? FindByVersion(string releaseLibraryRoot, string serviceId, string version)
        => FindByArtifactId(releaseLibraryRoot, BuildArtifactId(serviceId, version));

    public IReadOnlyList<ReleaseArtifactRecord> FindByService(string releaseLibraryRoot, string serviceId)
        => GetOrLoad(NormalizeRoot(releaseLibraryRoot)).Artifacts
            .Where(record => string.Equals(record.ServiceId, serviceId, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.GeneratedUtc)
            .ToList();

    public IReadOnlyList<ReleaseArtifactRecord> FindByPlatformVersion(string releaseLibraryRoot, string platformVersion)
        => GetOrLoad(NormalizeRoot(releaseLibraryRoot)).Artifacts
            .Where(record => string.Equals(record.PlatformVersion, platformVersion, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public ReleaseArtifactRecord? FindByFingerprint(string releaseLibraryRoot, string fingerprint)
        => GetOrLoad(NormalizeRoot(releaseLibraryRoot)).Artifacts
            .FirstOrDefault(record => string.Equals(record.PublishingFingerprint, fingerprint, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<ReleaseArtifactRecord> FindBySourceRevision(string releaseLibraryRoot, string sourceRevision)
        => GetOrLoad(NormalizeRoot(releaseLibraryRoot)).Artifacts
            .Where(record => string.Equals(record.SourceRevision, sourceRevision, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public ReleaseArtifactRecord? FindLatestCertified(string releaseLibraryRoot, string serviceId)
        => FindByService(releaseLibraryRoot, serviceId)
            .Where(record => record.IsCertified)
            .OrderByDescending(record => record.GeneratedUtc)
            .FirstOrDefault();

    public ReleaseArtifactRecord? FindLatest(string releaseLibraryRoot, string serviceId)
        => FindByService(releaseLibraryRoot, serviceId).OrderByDescending(record => record.GeneratedUtc).FirstOrDefault();

    public IReadOnlyList<ReleaseArtifactRecord> FindLatestPlatformBundle(string releaseLibraryRoot)
    {
        var index = GetOrLoad(NormalizeRoot(releaseLibraryRoot));
        var latestPlatformVersion = index.Artifacts
            .Where(record => !string.IsNullOrWhiteSpace(record.PlatformVersion))
            .OrderByDescending(record => record.GeneratedUtc)
            .Select(record => record.PlatformVersion)
            .FirstOrDefault();

        return string.IsNullOrWhiteSpace(latestPlatformVersion)
            ? []
            : FindByPlatformVersion(releaseLibraryRoot, latestPlatformVersion);
    }

    public ReleaseArtifactRecord? FindCompatibleRuntimeAgent(string releaseLibraryRoot, string contractVersion)
        => FindByService(releaseLibraryRoot, "runtime-agent")
            .Where(record => record.IsCertified && string.Equals(record.ContractVersion, contractVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.GeneratedUtc)
            .FirstOrDefault();

    public ReleaseArtifactRecord? FindRollbackCandidate(string releaseLibraryRoot, string serviceId, string currentVersion)
        => FindByService(releaseLibraryRoot, serviceId)
            .Where(record => (record.LifecycleState is ArtifactLifecycleState.Certified or ArtifactLifecycleState.Deployed or ArtifactLifecycleState.Promoted)
                              && !string.Equals(record.Version, currentVersion, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(record => record.GeneratedUtc)
            .FirstOrDefault();

    // -----------------------------------------------------------------
    // Internal helpers
    // -----------------------------------------------------------------

    private ReleaseIndexDocument GetOrLoad(string normalizedRoot)
        => cachedIndexes.GetOrAdd(normalizedRoot, ReleaseIndexStore.Load);

    private ReleaseArtifactRecord Persist(string normalizedRoot, ReleaseIndexDocument index, ReleaseArtifactRecord record)
    {
        var artifacts = index.Artifacts
            .Where(candidate => !string.Equals(candidate.ArtifactId, record.ArtifactId, StringComparison.OrdinalIgnoreCase))
            .Append(record)
            .ToList();

        var newIndex = index with { Artifacts = artifacts };
        ReleaseIndexStore.Save(normalizedRoot, newIndex);
        cachedIndexes[normalizedRoot] = newIndex;
        return record;
    }

    private static ReleaseArtifactRecord? FindInIndex(ReleaseIndexDocument index, string artifactId)
        => index.Artifacts.FirstOrDefault(record => string.Equals(record.ArtifactId, artifactId, StringComparison.OrdinalIgnoreCase));

    private SemaphoreSlim GetGate(string normalizedRoot)
        => rootGates.GetOrAdd(normalizedRoot, _ => new SemaphoreSlim(1, 1));

    private static string NormalizeRoot(string releaseLibraryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseLibraryRoot);
        return Path.GetFullPath(releaseLibraryRoot);
    }
}

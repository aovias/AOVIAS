using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.ReleaseLibrary;
using Aovias.Publishing.Core.Packaging;
using Aovias.Publishing.Core.ReleaseLibrary;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

/// <summary>
/// Sprint M1.5 regression suite: proves the Release Library is authoritative
/// (index-backed, not filesystem-scanned), that every artifact carries a
/// complete, validated lifecycle, and that integrity/retention/duplicate
/// detection behave exactly as documented in the M1.5 implementation report.
/// </summary>
public sealed class ReleaseLibraryRegressionTests : IDisposable
{
    private readonly string root;

    public ReleaseLibraryRegressionTests()
    {
        root = Path.Combine(Path.GetTempPath(), "aovias-m1-5-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup only; leaving a stray temp directory behind
            // must never fail the test run.
        }
    }

    // -----------------------------------------------------------------
    // D3/D4 — registration, metadata, and index persistence
    // -----------------------------------------------------------------

    [Fact]
    public async Task RegisterPublished_PersistsRecordAndCopiesPackageIntoLibrary()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-a");

        var record = await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "1.0.0", "fp-a", packagePath));

        Assert.Equal("gateway-1.0.0", record.ArtifactId);
        Assert.Equal(ArtifactLifecycleState.Published, record.LifecycleState);
        Assert.Equal(PublishingContentHasher.HashFile(packagePath), record.PackageHash);

        var storedPath = Path.Combine(root, record.RelativePackagePath);
        Assert.True(File.Exists(storedPath));

        // A second, independent service instance must see the same artifact —
        // proving the index (not this instance's in-memory cache) is authoritative.
        var reloaded = CreateService();
        var found = reloaded.FindByArtifactId(root, "gateway-1.0.0");
        Assert.NotNull(found);
        Assert.Equal(record.PublishingFingerprint, found!.PublishingFingerprint);
    }

    [Fact]
    public async Task RegisterPublished_SameArtifactId_SameFingerprint_IsIdempotent()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-b");
        var registration = BuildRegistration("identity-service", "2.0.0", "fp-b", packagePath);

        var first = await service.RegisterPublishedAsync(root, registration);
        var certified = await service.AdvanceToCertifiedAsync(root, first.ArtifactId);

        // Re-registering identical content must never regress lifecycle/promotion state.
        var second = await service.RegisterPublishedAsync(root, registration);

        Assert.Equal(certified.LifecycleState, second.LifecycleState);
        Assert.True(second.IsCertified);
    }

    [Fact]
    public async Task RegisterPublished_DuplicateArtifactId_DifferentFingerprint_Throws()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-c");
        await service.RegisterPublishedAsync(root, BuildRegistration("audit-service", "1.0.0", "fp-c1", packagePath));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterPublishedAsync(root, BuildRegistration("audit-service", "1.0.0", "fp-c2", packagePath)));
    }

    [Fact]
    public async Task RegisterPublished_DuplicateFingerprint_DifferentService_Throws()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-d");
        await service.RegisterPublishedAsync(root, BuildRegistration("outbox-worker", "1.0.0", "fp-shared", packagePath));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RegisterPublishedAsync(root, BuildRegistration("organization-service", "1.0.0", "fp-shared", packagePath)));
    }

    [Fact]
    public async Task RegisterPublished_MissingPackageFile_Throws()
    {
        var service = CreateService();
        var missingPath = Path.Combine(root, "does-not-exist.aovdeploy");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            service.RegisterPublishedAsync(root, BuildRegistration("portal", "1.0.0", "fp-e", missingPath)));
    }

    // -----------------------------------------------------------------
    // D2/D10 — lifecycle transition validity
    // -----------------------------------------------------------------

    [Fact]
    public async Task Lifecycle_ForwardTransitions_CanSkipAheadButNeverGoBackward()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-f");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("ai", "1.0.0", "fp-f", packagePath));

        // Skip Verified entirely: Published -> Certified must be legal.
        var certified = await service.AdvanceToCertifiedAsync(root, record.ArtifactId);
        Assert.Equal(ArtifactLifecycleState.Certified, certified.LifecycleState);

        // Moving backward to an earlier state must fail.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdvanceToVerifiedAsync(root, record.ArtifactId));
    }

    [Fact]
    public async Task Lifecycle_FullHappyPath_ReachesPromoted()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-g");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("knowledge-base", "1.0.0", "fp-g", packagePath));

        await service.AdvanceToVerifiedAsync(root, record.ArtifactId);
        await service.AdvanceToCertifiedAsync(root, record.ArtifactId);
        await service.AdvanceToReadyAsync(root, record.ArtifactId);
        await service.MarkDeployedAsync(root, record.ArtifactId);
        var promoted = await service.MarkPromotedAsync(root, record.ArtifactId);

        Assert.Equal(ArtifactLifecycleState.Promoted, promoted.LifecycleState);
        Assert.Equal(ArtifactPromotionStatus.ProductionApproved, promoted.PromotionStatus);
        // Sprint G1: first real execution of this test. RegisterPublishedAsync itself records an
        // initial Building->Published transition (ReleaseLibraryService.cs), so the 5 explicit
        // calls above (Verified/Certified/Ready/Deployed/Promoted) make 6 total, not 5 — the
        // stale expectation predates ever running this test against the real service.
        Assert.Equal(6, promoted.Transitions.Count);
    }

    [Fact]
    public async Task Lifecycle_DeprecatedIsTerminalExceptForArchive()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-h");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("compliance-service", "1.0.0", "fp-h", packagePath));
        await service.MarkDeprecatedAsync(root, record.ArtifactId, "Superseded by 1.1.0");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdvanceToCertifiedAsync(root, record.ArtifactId));

        var archived = await service.ArchiveAsync(root, record.ArtifactId, "Retention sweep");
        Assert.Equal(ArtifactLifecycleState.Archived, archived.LifecycleState);
    }

    [Fact]
    public async Task Lifecycle_TransitionOnUnknownArtifact_Throws()
    {
        var service = CreateService();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AdvanceToVerifiedAsync(root, "does-not-exist-1.0.0"));
    }

    // -----------------------------------------------------------------
    // D5 — artifact integrity and corruption detection
    // -----------------------------------------------------------------

    [Fact]
    public async Task VerifyIntegrity_IntactArtifact_Succeeds()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-i");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "3.0.0", "fp-i", packagePath));

        var report = await service.VerifyIntegrityAsync(root, record.ArtifactId);

        Assert.True(report.IsIntact);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task VerifyIntegrity_TamperedStoredPackage_DetectsHashMismatch()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-j");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("identity-service", "3.0.0", "fp-j", packagePath));

        var storedPath = Path.Combine(root, record.RelativePackagePath);
        File.WriteAllText(storedPath, "tampered-content");

        var report = await service.VerifyIntegrityAsync(root, record.ArtifactId);

        Assert.False(report.IsIntact);
        Assert.Contains(report.Errors, e => e.Contains("hash", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyIntegrity_MissingStoredPackage_ReportsFailureWithoutThrowing()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-k");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("audit-service", "3.0.0", "fp-k", packagePath));

        File.Delete(Path.Combine(root, record.RelativePackagePath));

        var report = await service.VerifyIntegrityAsync(root, record.ArtifactId);

        Assert.False(report.IsIntact);
        Assert.Contains(report.Errors, e => e.Contains("missing", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyIntegrity_UnknownArtifact_ReportsFailure()
    {
        var service = CreateService();
        var report = await service.VerifyIntegrityAsync(root, "unregistered-9.9.9");

        Assert.False(report.IsIntact);
        Assert.Contains(report.Errors, e => e.Contains("Release Index", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task VerifyAllIntegrity_CoversEveryRegisteredArtifact()
    {
        var service = CreateService();
        await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "4.0.0", "fp-l1", CreateDummyPackage("payload-l1")));
        await service.RegisterPublishedAsync(root, BuildRegistration("portal", "4.0.0", "fp-l2", CreateDummyPackage("payload-l2")));

        var reports = await service.VerifyAllIntegrityAsync(root);

        Assert.Equal(2, reports.Count);
        Assert.All(reports, r => Assert.True(r.IsIntact));
    }

    // -----------------------------------------------------------------
    // D4/D10 — Release Index corruption is a hard failure, never silent
    // -----------------------------------------------------------------

    [Fact]
    public void CorruptedIndex_InvalidJson_ThrowsRatherThanTreatingLibraryAsEmpty()
    {
        var indexDir = ReleaseIndexStore.GetIndexDirectory(root);
        Directory.CreateDirectory(indexDir);
        File.WriteAllText(ReleaseIndexStore.GetIndexPath(root), "{ not valid json");

        var service = CreateService();
        Assert.Throws<InvalidOperationException>(() => service.FindByArtifactId(root, "anything-1.0.0"));
    }

    // -----------------------------------------------------------------
    // D7 — configurable, conservative (archive-then-delete) retention
    // -----------------------------------------------------------------

    [Fact]
    public void EvaluateRetention_KeepsWindowAndArchivesOlderNonProtectedVersions()
    {
        var service = CreateService();
        var now = DateTimeOffset.UtcNow;
        SeedIndex(
            RecordFor("svc-a", "3.0.0", now, ArtifactLifecycleState.Published, isCertified: false, isProtected: false),
            RecordFor("svc-a", "2.0.0", now.AddDays(-1), ArtifactLifecycleState.Published, isCertified: false, isProtected: false),
            RecordFor("svc-a", "1.0.0", now.AddDays(-2), ArtifactLifecycleState.Published, isCertified: false, isProtected: false));

        var plan = service.EvaluateRetention(root, new ReleaseRetentionPolicy { KeepLatestVersionsPerService = 1 });

        Assert.Contains("svc-a-2.0.0", plan.ArchiveCandidateArtifactIds);
        Assert.Contains("svc-a-1.0.0", plan.ArchiveCandidateArtifactIds);
        Assert.DoesNotContain("svc-a-3.0.0", plan.ArchiveCandidateArtifactIds);
        Assert.Empty(plan.DeleteCandidateArtifactIds);
    }

    [Fact]
    public void EvaluateRetention_NeverArchivesCertifiedOrProtectedArtifacts()
    {
        var service = CreateService();
        var now = DateTimeOffset.UtcNow;
        SeedIndex(
            RecordFor("svc-b", "2.0.0", now, ArtifactLifecycleState.Published, isCertified: false, isProtected: false),
            RecordFor("svc-b", "1.0.0", now.AddDays(-30), ArtifactLifecycleState.Certified, isCertified: true, isProtected: true));

        var plan = service.EvaluateRetention(root, new ReleaseRetentionPolicy { KeepLatestVersionsPerService = 1 });

        Assert.Contains("svc-b-1.0.0", plan.RetainedProtectedArtifactIds);
        Assert.DoesNotContain("svc-b-1.0.0", plan.ArchiveCandidateArtifactIds);
        Assert.DoesNotContain("svc-b-1.0.0", plan.DeleteCandidateArtifactIds);
    }

    [Fact]
    public void EvaluateRetention_AlreadyArchivedAndStale_BecomesDeleteCandidate()
    {
        var service = CreateService();
        var now = DateTimeOffset.UtcNow;
        SeedIndex(
            RecordFor("svc-c", "2.0.0", now, ArtifactLifecycleState.Published, isCertified: false, isProtected: false),
            RecordFor("svc-c", "1.0.0", now.AddDays(-100), ArtifactLifecycleState.Archived, isCertified: false, isProtected: false));

        var plan = service.EvaluateRetention(
            root,
            new ReleaseRetentionPolicy { KeepLatestVersionsPerService = 1, ArchiveAfter = TimeSpan.FromDays(30) });

        Assert.Contains("svc-c-1.0.0", plan.DeleteCandidateArtifactIds);
        Assert.DoesNotContain("svc-c-1.0.0", plan.ArchiveCandidateArtifactIds);
    }

    [Fact]
    public async Task ApplyRetention_ArchivesCandidatesAndDeletesOnlyNonProtectedStaleArchived()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-retain");
        var registered = await service.RegisterPublishedAsync(root, BuildRegistration("svc-d", "2.0.0", "fp-retain", packagePath));
        Assert.Equal(ArtifactLifecycleState.Published, registered.LifecycleState);

        var plan = new ReleaseRetentionPlan { ArchiveCandidateArtifactIds = [registered.ArtifactId] };
        await service.ApplyRetentionAsync(root, plan);

        var afterArchive = service.FindByArtifactId(root, registered.ArtifactId);
        Assert.Equal(ArtifactLifecycleState.Archived, afterArchive!.LifecycleState);

        var deletePlan = new ReleaseRetentionPlan { DeleteCandidateArtifactIds = [registered.ArtifactId] };
        await service.ApplyRetentionAsync(root, deletePlan);

        Assert.Null(service.FindByArtifactId(root, registered.ArtifactId));
        Assert.False(File.Exists(Path.Combine(root, afterArchive.RelativePackagePath)));
    }

    [Fact]
    public async Task ApplyRetention_NeverDeletesCertifiedArtifactEvenIfListedAsCandidate()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-protect");
        var registered = await service.RegisterPublishedAsync(root, BuildRegistration("svc-e", "1.0.0", "fp-protect", packagePath));
        await service.AdvanceToCertifiedAsync(root, registered.ArtifactId);

        // Simulate a caller mistakenly passing a certified artifact as a delete
        // candidate: the service's own guard must refuse to delete it regardless.
        var plan = new ReleaseRetentionPlan { DeleteCandidateArtifactIds = [registered.ArtifactId] };
        await service.ApplyRetentionAsync(root, plan);

        var stillPresent = service.FindByArtifactId(root, registered.ArtifactId);
        Assert.NotNull(stillPresent);
        Assert.True(File.Exists(Path.Combine(root, stillPresent!.RelativePackagePath)));
    }

    // -----------------------------------------------------------------
    // D8 — solution/dependency composition
    // -----------------------------------------------------------------

    [Fact]
    public async Task GetSolutionComposition_ReturnsOnlyArtifactsMatchingSolutionAndPlatformVersion()
    {
        var service = CreateService();
        await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "5.0.0", "fp-m1", CreateDummyPackage("payload-m1"), solution: "AOVIAS-Platform", platformVersion: "2026.1"));
        await service.RegisterPublishedAsync(root, BuildRegistration("portal", "5.0.0", "fp-m2", CreateDummyPackage("payload-m2"), solution: "AOVIAS-Platform", platformVersion: "2026.1"));
        await service.RegisterPublishedAsync(root, BuildRegistration("ai", "1.0.0", "fp-m3", CreateDummyPackage("payload-m3"), solution: "AOVIAS-Platform", platformVersion: "2025.4"));

        var composition = service.GetSolutionComposition(root, "AOVIAS-Platform", "2026.1");

        Assert.Equal(2, composition.Count);
        Assert.All(composition, r => Assert.Equal("2026.1", r.PlatformVersion));
    }

    // -----------------------------------------------------------------
    // D9 — search & query API
    // -----------------------------------------------------------------

    [Fact]
    public async Task FindByVersion_And_FindByFingerprint_ResolveTheSameArtifact()
    {
        var service = CreateService();
        var packagePath = CreateDummyPackage("payload-n");
        var record = await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "6.0.0", "fp-n", packagePath));

        Assert.Equal(record.ArtifactId, service.FindByVersion(root, "gateway", "6.0.0")!.ArtifactId);
        Assert.Equal(record.ArtifactId, service.FindByFingerprint(root, "fp-n")!.ArtifactId);
    }

    [Fact]
    public async Task FindBySourceRevision_ReturnsAllArtifactsBuiltFromThatRevision()
    {
        var service = CreateService();
        await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "7.0.0", "fp-o1", CreateDummyPackage("payload-o1"), sourceRevision: "abc123"));
        await service.RegisterPublishedAsync(root, BuildRegistration("portal", "7.0.0", "fp-o2", CreateDummyPackage("payload-o2"), sourceRevision: "abc123"));
        await service.RegisterPublishedAsync(root, BuildRegistration("ai", "7.0.0", "fp-o3", CreateDummyPackage("payload-o3"), sourceRevision: "different"));

        var fromRevision = service.FindBySourceRevision(root, "abc123");

        Assert.Equal(2, fromRevision.Count);
    }

    [Fact]
    public async Task FindLatestCertified_IgnoresUncertifiedNewerVersion()
    {
        var service = CreateService();
        var v1 = await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "8.0.0", "fp-p1", CreateDummyPackage("payload-p1")));
        await service.AdvanceToCertifiedAsync(root, v1.ArtifactId);
        await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "8.1.0", "fp-p2", CreateDummyPackage("payload-p2")));

        var latestCertified = service.FindLatestCertified(root, "gateway");

        Assert.Equal("gateway-8.0.0", latestCertified!.ArtifactId);
    }

    [Fact]
    public async Task LoadAsync_refreshes_stale_in_process_cache_after_another_instance_updates_the_index()
    {
        var writer = CreateService();
        var reader = CreateService();
        await writer.RegisterPublishedAsync(root, BuildRegistration("runtime-agent", "1.0.0", "fp-refresh-1", CreateDummyPackage("payload-refresh-1")));
        await reader.LoadAsync(root);

        Assert.Single(reader.FindByService(root, "runtime-agent"));

        await writer.RegisterPublishedAsync(root, BuildRegistration("runtime-agent", "1.0.1", "fp-refresh-2", CreateDummyPackage("payload-refresh-2")));
        Assert.Single(reader.FindByService(root, "runtime-agent"));

        await reader.LoadAsync(root);

        Assert.Equal(2, reader.FindByService(root, "runtime-agent").Count);
    }

    [Fact]
    public async Task FindCompatibleRuntimeAgent_MatchesOnCertifiedStatusAndContractVersion()
    {
        var service = CreateService();
        var record = await service.RegisterPublishedAsync(
            root,
            BuildRegistration("runtime-agent", "1.0.0", "fp-q", CreateDummyPackage("payload-q"), contractVersion: "1.0"));
        await service.AdvanceToCertifiedAsync(root, record.ArtifactId);

        var compatible = service.FindCompatibleRuntimeAgent(root, "1.0");

        Assert.NotNull(compatible);
        Assert.Equal(record.ArtifactId, compatible!.ArtifactId);
        Assert.Null(service.FindCompatibleRuntimeAgent(root, "2.0"));
    }

    [Fact]
    public async Task FindRollbackCandidate_ReturnsNewestCertifiedVersionOtherThanCurrent()
    {
        var service = CreateService();
        var older = await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "9.0.0", "fp-r1", CreateDummyPackage("payload-r1")));
        await service.AdvanceToCertifiedAsync(root, older.ArtifactId);
        var newer = await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "9.1.0", "fp-r2", CreateDummyPackage("payload-r2")));
        await service.AdvanceToCertifiedAsync(root, newer.ArtifactId);

        var rollback = service.FindRollbackCandidate(root, "gateway", currentVersion: "9.1.0");

        Assert.Equal("gateway-9.0.0", rollback!.ArtifactId);
    }

    [Fact]
    public async Task FindLatestPlatformBundle_ReturnsArtifactsFromTheNewestPlatformVersion()
    {
        var service = CreateService();
        await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "10.0.0", "fp-s1", CreateDummyPackage("payload-s1"), platformVersion: "2025.4"));
        await service.RegisterPublishedAsync(root, BuildRegistration("gateway", "11.0.0", "fp-s2", CreateDummyPackage("payload-s2"), platformVersion: "2026.1"));
        await service.RegisterPublishedAsync(root, BuildRegistration("portal", "11.0.0", "fp-s3", CreateDummyPackage("payload-s3"), platformVersion: "2026.1"));

        var bundle = service.FindLatestPlatformBundle(root);

        Assert.Equal(2, bundle.Count);
        Assert.All(bundle, r => Assert.Equal("2026.1", r.PlatformVersion));
    }

    // -----------------------------------------------------------------
    // CSM/SDK consolidation: package-path-only registration overload
    // -----------------------------------------------------------------

    [Fact]
    public async Task RegisterOrganizedCopyAsync_PackagePathOnly_DerivesIdentityFromManifestAndAutoCertifies()
    {
        var reader = new FakeDeployPackageReader();
        var verifier = new FakeDeployPackageVerifier();
        var service = new ReleaseLibraryService(reader, verifier);
        var packagePath = CreateDummyPackage("payload-t");

        reader.Contents = BuildPackageContents(packagePath, serviceId: "compliance-service", version: "1.2.3", withCertificate: true);

        var storedPath = await service.RegisterOrganizedCopyAsync(root, packagePath);

        Assert.True(File.Exists(storedPath));
        var record = service.FindByArtifactId(root, "compliance-service-1.2.3");
        Assert.NotNull(record);
        Assert.True(record!.IsCertified);
        Assert.Equal(ArtifactLifecycleState.Certified, record.LifecycleState);
    }

    [Fact]
    public async Task RegisterOrganizedCopyAsync_WithoutCertificate_StaysAtPublished()
    {
        var reader = new FakeDeployPackageReader();
        var verifier = new FakeDeployPackageVerifier();
        var service = new ReleaseLibraryService(reader, verifier);
        var packagePath = CreateDummyPackage("payload-u");

        reader.Contents = BuildPackageContents(packagePath, serviceId: "outbox-worker", version: "1.0.0", withCertificate: false);

        await service.RegisterOrganizedCopyAsync(root, packagePath);

        var record = service.FindByArtifactId(root, "outbox-worker-1.0.0");
        Assert.NotNull(record);
        Assert.False(record!.IsCertified);
        Assert.Equal(ArtifactLifecycleState.Published, record.LifecycleState);
    }

    // -----------------------------------------------------------------
    // Test infrastructure
    // -----------------------------------------------------------------

    private static ReleaseLibraryService CreateService()
        => new(new FakeDeployPackageReader(), new FakeDeployPackageVerifier());

    private string CreateDummyPackage(string content)
    {
        var path = Path.Combine(root, $"src-{Guid.NewGuid():N}.aovdeploy");
        File.WriteAllText(path, content);
        return path;
    }

    private static ReleaseArtifactRegistration BuildRegistration(
        string serviceId,
        string version,
        string fingerprint,
        string packagePath,
        string? sourceRevision = null,
        string? solution = null,
        string? platformVersion = null,
        string contractVersion = "1.0")
        => new()
        {
            ServiceId = serviceId,
            ServiceDisplayName = serviceId,
            Version = version,
            PackagePath = packagePath,
            SourceRevision = sourceRevision ?? "rev-" + Guid.NewGuid().ToString("N")[..8],
            BuildUtc = DateTimeOffset.UtcNow,
            PublishingFingerprint = fingerprint,
            ContractVersion = contractVersion,
            PublishingEngineVersion = "1.0.0-test",
            FingerprintPolicyVersion = 1,
            Solution = solution,
            PlatformVersion = platformVersion
        };

    private void SeedIndex(params ReleaseArtifactRecord[] records)
        => ReleaseIndexStore.Save(root, new ReleaseIndexDocument { Artifacts = records });

    private ReleaseArtifactRecord RecordFor(
        string serviceId,
        string version,
        DateTimeOffset generatedUtc,
        ArtifactLifecycleState state,
        bool isCertified,
        bool isProtected)
    {
        var packagePath = CreateDummyPackage($"{serviceId}-{version}");
        var fileName = Path.GetFileName(packagePath);
        return new ReleaseArtifactRecord
        {
            ArtifactId = $"{serviceId}-{version}",
            ServiceId = serviceId,
            Service = serviceId,
            Version = version,
            BuildIdentity = $"rev@{generatedUtc:O}",
            SourceRevision = "rev",
            PublishingFingerprint = $"fp-{serviceId}-{version}",
            PackageHash = PublishingContentHasher.HashFile(packagePath),
            PackageSizeBytes = new FileInfo(packagePath).Length,
            GeneratedUtc = generatedUtc,
            ContractVersion = "1.0",
            PublishingEngineVersion = "1.0.0-test",
            SupportedRuntimeVersion = "net10.0",
            DeploymentCompatibility = "contract:1.0;engine:1.0.0-test;fingerprint-policy:1",
            RelativePackagePath = fileName,
            LifecycleState = state,
            IsCertified = isCertified,
            IsProtected = isProtected
        };
    }

    private static DeployPackageContents BuildPackageContents(string packagePath, string serviceId, string version, bool withCertificate)
        => new()
        {
            PackagePath = packagePath,
            Manifest = new DeployManifest
            {
                Service = serviceId,
                Version = version,
                Type = "service",
                Container = serviceId,
                Health = "/health",
                EntryPoint = "dotnet",
                Configuration = "config-template.json",
                ImageName = serviceId,
                ImageTag = version
            },
            Health = new DeployHealthDefinition { Endpoint = "/health" },
            Metadata = new DeployPackageMetadata
            {
                Publisher = "test-suite",
                PublishingEngineVersion = "1.0.0-test",
                PackageVersion = version,
                PackageType = "service",
                CreatedUtc = DateTimeOffset.UtcNow,
                SourceRevision = "rev-test",
                PackageFingerprint = $"fp-{serviceId}-{version}"
            },
            Checksums = new Dictionary<string, string>(),
            ConfigurationTemplate = new Dictionary<string, string>(),
            ReadinessCertificate = withCertificate
                ? new DeploymentReadinessCertificate
                {
                    ValidationId = Guid.NewGuid().ToString("N"),
                    ValidatedUtc = DateTimeOffset.UtcNow,
                    DrvVersion = "1.0",
                    RuntimeCompatibility = "net10.0",
                    PackageChecksum = "n/a",
                    ValidationScore = 100
                }
                : null
        };

    private sealed class FakeDeployPackageReader : IDeployPackageReader
    {
        public DeployPackageContents? Contents { get; set; }

        public Task<DeployPackageContents?> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
            => Task.FromResult(Contents);
    }

    private sealed class FakeDeployPackageVerifier : IDeployPackageVerifier
    {
        public DeployPackageVerificationResult Result { get; set; } = DeployPackageVerificationResult.Success([], 0, hasCertificate: true);

        public Task<DeployPackageVerificationResult> VerifyAsync(string packagePath, bool expectCertificate, CancellationToken cancellationToken = default)
            => Task.FromResult(Result);
    }
}

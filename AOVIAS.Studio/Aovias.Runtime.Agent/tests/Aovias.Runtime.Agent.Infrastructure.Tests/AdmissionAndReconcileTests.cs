using Aovias.Configuration.Deployment;
using Aovias.Deployment.Contracts.Admission;
using Aovias.Deployment.Contracts.Capabilities;
using Aovias.Deployment.Contracts.Infrastructure;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Admission;
using Aovias.Runtime.Agent.Infrastructure.Reconciliation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aovias.Runtime.Agent.Infrastructure.Tests;

/// <summary>
/// First Runtime Agent Infrastructure test project (Phase 9 / P8-CL02).
/// Covers admission persistence and reconcile maintenance-mode safety signal without live Docker.
/// </summary>
public sealed class AdmissionAndReconcileTests
{
    [Fact]
    public async Task EnvironmentAdmissionStore_RoundTripsNotReadyReport()
    {
        using var harness = new TempConfigHarness();
        var store = new EnvironmentAdmissionStore(harness.PathResolver, NullLogger<EnvironmentAdmissionStore>.Instance);

        var report = EnvironmentAdmissionEvaluator.Evaluate(
            "docker-host",
            new Dictionary<string, IReadOnlyList<Requirement>>
            {
                ["identity"] =
                [
                    new Requirement
                    {
                        Id = "attachments",
                        Category = RequirementCategories.ObjectStorage,
                        Mandatory = true
                    }
                ]
            });

        Assert.False(report.IsReady);
        await store.SaveAsync(report);

        var loaded = await store.TryLoadAsync();
        Assert.NotNull(loaded);
        Assert.False(loaded!.IsReady);
        Assert.True(loaded.MissingCount > 0);
        Assert.Contains("identity", loaded.ServiceIdsEvaluated);
    }

    [Fact]
    public async Task EnvironmentAdmissionStore_MissingFile_ReturnsNull()
    {
        using var harness = new TempConfigHarness();
        var store = new EnvironmentAdmissionStore(harness.PathResolver, NullLogger<EnvironmentAdmissionStore>.Instance);

        var loaded = await store.TryLoadAsync();
        Assert.Null(loaded);
    }

    [Fact]
    public void MaintenanceModeStore_Enable_SetsActiveStatusForReconcileSafety()
    {
        var store = new MaintenanceModeStore();
        Assert.False(store.GetStatus().Active);

        var status = store.Enable(new MaintenanceModeRequest
        {
            Reason = "phase9-test",
            RequestedBy = "p8-cl02"
        });

        Assert.True(status.Active);
        Assert.Equal("phase9-test", status.Reason);
        Assert.Equal("p8-cl02", status.ChangedBy);
        Assert.True(store.GetStatus().Active);
    }

    [Fact]
    public void MaintenanceModeStore_Resume_ClearsActiveStatus()
    {
        var store = new MaintenanceModeStore();
        store.Enable(new MaintenanceModeRequest { Reason = "hold", RequestedBy = "op" });
        var resumed = store.Resume("op");

        Assert.False(resumed.Active);
        Assert.False(store.GetStatus().Active);
    }

    private sealed class TempConfigHarness : IDisposable
    {
        private readonly string tempDirectory = Path.Combine(Path.GetTempPath(), "aovias-ra-infra-tests-" + Guid.NewGuid().ToString("N"));

        public TempConfigHarness()
        {
            Directory.CreateDirectory(tempDirectory);
            PathResolver = new FakeDeploymentPathResolver(tempDirectory);
        }

        public FakeDeploymentPathResolver PathResolver { get; }

        public void Dispose()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private sealed class FakeDeploymentPathResolver(string configRoot) : IDeploymentPathResolver
    {
        public string ConfigRoot { get; } = configRoot;
        public string DeploymentRoot => throw new NotSupportedException();
        public string IncomingRoot => throw new NotSupportedException();
        public string ReleaseRoot => throw new NotSupportedException();
        public string TempRoot => throw new NotSupportedException();
        public string ArchiveRoot => throw new NotSupportedException();
        public string StagingRoot => throw new NotSupportedException();
        public string ExtractionRoot => throw new NotSupportedException();
        public string ActiveRoot => throw new NotSupportedException();
        public string RollbackRoot => throw new NotSupportedException();
        public string AuditRoot => throw new NotSupportedException();
        public string RuntimeRoot => throw new NotSupportedException();
        public string LogsRoot => throw new NotSupportedException();
        public string PackagesRoot => throw new NotSupportedException();
        public string CertificatesRoot => throw new NotSupportedException();
        public string ProfilesRoot => throw new NotSupportedException();
        public string SecretsRoot => throw new NotSupportedException();
        public string ResolveUnderDeploymentRoot(string relativeSegment) => throw new NotSupportedException();
        public void EnsureDirectory(string path) => Directory.CreateDirectory(path);
        public DeploymentStorageValidationResult ValidateStorageLayout(bool createMissing = false) => throw new NotSupportedException();
        public DeploymentStorageValidationResult ValidateWritable(string path, string label, string phase, string operation) => throw new NotSupportedException();
    }
}
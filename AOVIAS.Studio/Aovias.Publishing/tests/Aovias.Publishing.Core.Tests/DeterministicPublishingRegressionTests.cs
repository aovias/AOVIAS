using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Execution;
using Aovias.Publishing.Core.Packaging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.Publishing.Core.Tests;

public sealed class DeterministicPublishingRegressionTests
{
    [Fact]
    public void BuildIdentityService_IdenticalInputs_ProduceSameSourceRevision()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-m12-" + Guid.NewGuid().ToString("N"));
        var publishDir = Path.Combine(tempRoot, "publish");
        Directory.CreateDirectory(publishDir);
        File.WriteAllText(Path.Combine(publishDir, "app.dll"), "same-content");

        var projectPath = Path.Combine(tempRoot, "Service.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var dockerfilePath = Path.Combine(tempRoot, "Dockerfile");
        File.WriteAllText(dockerfilePath, "FROM mcr.microsoft.com/dotnet/aspnet:10.0");

        var service = CreateIdentityService();
        var firstContext = CreateContext(tempRoot, projectPath, publishDir, dockerfilePath);
        var secondContext = CreateContext(tempRoot, projectPath, publishDir, dockerfilePath);

        var first = service.EstablishPreDocker(firstContext);
        var second = service.EstablishPreDocker(secondContext);

        Assert.Equal(first.SourceRevision, second.SourceRevision);
        Assert.Equal(FingerprintPolicy.Version, first.FingerprintPolicyVersion);
    }

    [Fact]
    public void BuildIdentityService_ChangedPublishOutput_ProducesDifferentSourceRevision()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-m12-" + Guid.NewGuid().ToString("N"));
        var publishDir = Path.Combine(tempRoot, "publish");
        Directory.CreateDirectory(publishDir);
        File.WriteAllText(Path.Combine(publishDir, "app.dll"), "baseline");

        var projectPath = Path.Combine(tempRoot, "Service.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var dockerfilePath = Path.Combine(tempRoot, "Dockerfile");
        File.WriteAllText(dockerfilePath, "FROM mcr.microsoft.com/dotnet/aspnet:10.0");

        var service = CreateIdentityService();
        var baselineContext = CreateContext(tempRoot, projectPath, publishDir, dockerfilePath);
        var baseline = service.EstablishPreDocker(baselineContext);

        File.WriteAllText(Path.Combine(publishDir, "app.dll"), "changed");
        var changedContext = CreateContext(tempRoot, projectPath, publishDir, dockerfilePath);
        var changed = service.EstablishPreDocker(changedContext);

        Assert.NotEqual(baseline.SourceRevision, changed.SourceRevision);
    }

    [Fact]
    public void BuildIdentityService_FinalizeInputFingerprint_ChangesWhenComposeChanges()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-m12-" + Guid.NewGuid().ToString("N"));
        var publishDir = Path.Combine(tempRoot, "publish");
        Directory.CreateDirectory(publishDir);
        File.WriteAllText(Path.Combine(publishDir, "app.dll"), "content");

        var projectPath = Path.Combine(tempRoot, "Service.csproj");
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        var dockerfilePath = Path.Combine(tempRoot, "Dockerfile");
        File.WriteAllText(dockerfilePath, "FROM mcr.microsoft.com/dotnet/aspnet:10.0");

        var service = CreateIdentityService();
        var baselineContext = CreateContext(tempRoot, projectPath, publishDir, dockerfilePath);
        service.EstablishPreDocker(baselineContext);
        service.ApplyDockerImageIdentity(baselineContext, "sha256:baseline");
        baselineContext.ComposeContent = "services:\n  app:\n    image: aovias/service:1.0.0\n";
        var baseline = service.FinalizeInputFingerprint(baselineContext);

        var changedContext = CreateContext(tempRoot, projectPath, publishDir, dockerfilePath);
        service.EstablishPreDocker(changedContext);
        service.ApplyDockerImageIdentity(changedContext, "sha256:baseline");
        changedContext.ComposeContent = "services:\n  app:\n    image: aovias/service:1.0.0-changed\n";
        var changed = service.FinalizeInputFingerprint(changedContext);

        Assert.NotEqual(baseline.PublishingInputFingerprint, changed.PublishingInputFingerprint);
    }

    [Fact]
    public void StagingIntegrityValidator_RejectsStaleManifestTraceability()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "aovias-m12-staging-" + Guid.NewGuid().ToString("N"));
        var staging = Path.Combine(tempRoot, "staging");
        Directory.CreateDirectory(staging);

        var service = CreateIdentityService();
        var context = CreateContext(tempRoot, Path.Combine(tempRoot, "Service.csproj"), Path.Combine(tempRoot, "publish"), Path.Combine(tempRoot, "Dockerfile"));
        Directory.CreateDirectory(context.PublishOutputDirectory!);
        File.WriteAllText(Path.Combine(context.PublishOutputDirectory!, "app.dll"), "content");
        service.EstablishPreDocker(context);
        service.ApplyDockerImageIdentity(context, "sha256:identity");
        context.Manifest = new DeployManifest
        {
            Service = "service-a",
            Version = "1.0.0",
            Type = "AspNetCore",
            Container = "service-a",
            Health = "/health",
            EntryPoint = "app.dll",
            Ports = [8080],
            Dependencies = [],
            Configuration = DeployPackageLayout.ConfigTemplateFile,
            ImageName = "aovias/service-a",
            ImageTag = "1.0.0"
        };
        service.FinalizeInputFingerprint(context);

        context.StagingDirectory = staging;
        var manifest = context.Manifest!;
        context.Manifest = new DeployManifest
        {
            Service = manifest.Service,
            Version = manifest.Version,
            Type = manifest.Type,
            Container = manifest.Container,
            Health = manifest.Health,
            EntryPoint = manifest.EntryPoint,
            Ports = manifest.Ports,
            Dependencies = manifest.Dependencies,
            Configuration = manifest.Configuration,
            ImageName = manifest.ImageName,
            ImageTag = manifest.ImageTag,
            SourceRevision = "stale-revision",
            PublishingInputFingerprint = manifest.PublishingInputFingerprint,
            DockerImageIdentity = manifest.DockerImageIdentity
        };
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.ManifestFile), "{}");
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.HealthFile), "{}");
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.MetadataFile), "{}");
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.ConfigTemplateFile), "{}");
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.ComposeFile), "services:{}");
        File.WriteAllText(Path.Combine(staging, DeployPackageLayout.EnvTemplateFile), "A=1");
        File.WriteAllBytes(Path.Combine(staging, DeployPackageLayout.ServiceImageTar), [1, 2, 3]);

        var errors = PublishingStagingIntegrityValidator.Validate(context);
        Assert.Contains(errors, error => error.Contains("source revision", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FingerprintPolicy_UsesImageIdentityForServiceImageTar()
    {
        Assert.True(FingerprintPolicy.UsesImageIdentity(DeployPackageLayout.ServiceImageTar));
        Assert.Contains(DeployPackageLayout.PublishingBuildManifestFile, FingerprintPolicy.VolatilePackageEntries);
    }

    private static PublishingBuildIdentityService CreateIdentityService()
        => new(new NoOpProcessRunner(), new TestOptionsMonitor(new PublishingSettings()));

    private static PublishingContext CreateContext(
        string projectRoot,
        string projectPath,
        string publishDir,
        string dockerfilePath)
        => new()
        {
            Request = new PublishingRequest
            {
                ProjectPath = projectPath,
                ServiceId = "service-a",
                Version = "1.0.0",
                Profile = "demo"
            },
            Discovery = new ProjectDiscoveryResult
            {
                ProjectType = PublishingProjectType.AspNetCore,
                ProjectPath = projectPath,
                ProjectRoot = projectRoot,
                ServiceId = "service-a",
                ServiceName = "Service A",
                ProviderName = "DotNet",
                Framework = "net10.0",
                ContainerPort = 8080
            },
            PublishOutputDirectory = publishDir,
            DockerfilePath = dockerfilePath,
            WorkingDirectory = projectRoot,
            StagingDirectory = Path.Combine(projectRoot, "staging")
        };

    private sealed class NoOpProcessRunner : IProcessRunner
    {
        public Task<ProcessExecutionResult> RunAsync(
            string executable,
            string arguments,
            string workingDirectory,
            int timeoutSeconds,
            CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult
            {
                ExitCode = 1,
                StandardError = "git unavailable in test"
            });
    }

    private sealed class TestOptionsMonitor(PublishingSettings value) : IOptionsMonitor<PublishingSettings>
    {
        public PublishingSettings CurrentValue { get; } = value;
        public PublishingSettings Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<PublishingSettings, string?> listener) => null;
    }
}

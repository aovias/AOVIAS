using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.Contracts.Verification;
using Aovias.Publishing.Core.Execution;
using Aovias.Publishing.Core.Verification;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.Publishing.Core.Tests.Verification;

/// <summary>
/// Sprint M3.3 regression tests for the end-to-end verification pipeline's aggregation logic.
/// Uses fakes for the dotnet CLI, solution publishing orchestrator, and package reader so these
/// tests are deterministic and require no real .NET SDK or Docker â€” consistent with how the rest
/// of this project fakes <see cref="IDotNetCliExecutor"/> (see <c>DotNetProjectTypeProviderTests</c>)
/// and <see cref="IOptionsMonitor{TOptions}"/> (see <c>DeterministicPublishingRegressionTests</c>).
/// </summary>
public sealed class PlatformVerificationPipelineTests
{
    [Fact]
    public async Task VerifyAsync_marks_build_and_test_blocked_when_dotnet_sdk_is_unavailable()
    {
        var dotNet = new FakeDotNetCliExecutor { InspectResult = new ProcessExecutionResult { ExitCode = -1, StandardError = "Unable to start dotnet." } };
        var pipeline = CreatePipeline(dotNet, SuccessfulSolutionResult());

        var report = await pipeline.VerifyAsync(new PlatformVerificationRequest { OutputDirectory = TempDir() });

        Assert.Equal(VerificationOutcome.Blocked, report.BuildStatus.Outcome);
        Assert.Equal(VerificationOutcome.Blocked, report.TestStatus.Outcome);
        Assert.False(report.OverallResult);
        Assert.NotNull(report.BuildStatus.RootCause);
    }

    [Fact]
    public async Task VerifyAsync_skips_build_and_test_when_not_requested()
    {
        var dotNet = new FakeDotNetCliExecutor();
        var pipeline = CreatePipeline(dotNet, SuccessfulSolutionResult());

        var report = await pipeline.VerifyAsync(new PlatformVerificationRequest
        {
            OutputDirectory = TempDir(),
            VerifySolutionBuildAndTest = false
        });

        Assert.Equal(VerificationOutcome.Skipped, report.BuildStatus.Outcome);
        Assert.Equal(VerificationOutcome.Skipped, report.TestStatus.Outcome);
        Assert.False(dotNet.InspectCalled);
    }

    [Fact]
    public async Task VerifyAsync_reports_overall_success_when_every_stage_passes()
    {
        var dotNet = new FakeDotNetCliExecutor();
        var solutionResult = SuccessfulSolutionResult();
        var pipeline = CreatePipeline(dotNet, solutionResult);

        var report = await pipeline.VerifyAsync(new PlatformVerificationRequest { OutputDirectory = TempDir() });

        Assert.True(report.BuildStatus.IsPassed);
        Assert.True(report.TestStatus.IsPassed);
        Assert.True(report.PublishStatus.IsPassed);
        Assert.True(report.OverallResult);
        Assert.NotNull(report.ReportPath);
        Assert.True(File.Exists(report.ReportPath));

        // Sprint M3.4: with no projects in this fake solution result, deploy-time lifecycle
        // checks that need a real published bundle are Skipped (acceptable), while the two pure
        // source-wiring checks run for real against this checked-out repository and pass.
        Assert.Equal(VerificationOutcome.Skipped, report.HealthGateValidation.Outcome);
        Assert.Equal(VerificationOutcome.Skipped, report.DeploymentCertificationValidation.Outcome);
        Assert.Equal(VerificationOutcome.Skipped, report.RollbackReadinessValidation.Outcome);
        Assert.True(report.RuntimeStartupValidation.IsPassed);
        Assert.True(report.ReconciliationInitializationValidation.IsPassed);
    }

    [Fact]
    public async Task VerifyAsync_reports_overall_failure_with_root_cause_when_solution_publish_fails()
    {
        var dotNet = new FakeDotNetCliExecutor();
        var solutionResult = new SolutionPublishingResult
        {
            IsSuccess = false,
            Message = "1 of 1 project(s) failed.",
            ProjectResults = []
        };
        var pipeline = CreatePipeline(dotNet, solutionResult);

        var report = await pipeline.VerifyAsync(new PlatformVerificationRequest { OutputDirectory = TempDir() });

        Assert.False(report.PublishStatus.IsPassed);
        Assert.False(report.OverallResult);
        Assert.NotNull(report.PublishStatus.RootCause);
    }

    [Fact]
    public async Task VerifyAsync_skips_publish_and_stays_deterministic_when_solution_build_fails()
    {
        // Deterministic ordering: a proven build failure must not be papered over by a publish
        // attempt that would just re-fail per project and muddy the root cause.
        var dotNet = new FakeDotNetCliExecutor
        {
            BuildResult = new ProcessExecutionResult { ExitCode = 1, StandardError = "CS1002: ; expected" }
        };
        var solutionPublisher = new FakeSolutionPublishingOrchestrator(SuccessfulSolutionResult());
        var pipeline = new PlatformVerificationPipeline(
            dotNet,
            solutionPublisher,
            new FakeDeployPackageReader(),
            new TestOptionsMonitor(new PublishingSettings()),
            NullLogger<PlatformVerificationPipeline>.Instance);

        var report = await pipeline.VerifyAsync(new PlatformVerificationRequest { OutputDirectory = TempDir() });

        Assert.Equal(VerificationOutcome.Failed, report.BuildStatus.Outcome);
        Assert.Equal(VerificationOutcome.Skipped, report.PublishStatus.Outcome);
        Assert.Equal(VerificationOutcome.Skipped, report.TestStatus.Outcome);
        Assert.False(report.OverallResult);
        Assert.False(solutionPublisher.PublishCalled);
    }

    private static PlatformVerificationPipeline CreatePipeline(
        FakeDotNetCliExecutor dotNet,
        SolutionPublishingResult solutionResult)
        => new(
            dotNet,
            new FakeSolutionPublishingOrchestrator(solutionResult),
            new FakeDeployPackageReader(),
            new TestOptionsMonitor(new PublishingSettings()),
            NullLogger<PlatformVerificationPipeline>.Instance);

    private static SolutionPublishingResult SuccessfulSolutionResult()
        => new()
        {
            IsSuccess = true,
            Message = "All project(s) published successfully.",
            ProjectResults = []
        };

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "m3.3-verification-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private sealed class FakeDotNetCliExecutor : IDotNetCliExecutor
    {
        public ProcessExecutionResult InspectResult { get; set; } = new() { ExitCode = 0, StandardOutput = "10.0.0" };
        public ProcessExecutionResult BuildResult { get; set; } = new() { ExitCode = 0 };
        public bool InspectCalled { get; private set; }

        public Task<ProcessExecutionResult> RestoreAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult { ExitCode = 0 });

        public Task<ProcessExecutionResult> BuildAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken)
            => Task.FromResult(BuildResult);

        public Task<ProcessExecutionResult> PublishAsync(string projectPath, string outputDirectory, string workingDirectory, IReadOnlyDictionary<string, string> publishProperties, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult { ExitCode = 0 });

        public Task<ProcessExecutionResult> TestAsync(string projectOrSolutionPath, string workingDirectory, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult { ExitCode = 0 });

        public Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken)
        {
            InspectCalled = true;
            return Task.FromResult(InspectResult);
        }
    }

    private sealed class FakeSolutionPublishingOrchestrator(SolutionPublishingResult result) : ISolutionPublishingOrchestrator
    {
        public bool PublishCalled { get; private set; }

        public Task<SolutionPublishingResult> PublishSolutionAsync(
            SolutionPublishingRequest request,
            IProgress<SolutionPublishingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            PublishCalled = true;
            return Task.FromResult(result);
        }
    }

    private sealed class FakeDeployPackageReader : IDeployPackageReader
    {
        public Task<DeployPackageContents?> ReadAsync(string packagePath, CancellationToken cancellationToken = default)
            => Task.FromResult<DeployPackageContents?>(null);
    }

    private sealed class TestOptionsMonitor(PublishingSettings value) : IOptionsMonitor<PublishingSettings>
    {
        public PublishingSettings CurrentValue { get; } = value;
        public PublishingSettings Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<PublishingSettings, string?> listener) => null;
    }
}

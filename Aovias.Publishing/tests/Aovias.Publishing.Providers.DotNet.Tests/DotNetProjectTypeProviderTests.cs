using Aovias.Publishing.Contracts;
using Aovias.Publishing.Core.Execution;
using Aovias.Publishing.Providers.DotNet;
using Xunit;

namespace Aovias.Publishing.Providers.DotNet.Tests;

public sealed class DotNetProjectTypeProviderTests
{
    [Fact]
    public async Task DiscoverAsync_detects_aspnet_core_project()
    {
        var root = Path.Combine(Path.GetTempPath(), "ape-dotnet-test", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var projectPath = Path.Combine(root, "SampleApi.csproj");
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        try
        {
            var provider = new DotNetProjectTypeProvider(new FakeDotNetCliExecutor());
            var discovery = await provider.DiscoverAsync(projectPath, CancellationToken.None);

            Assert.NotNull(discovery);
            Assert.Equal(PublishingProjectType.AspNetCore, discovery!.ProjectType);
            Assert.Equal("SampleApi", discovery.ServiceId);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class FakeDotNetCliExecutor : IDotNetCliExecutor
    {
        public Task<ProcessExecutionResult> RestoreAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult());

        public Task<ProcessExecutionResult> BuildAsync(string projectPath, string workingDirectory, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult());

        public Task<ProcessExecutionResult> PublishAsync(string projectPath, string outputDirectory, string workingDirectory, IReadOnlyDictionary<string, string> publishProperties, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult());

        public Task<ProcessExecutionResult> TestAsync(string projectOrSolutionPath, string workingDirectory, CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult());

        public Task<ProcessExecutionResult> InspectAsync(CancellationToken cancellationToken)
            => Task.FromResult(new ProcessExecutionResult { StandardOutput = "10.0.0" });
    }
}

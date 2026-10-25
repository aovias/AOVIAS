using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.SDK;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aovias.DeploymentValidation.Tests.Reliability;

public sealed class DeploymentUploadFailureTests
{
    [Fact]
    public void Transport_failure_preserves_complete_exception_chain_and_socket_reason()
    {
        var socket = new SocketException((int)SocketError.ConnectionReset);
        var io = new IOException("Network stream ended while sending chunk 7.", socket);
        var request = new HttpRequestException("HTTP content copy failed.", io);

        var failure = DeploymentFailureMapper.FromException(
            request,
            "Upload",
            "DeploymentClient",
            "UploadChunk",
            DeploymentFailureCategory.Upload,
            correlationId: "deployment-123");

        Assert.Equal(3, failure.ExceptionChain.Count);
        Assert.Equal("ConnectionReset", failure.ExceptionChain[^1].SocketError);
        Assert.True(failure.IsTransient);
        Assert.Equal("deployment-123", failure.CorrelationId);
    }

    [Fact]
    public void Validation_failure_is_not_classified_as_transient()
    {
        var failure = DeploymentFailureMapper.FromException(
            new InvalidDataException("Chunk checksum mismatch."),
            "Upload",
            "DeploymentPackageStore",
            "UploadChunk",
            DeploymentFailureCategory.Upload);

        Assert.False(failure.IsTransient);
    }

    [Fact]
    public async Task Deployment_client_preserves_selected_runtime_path_base_and_correlation_id()
    {
        string? capturedPath = null;
        string? capturedCorrelation = null;
        var handler = new StubHandler(request =>
        {
            capturedPath = request.RequestUri!.AbsolutePath;
            capturedCorrelation = request.Headers.GetValues("X-Correlation-Id").Single();
            var payload = new ChunkedUploadSession
            {
                UploadId = "upload-1",
                DeploymentId = "deployment-1",
                FileName = "gateway.aovdeploy"
            };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload))
            });
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://demo.example/runtime") };
        var client = new DeploymentClient(
            http,
            new StaticOptionsMonitor<RemoteDeploymentSettings>(new RemoteDeploymentSettings()));

        await client.InitiateChunkedUploadAsync(new InitiateChunkedUploadRequest
        {
            DeploymentId = "deployment-1",
            ServiceId = "gateway",
            FileName = "gateway.aovdeploy",
            TotalBytes = 10,
            Checksum = new string('a', 64)
        });

        Assert.Equal("/runtime/api/deployment/upload/initiate", capturedPath);
        Assert.Equal("deployment-1", capturedCorrelation);
    }

    [Fact]
    public async Task Legacy_release_upload_reopens_stream_for_transient_retry()
    {
        var attempts = 0;
        var receivedLengths = new List<int>();
        var handler = new StubHandler(async request =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new HttpRequestException(
                    "connection reset",
                    new SocketException((int)SocketError.ConnectionReset));
            }

            receivedLengths.Add((await request.Content!.ReadAsByteArrayAsync()).Length);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    {"releaseId":"r1","packageId":"gateway","packagePath":"/incoming/gateway.aovdeploy","state":"Uploaded"}
                    """)
            };
        });
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://demo.example/runtime") };
        var settings = new ReleaseManagementSettings
        {
            UploadEndpoint = "/api/releases/upload",
            ValidateEndpoint = "/api/releases/validate",
            PlanEndpoint = "/api/releases/plan",
            DeployEndpoint = "/api/releases/deploy",
            RollbackEndpoint = "/api/releases/rollback",
            ListEndpoint = "/api/releases",
            HistoryEndpoint = "/api/releases/history",
            DetailEndpointTemplate = "/api/releases/{id}",
            SupportedPackageVersion = "1.0.0",
            SupportedRuntimeVersion = "1.0.0",
            MaximumPackageSizeBytes = 1024 * 1024,
            TimeoutSeconds = 30,
            RetryCount = 1
        };
        var packagePath = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(packagePath, RandomNumberGenerator.GetBytes(4096));
            var client = new ReleaseClient(
                http,
                new StaticOptionsMonitor<ReleaseManagementSettings>(settings));

            var result = await client.UploadPackageAsync(
                packagePath,
                "runtime",
                "demo",
                "tester");

            Assert.Equal("r1", result.ReleaseId);
            Assert.Equal(2, attempts);
            Assert.Single(receivedLengths);
            Assert.True(receivedLengths[0] > 4096);
        }
        finally
        {
            File.Delete(packagePath);
        }
    }

    private sealed class StubHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => send(request);
    }

    private sealed class StaticOptionsMonitor<T>(T value) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = value;

        public T Get(string? name) => CurrentValue;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

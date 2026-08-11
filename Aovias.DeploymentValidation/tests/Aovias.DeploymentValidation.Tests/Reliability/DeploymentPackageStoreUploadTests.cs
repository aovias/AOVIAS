using System.Security.Cryptography;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Runtime.Agent.Infrastructure.Deployment;
using Xunit;

namespace Aovias.DeploymentValidation.Tests.Reliability;

public sealed class DeploymentPackageStoreUploadTests
{
    [Fact]
    public async Task Chunked_upload_verifies_chunks_and_commits_package_atomically()
    {
        await WithStoreAsync(async (store, packageRoot) =>
        {
            var payload = RandomNumberGenerator.GetBytes((4 * 1024 * 1024) + 137);
            var request = Request("deploy-1", "gateway.aovdeploy", payload);
            var session = store.Initiate(request);

            await UploadAllAsync(store, session, payload);
            var committed = await store.CompleteAsync(session.UploadId);

            Assert.Equal(payload, await File.ReadAllBytesAsync(committed));
            Assert.Equal("Completed", store.Get(session.UploadId).State);
            Assert.StartsWith(packageRoot.Replace('\\', '/'), committed.Replace('\\', '/'), StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Upload_resumes_after_store_recreation_without_resending_acknowledged_chunks()
    {
        await WithStoreAsync(async (store, packageRoot) =>
        {
            var payload = RandomNumberGenerator.GetBytes((4 * 1024 * 1024) + 31);
            var request = Request("deploy-resume", "gateway.aovdeploy", payload);
            var session = store.Initiate(request);
            await UploadChunkAsync(store, session, payload, 0);

            var resolver = RuntimeAgentDeploymentTestSupport.CreateResolver(
                Path.Combine(Path.GetDirectoryName(packageRoot)!, "releases"),
                packageRoot);
            var restartedStore = new DeploymentPackageStore(resolver);
            var resumed = restartedStore.Initiate(request);

            Assert.Equal(session.UploadId, resumed.UploadId);
            Assert.Contains(0, resumed.ReceivedChunkIndices);
            await UploadChunkAsync(restartedStore, resumed, payload, 1);
            var committed = await restartedStore.CompleteAsync(resumed.UploadId);
            Assert.Equal(payload, await File.ReadAllBytesAsync(committed));
        });
    }

    [Fact]
    public async Task Corrupt_chunk_is_rejected_before_it_is_acknowledged()
    {
        await WithStoreAsync(async (store, _) =>
        {
            var payload = RandomNumberGenerator.GetBytes(1024 * 1024);
            var session = store.Initiate(Request("deploy-bad", "gateway.aovdeploy", payload));
            await using var stream = new MemoryStream(payload);

            var exception = await Assert.ThrowsAsync<UploadRejectedException>(() =>
                store.UploadChunkAsync(
                    session.UploadId,
                    0,
                    new UploadChunkMetadata
                    {
                        DeploymentId = session.DeploymentId,
                        Checksum = new string('0', 64),
                        ContentLength = payload.Length
                    },
                    stream));

            Assert.Contains("checksum mismatch", exception.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(store.Get(session.UploadId).ReceivedChunkIndices);
        });
    }

    [Fact]
    public async Task Simultaneous_deployments_have_independent_sessions()
    {
        await WithStoreAsync(async (store, _) =>
        {
            var first = RandomNumberGenerator.GetBytes(512 * 1024);
            var second = RandomNumberGenerator.GetBytes(768 * 1024);
            var firstSession = store.Initiate(Request("deploy-a", "gateway.aovdeploy", first));
            var secondSession = store.Initiate(Request("deploy-b", "identity.aovdeploy", second));

            await Task.WhenAll(
                UploadAllAsync(store, firstSession, first),
                UploadAllAsync(store, secondSession, second));

            Assert.NotEqual(firstSession.UploadId, secondSession.UploadId);
            Assert.Equal(first.Length, store.Get(firstSession.UploadId).BytesReceived);
            Assert.Equal(second.Length, store.Get(secondSession.UploadId).BytesReceived);
        });
    }

    private static InitiateChunkedUploadRequest Request(string deploymentId, string fileName, byte[] payload)
        => new()
        {
            DeploymentId = deploymentId,
            ServiceId = Path.GetFileNameWithoutExtension(fileName),
            FileName = fileName,
            TotalBytes = payload.Length,
            Checksum = Sha256(payload),
            ChunkSizeBytes = 4 * 1024 * 1024
        };

    private static async Task UploadAllAsync(
        DeploymentPackageStore store,
        ChunkedUploadSession session,
        byte[] payload)
    {
        for (var index = 0; index < session.TotalChunks; index++)
        {
            await UploadChunkAsync(store, session, payload, index);
        }
    }

    private static async Task UploadChunkAsync(
        DeploymentPackageStore store,
        ChunkedUploadSession session,
        byte[] payload,
        int index)
    {
        var offset = index * session.ChunkSizeBytes;
        var length = Math.Min(session.ChunkSizeBytes, payload.Length - offset);
        var chunk = payload.AsSpan(offset, length).ToArray();
        await using var stream = new MemoryStream(chunk);
        await store.UploadChunkAsync(
            session.UploadId,
            index,
            new UploadChunkMetadata
            {
                DeploymentId = session.DeploymentId,
                Checksum = Sha256(chunk),
                ContentLength = chunk.Length
            },
            stream);
    }

    private static string Sha256(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static async Task WithStoreAsync(Func<DeploymentPackageStore, string, Task> test)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"aovias-upload-{Guid.NewGuid():N}");
        var releaseRoot = Path.Combine(tempRoot, "releases");
        var packageRoot = Path.Combine(tempRoot, "runtime", "packages");
        var resolver = RuntimeAgentDeploymentTestSupport.CreateResolver(releaseRoot, packageRoot);
        try
        {
            await test(new DeploymentPackageStore(resolver), packageRoot);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}

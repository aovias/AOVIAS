using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Aovias.Release.Contracts.RemoteDeployment;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Stores uploaded deployment packages with current/previous/archive retention.
/// </summary>
public sealed class DeploymentPackageStore
{
    private const int MinimumChunkSizeBytes = 256 * 1024;
    private const int MaximumChunkSizeBytes = 16 * 1024 * 1024;
    private const long MinimumDiskReserveBytes = 64L * 1024 * 1024;

    private readonly ConcurrentDictionary<string, ChunkUploadState> uploads = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> uploadLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly RuntimeAgentDeploymentPathResolver pathResolver;
    private readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public DeploymentPackageStore(RuntimeAgentDeploymentPathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
        LoadPersistedSessions();
    }

    public ChunkedUploadSession Initiate(InitiateChunkedUploadRequest request)
    {
        ValidateInitiateRequest(request);
        EnsureStoreRoot();

        var existing = uploads.Values.FirstOrDefault(state =>
            !state.Completed
            && string.Equals(state.DeploymentId, request.DeploymentId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(state.FileName, request.FileName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(state.Checksum, request.Checksum, StringComparison.OrdinalIgnoreCase)
            && state.TotalBytes == request.TotalBytes
            && state.ChunkSizeBytes == request.ChunkSizeBytes);
        if (existing is not null)
        {
            return existing.ToSession();
        }

        ValidateStorage(request.TotalBytes);
        var uploadId = Guid.NewGuid().ToString("N");
        var totalChunks = (int)Math.Ceiling(request.TotalBytes / (double)Math.Max(request.ChunkSizeBytes, 1));
        var state = new ChunkUploadState
        {
            UploadId = uploadId,
            DeploymentId = request.DeploymentId,
            ServiceId = request.ServiceId,
            FileName = request.FileName,
            TotalBytes = request.TotalBytes,
            Checksum = request.Checksum,
            ChunkSizeBytes = request.ChunkSizeBytes,
            TotalChunks = totalChunks,
            TempPath = Path.Combine(GetSessionRoot(), $"{uploadId}.part"),
            CreatedUtc = DateTimeOffset.UtcNow,
            UpdatedUtc = DateTimeOffset.UtcNow
        };

        uploads[uploadId] = state;
        Persist(state);
        return state.ToSession();
    }

    public async Task<ChunkedUploadSession> UploadChunkAsync(
        string uploadId,
        int chunkIndex,
        UploadChunkMetadata metadata,
        Stream chunkStream,
        CancellationToken cancellationToken = default)
    {
        var state = RequireSession(uploadId);
        ValidateChunk(state, chunkIndex, metadata);
        var gate = uploadLocks.GetOrAdd(uploadId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (state.ReceivedChunkIndices.Contains(chunkIndex))
            {
                return state.ToSession();
            }

            await using var bufferedChunk = new MemoryStream();
            await chunkStream.CopyToAsync(bufferedChunk, cancellationToken);
            if (bufferedChunk.Length != metadata.ContentLength)
            {
                throw UploadRejectedException.Validation(
                    $"Chunk {chunkIndex} length mismatch. Expected {metadata.ContentLength} bytes; received {bufferedChunk.Length} bytes.",
                    "Retry this chunk. If the mismatch repeats, inspect the proxy for request-body truncation.");
            }

            bufferedChunk.Position = 0;
            var actualChecksum = Convert.ToHexString(
                await SHA256.HashDataAsync(bufferedChunk, cancellationToken)).ToLowerInvariant();
            if (!string.Equals(actualChecksum, metadata.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                throw UploadRejectedException.Validation(
                    $"Chunk {chunkIndex} checksum mismatch. Expected {metadata.Checksum}; received {actualChecksum}.",
                    "Retry this chunk. Do not retry finalization until every chunk checksum passes.");
            }

            bufferedChunk.Position = 0;
            await using var destination = new FileStream(
                state.TempPath,
                FileMode.OpenOrCreate,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);
            destination.Seek(chunkIndex * (long)state.ChunkSizeBytes, SeekOrigin.Begin);
            await bufferedChunk.CopyToAsync(destination, cancellationToken);
            await destination.FlushAsync(cancellationToken);

            state.ReceivedChunkIndices.Add(chunkIndex);
            state.ChunkLengths[chunkIndex] = metadata.ContentLength;
            state.BytesReceived = state.ChunkLengths.Values.Sum();
            state.UpdatedUtc = DateTimeOffset.UtcNow;
            Persist(state);
            return state.ToSession();
        }
        catch (UnauthorizedAccessException exception)
        {
            throw UploadRejectedException.Storage(
                $"Upload rejected: Runtime Agent cannot write to '{state.TempPath}'.",
                "Grant the Runtime Agent write permission to the package store.",
                exception);
        }
        catch (IOException exception) when (IsDiskFull(exception))
        {
            throw UploadRejectedException.InsufficientStorage(
                state.TotalBytes,
                GetAvailableDiskBytes(),
                exception);
        }
        finally
        {
            gate.Release();
        }
    }

    public ChunkedUploadSession Get(string uploadId)
        => RequireSession(uploadId).ToSession();

    public async Task<string> CompleteAsync(string uploadId, CancellationToken cancellationToken = default)
    {
        var state = RequireSession(uploadId);
        var gate = uploadLocks.GetOrAdd(uploadId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (state.Completed && !string.IsNullOrWhiteSpace(state.PackagePath) && File.Exists(state.PackagePath))
            {
                return state.PackagePath;
            }

            var missing = Enumerable.Range(0, state.TotalChunks)
                .Where(index => !state.ReceivedChunkIndices.Contains(index))
                .ToList();
            if (missing.Count > 0)
            {
                throw UploadRejectedException.Validation(
                    $"Upload incomplete: {missing.Count} chunk(s) missing. Resume at chunk {missing[0]} of {state.TotalChunks}.",
                    "Query the upload session, upload only missing chunks, then finalize again.");
            }

            var fileInfo = new FileInfo(state.TempPath);
            if (!fileInfo.Exists || fileInfo.Length != state.TotalBytes)
            {
                throw UploadRejectedException.Validation(
                    $"Uploaded package length mismatch. Expected {state.TotalBytes} bytes; found {(fileInfo.Exists ? fileInfo.Length : 0)} bytes.",
                    "Resume the upload. The Runtime Agent will report which chunks must be sent.");
            }

            string checksum;
            await using (var package = File.OpenRead(state.TempPath))
            {
                checksum = Convert.ToHexString(
                    await SHA256.HashDataAsync(package, cancellationToken)).ToLowerInvariant();
            }

            if (!string.Equals(checksum, state.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                throw UploadRejectedException.Validation(
                    $"Package checksum mismatch. Expected {state.Checksum}; received {checksum}.",
                    "Discard this upload session and begin a new upload from the original package.");
            }

            var destination = Path.Combine(GetStoreRoot(), state.FileName);
            File.Move(state.TempPath, destination, overwrite: true);
            state.Completed = true;
            state.State = "Completed";
            state.PackagePath = destination;
            state.UpdatedUtc = DateTimeOffset.UtcNow;
            Persist(state);
            return destination;
        }
        finally
        {
            gate.Release();
        }
    }

    private void EnsureStoreRoot()
    {
        var root = GetStoreRoot();
        pathResolver.EnsureDirectory(root);
        pathResolver.EnsureDirectory(Path.Combine(root, "current"));
        pathResolver.EnsureDirectory(Path.Combine(root, "previous"));
        pathResolver.EnsureDirectory(Path.Combine(root, "archive"));
        pathResolver.EnsureDirectory(GetSessionRoot());
    }

    private string GetStoreRoot()
        => pathResolver.ResolvePackageStoreRoot();

    private string GetSessionRoot()
        => Path.Combine(GetStoreRoot(), ".uploads");

    private string GetStatePath(string uploadId)
        => Path.Combine(GetSessionRoot(), $"{uploadId}.json");

    private void ValidateInitiateRequest(InitiateChunkedUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeploymentId))
        {
            throw UploadRejectedException.Validation("DeploymentId is required.", "Create a deployment correlation id before upload.");
        }

        if (request.TotalBytes <= 0)
        {
            throw UploadRejectedException.Validation("Package size must be greater than zero.", "Republish the package and retry.");
        }

        if (request.ChunkSizeBytes is < MinimumChunkSizeBytes or > MaximumChunkSizeBytes)
        {
            throw UploadRejectedException.Validation(
                $"Chunk size must be between {MinimumChunkSizeBytes} and {MaximumChunkSizeBytes} bytes.",
                "Use the configured 4 MB default unless the network requires a different supported size.");
        }

        if (request.Checksum.Length != 64 || !request.Checksum.All(Uri.IsHexDigit))
        {
            throw UploadRejectedException.Validation("A valid SHA-256 package checksum is required.", "Recalculate the package checksum before upload.");
        }

        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName)
            || !string.Equals(safeFileName, request.FileName, StringComparison.Ordinal))
        {
            throw UploadRejectedException.Validation("Package filename is invalid.", "Use only the package filename, without directories.");
        }
    }

    private void ValidateStorage(long packageBytes)
    {
        var root = GetStoreRoot();
        try
        {
            var probe = Path.Combine(root, $".write-probe-{Guid.NewGuid():N}");
            using (File.Create(probe, 1, FileOptions.DeleteOnClose))
            {
            }
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException)
        {
            throw UploadRejectedException.Storage(
                $"Upload rejected: package store '{root}' is not writable.",
                "Correct ownership and write permissions for the Runtime Agent package store.",
                exception);
        }

        var available = GetAvailableDiskBytes();
        var required = checked(packageBytes + MinimumDiskReserveBytes);
        if (available >= 0 && available < required)
        {
            throw UploadRejectedException.InsufficientStorage(required, available);
        }
    }

    private long GetAvailableDiskBytes()
    {
        try
        {
            var root = Path.GetPathRoot(Path.GetFullPath(GetStoreRoot()));
            return string.IsNullOrWhiteSpace(root) ? -1 : new DriveInfo(root).AvailableFreeSpace;
        }
        catch
        {
            return -1;
        }
    }

    private static void ValidateChunk(ChunkUploadState state, int chunkIndex, UploadChunkMetadata metadata)
    {
        if (!string.Equals(state.DeploymentId, metadata.DeploymentId, StringComparison.OrdinalIgnoreCase))
        {
            throw UploadRejectedException.Conflict(
                $"Chunk correlation id '{metadata.DeploymentId}' does not match upload deployment '{state.DeploymentId}'.",
                "Query the upload session and resend the chunk with its original deployment correlation id.");
        }

        if (state.Completed)
        {
            throw UploadRejectedException.Conflict(
                $"Upload '{state.UploadId}' is already complete.",
                "Use the committed package or start a new deployment.");
        }

        if (chunkIndex < 0 || chunkIndex >= state.TotalChunks)
        {
            throw UploadRejectedException.Validation(
                $"Chunk index {chunkIndex} is outside the valid range 0..{state.TotalChunks - 1}.",
                "Query upload status and send a valid missing chunk.");
        }

        var expectedLength = chunkIndex == state.TotalChunks - 1
            ? state.TotalBytes - ((long)chunkIndex * state.ChunkSizeBytes)
            : state.ChunkSizeBytes;
        if (metadata.ContentLength != expectedLength)
        {
            throw UploadRejectedException.Validation(
                $"Chunk {chunkIndex} length is invalid. Expected {expectedLength} bytes; declared {metadata.ContentLength} bytes.",
                "Re-read this chunk from the package at the correct byte offset.");
        }

        if (metadata.Checksum.Length != 64 || !metadata.Checksum.All(Uri.IsHexDigit))
        {
            throw UploadRejectedException.Validation(
                $"Chunk {chunkIndex} requires a SHA-256 checksum.",
                "Calculate and send the chunk checksum.");
        }
    }

    private ChunkUploadState RequireSession(string uploadId)
        => uploads.TryGetValue(uploadId, out var state)
            ? state
            : throw UploadRejectedException.NotFound(
                $"Upload session '{uploadId}' was not found.",
                "Initiate a new upload. If the Runtime Agent restarted, verify its package store is persistent.");

    private void Persist(ChunkUploadState state)
    {
        EnsureStoreRoot();
        var statePath = GetStatePath(state.UploadId);
        var tempPath = $"{statePath}.tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(state, jsonOptions));
        File.Move(tempPath, statePath, overwrite: true);
    }

    private void LoadPersistedSessions()
    {
        try
        {
            EnsureStoreRoot();
            foreach (var path in Directory.EnumerateFiles(GetSessionRoot(), "*.json"))
            {
                try
                {
                    var state = JsonSerializer.Deserialize<ChunkUploadState>(File.ReadAllText(path), jsonOptions);
                    if (state is not null)
                    {
                        uploads[state.UploadId] = state;
                    }
                }
                catch (Exception) when (File.Exists(path))
                {
                    // A corrupt state file is isolated; other resumable sessions remain available.
                }
            }
        }
        catch
        {
            // Startup storage diagnostics report the authoritative package-store failure.
        }
    }

    private static bool IsDiskFull(IOException exception)
        => exception.HResult is unchecked((int)0x80070070) or unchecked((int)0x80070027)
            || exception.Message.Contains("space", StringComparison.OrdinalIgnoreCase);

    internal sealed class ChunkUploadState
    {
        public required string UploadId { get; init; }

        public required string DeploymentId { get; init; }

        public string ServiceId { get; init; } = string.Empty;

        public required string FileName { get; init; }

        public long TotalBytes { get; init; }

        public string Checksum { get; init; } = string.Empty;

        public int ChunkSizeBytes { get; init; }

        public HashSet<int> ReceivedChunkIndices { get; init; } = [];

        public Dictionary<int, long> ChunkLengths { get; init; } = [];

        public long BytesReceived { get; set; }

        public int TotalChunks { get; init; }

        public string State { get; set; } = "Uploading";

        public DateTimeOffset CreatedUtc { get; init; }

        public DateTimeOffset UpdatedUtc { get; set; }

        public bool Completed { get; set; }

        public required string TempPath { get; init; }

        public string? PackagePath { get; set; }

        public ChunkedUploadSession ToSession()
            => new()
            {
                UploadId = UploadId,
                DeploymentId = DeploymentId,
                ProtocolVersion = 1,
                FileName = FileName,
                TotalBytes = TotalBytes,
                Checksum = Checksum,
                ChunkSizeBytes = ChunkSizeBytes,
                ReceivedChunks = ReceivedChunkIndices.Count,
                ReceivedChunkIndices = ReceivedChunkIndices.Order().ToList(),
                TotalChunks = TotalChunks,
                BytesReceived = BytesReceived,
                ProgressPercent = TotalBytes == 0 ? 0 : (int)Math.Round(BytesReceived * 100d / TotalBytes),
                State = State,
                CreatedUtc = CreatedUtc,
                UpdatedUtc = UpdatedUtc,
                Completed = Completed,
                PackagePath = PackagePath
            };
    }
}

public sealed class UploadRejectedException : Exception
{
    private UploadRejectedException(int statusCode, string message, string recovery, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        Recovery = recovery;
    }

    public int StatusCode { get; }

    public string Recovery { get; }

    public static UploadRejectedException Validation(string message, string recovery)
        => new(400, message, recovery);

    public static UploadRejectedException NotFound(string message, string recovery)
        => new(404, message, recovery);

    public static UploadRejectedException Conflict(string message, string recovery)
        => new(409, message, recovery);

    public static UploadRejectedException Storage(string message, string recovery, Exception? innerException = null)
        => new(500, message, recovery, innerException);

    public static UploadRejectedException InsufficientStorage(long requiredBytes, long availableBytes, Exception? innerException = null)
        => new(
            507,
            $"Upload rejected: insufficient disk space. Required {requiredBytes} bytes; available {availableBytes} bytes.",
            "Free package-store disk space or increase the Runtime Agent volume, then resume the upload.",
            innerException);
}

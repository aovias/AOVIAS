namespace ComplianceService.Application.Contracts;

/// <summary>
/// Abstraction for evidence file persistence (local disk or Azure Blob via <c>Storage:Provider</c>).
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Writes the stream to storage under <paramref name="relativePath"/> with <paramref name="fileName"/>.
    /// Does not dispose <paramref name="stream"/>.
    /// </summary>
    /// <returns>
    /// URI to store in <see cref="Domain.Evidence.FileUrl"/> — e.g. <c>local://...</c> or <c>azureblob://...</c>.
    /// </returns>
    Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string contentType,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a read stream for a path previously returned by <see cref="SaveAsync"/>.
    /// Caller must dispose the returned stream.
    /// </summary>
    Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the file at <paramref name="path"/>. No-op if the file does not exist.
    /// </summary>
    Task DeleteAsync(string path, CancellationToken cancellationToken = default);
}

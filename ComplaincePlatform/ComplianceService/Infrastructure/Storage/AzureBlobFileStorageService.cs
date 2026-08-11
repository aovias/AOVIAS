using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ComplianceService.Application.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Infrastructure.Storage;

/// <summary>
/// Azure Blob Storage implementation of <see cref="IFileStorageService"/>.
/// Selected when <c>Storage:Provider=AzureBlob</c>. Returns <c>azureblob://{container}/{blob}</c> URIs.
/// </summary>
public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _container;
    private readonly ILogger<AzureBlobFileStorageService> _logger;

    public AzureBlobFileStorageService(IConfiguration configuration, ILogger<AzureBlobFileStorageService> logger)
    {
        _logger = logger;
        var connectionString = configuration["Storage:AzureBlob:ConnectionString"]
            ?? configuration.GetConnectionString("AzureBlob")
            ?? throw new InvalidOperationException(
                "Storage:Provider=AzureBlob requires Storage:AzureBlob:ConnectionString or ConnectionStrings:AzureBlob.");
        var containerName = configuration["Storage:AzureBlob:ContainerName"] ?? "aovias-files";

        var service = new BlobServiceClient(connectionString);
        _container = service.GetBlobContainerClient(containerName);
    }

    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string contentType,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);

        await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var blobName = BuildBlobName(relativePath, fileName);
        var blob = _container.GetBlobClient(blobName);

        var headers = new BlobHttpHeaders
        {
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        };

        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = headers }, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Uploaded blob {BlobName} to container {Container}", blobName, _container.Name);
        return $"azureblob://{_container.Name}/{blobName}";
    }

    public async Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        var blobName = ResolveBlobName(path);
        var blob = _container.GetBlobClient(blobName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return response.Value.Content;
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        var blobName = ResolveBlobName(path);
        await _container.GetBlobClient(blobName)
            .DeleteIfExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private static string BuildBlobName(string relativePath, string fileName)
    {
        var combined = Path.Combine(relativePath ?? string.Empty, fileName ?? string.Empty)
            .Replace("\\", "/")
            .TrimStart('/');
        return combined;
    }

    private string ResolveBlobName(string path)
    {
        if (path.StartsWith("azureblob://", StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path["azureblob://".Length..];
            var slash = remainder.IndexOf('/');
            if (slash < 0)
            {
                return remainder;
            }

            var container = remainder[..slash];
            if (!string.Equals(container, _container.Name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Blob path container '{container}' does not match configured container '{_container.Name}'.");
            }

            return remainder[(slash + 1)..];
        }

        if (path.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cannot open local:// paths with AzureBlobFileStorageService. Switch Storage:Provider or migrate objects.");
        }

        return path.TrimStart('/');
    }
}

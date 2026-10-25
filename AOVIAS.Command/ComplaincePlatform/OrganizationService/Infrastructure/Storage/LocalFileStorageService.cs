using OrganizationService.Application.Contracts;

namespace OrganizationService.Infrastructure.Storage;

public sealed class LocalFileStorageService(IHostEnvironment env) : IFileStorageService
{
    public async Task<string> SaveAsync(
        Stream stream,
        string fileName,
        string contentType,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        _ = contentType;
        ArgumentNullException.ThrowIfNull(stream);

        var fullDir = Path.Combine(env.ContentRootPath, relativePath);
        Directory.CreateDirectory(fullDir);

        var destPath = Path.Combine(fullDir, fileName);

        await using (var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        var relativeUrl = Path.Combine(relativePath, fileName).Replace("\\", "/");
        return $"local://{relativeUrl}";
    }

    public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var fullPath = ResolveLocalPath(path);
        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        var fullPath = ResolveLocalPath(path);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveLocalPath(string path)
    {
        var relative = path.StartsWith("local://", StringComparison.Ordinal)
            ? path["local://".Length..]
            : path;
        relative = relative.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        return Path.GetFullPath(Path.Combine(env.ContentRootPath, relative));
    }
}

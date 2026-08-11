namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Shared filesystem helpers for publishing providers and packaging.
/// Uses relative-path composition (never string replacement) to remain
/// deterministic across operating systems.
/// </summary>
public static class PublishingFileSystem
{
    /// <summary>
    /// Recursively copies a directory tree, overwriting existing files.
    /// </summary>
    public static void CopyDirectory(string source, string destination)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(destination);

        Directory.CreateDirectory(destination);

        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}

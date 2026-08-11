namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Validates publishing project paths for both file-based (.csproj, angular.json) and directory-based discovery roots.
/// </summary>
public static class PublishingProjectPaths
{
    public static bool Exists(string? projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return false;
        }

        return File.Exists(projectPath) || Directory.Exists(projectPath);
    }
}

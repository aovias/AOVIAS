namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Documents publishing path conventions.
/// </summary>
public static class PublishingPathPolicy
{
    /// <summary>
    /// Session workspace: {TemporaryFolder}/sessions/{projectFileName}/{sessionGuid}/.
    /// </summary>
    public const string SessionWorkspacePattern = "{TemporaryFolder}/sessions/{projectFileName}/{sessionGuid}/";

    /// <summary>
    /// Package output: {OutputDirectory}/{serviceId}.aovdeploy (versionless; versioned overload exists but is unused).
    /// </summary>
    public const string PackageOutputPattern = "{OutputDirectory}/{serviceId}.aovdeploy";

    /// <summary>
    /// Release library (solution flow): {releaseLibraryRoot}/{category}/{serviceId}.aovdeploy.
    /// </summary>
    public const string ReleaseLibraryCategoryPattern = "{releaseLibraryRoot}/{category}/{serviceId}.aovdeploy";

    /// <summary>
    /// Release library (CSM flow): flat copy under configured packages root — unified in M1 Step 7.
    /// </summary>
    public const string ReleaseLibraryFlatPattern = "{libraryRoot}/{originalFileName}";
}

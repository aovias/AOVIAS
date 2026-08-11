using Aovias.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace Aovias.Configuration.Deployment;

/// <summary>
/// Synchronizes legacy <see cref="ReleaseManagementSettings"/> path properties from <see cref="IDeploymentPathResolver"/>.
/// </summary>
internal sealed class ReleaseManagementPathOptionsConfigurator : IConfigureOptions<ReleaseManagementSettings>
{
    private readonly IDeploymentPathResolver pathResolver;

    public ReleaseManagementPathOptionsConfigurator(IDeploymentPathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
    }

    public void Configure(ReleaseManagementSettings options)
    {
        options.ReleaseRoot = pathResolver.ReleaseRoot;
        options.PackageRoot = pathResolver.PackagesRoot;
        options.TemporaryFolder = pathResolver.TempRoot;
        options.ArchiveFolder = pathResolver.ArchiveRoot;
        options.IncomingFolder = pathResolver.IncomingRoot;
        options.StagingFolder = pathResolver.StagingRoot;
        options.ExtractionFolder = pathResolver.ExtractionRoot;
        options.ActiveFolder = pathResolver.ActiveRoot;
        options.RollbackFolder = pathResolver.RollbackRoot;
        options.AuditFolder = pathResolver.AuditRoot;
    }
}

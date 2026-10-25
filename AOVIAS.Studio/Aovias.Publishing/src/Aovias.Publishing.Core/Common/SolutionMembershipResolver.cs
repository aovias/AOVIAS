using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Common;

/// <summary>
/// Resolves demo and solution membership from deployment metadata.
/// </summary>
public static class SolutionMembershipResolver
{
    public static bool IsDemoBundleMember(DeployableProject project)
        => project.IncludeInDemoBundle;

    public static bool IsDemoBundleMember(DeploymentMetadata metadata)
        => metadata.IncludeInDemoBundle;

    public static bool IsSolutionDeploymentMember(DeployableProject project)
        => project.IncludeInSolutionDeployment;

    public static IReadOnlyList<DeployableProject> FilterForProfile(
        IReadOnlyList<DeployableProject> projects,
        string? profile)
    {
        if (string.Equals(profile, "demo", StringComparison.OrdinalIgnoreCase))
        {
            return projects.Where(IsDemoBundleMember).ToList();
        }

        return projects.Where(IsSolutionDeploymentMember).ToList();
    }
}

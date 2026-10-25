using Platform.Dashboard.Models;

namespace Platform.Dashboard.Contracts;

/// <summary>
/// Application-specific dashboard provider. Owns business logic for summary contributions and metadata.
/// </summary>
public interface IDashboardProvider
{
    string ApplicationName { get; }

    int Order { get; }

    IReadOnlyList<string> GetPermissions();

    bool SupportsApplication(string applicationId);

    Task<DashboardSummaryContribution> GetSummaryContributionAsync(
        DashboardContext context,
        CancellationToken cancellationToken = default);
}

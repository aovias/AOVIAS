using CommunityToolkit.Mvvm.ComponentModel;

namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Release catalog card for the unified Deploy workflow. Checking the card selects that
/// service/package for deployment — release selection and service selection are the same action.
/// </summary>
public sealed partial class DeploymentCatalogCardItem : ObservableObject
{
    public required string Id { get; init; }

    /// <summary>Service id used for <c>SelectedServiceIds</c> on deploy (from package id).</summary>
    public required string ServiceId { get; init; }

    public required string ServiceName { get; init; }

    public required string PackageVersion { get; init; }

    public required string PublishedDisplay { get; init; }

    public required string PackageSize { get; init; }

    public required string CertificationDisplay { get; init; }

    public required string DependenciesDisplay { get; init; }

    public required string PackagePath { get; init; }

    public required string PackageType { get; init; }

    public string FrameworkDisplay { get; init; } = "AspNetCore";

    public string BuildDisplay { get; init; } = "—";

    public string ChecksumDisplay { get; init; } = "—";

    public string PublisherDisplay { get; init; } = "—";

    public string TagsDisplay { get; init; } = "—";

    public string ServicesIncludedDisplay { get; init; } = "—";

    public string? ReleaseId { get; init; }

    public bool IsPlatformReady { get; init; }

    public bool IsCertified { get; init; }

    public bool NeedsRepublish { get; init; }

    public DateTimeOffset SortDate { get; init; }

    [ObservableProperty]
    private bool isSelected;

    public event Action? SelectionChanged;

    partial void OnIsSelectedChanged(bool value) => SelectionChanged?.Invoke();
}

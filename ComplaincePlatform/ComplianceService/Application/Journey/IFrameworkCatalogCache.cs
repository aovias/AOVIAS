namespace ComplianceService.Application.Journey;

/// <summary>
/// Shared cache for framework static structure (chapters / standards / OE metadata).
/// Designed to be backed by IMemoryCache first and swappable to distributed cache later.
/// </summary>
public interface IFrameworkCatalogCache
{
    Task<FrameworkCatalogSnapshot> GetOrLoadAsync(
        Guid frameworkId,
        string modeKey,
        Func<CancellationToken, Task<FrameworkCatalogSnapshot>> loader,
        CancellationToken cancellationToken = default);

    void InvalidateFramework(Guid frameworkId);

    void InvalidateAll();
}


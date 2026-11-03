namespace AccredaOne.Api.DevelopmentSynthetic;

public interface IDevelopmentSyntheticDataStore
{
    DevelopmentSyntheticDataset GetDataset();
}

public sealed class DevelopmentSyntheticDataStore : IDevelopmentSyntheticDataStore
{
    private readonly Lazy<DevelopmentSyntheticDataset> _dataset;

    public DevelopmentSyntheticDataStore()
    {
        _dataset = new Lazy<DevelopmentSyntheticDataset>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public DevelopmentSyntheticDataset GetDataset() => _dataset.Value;

    private static DevelopmentSyntheticDataset Load()
    {
        var directory = DevelopmentSyntheticDataLoader.ResolveDatasetDirectory();
        return DevelopmentSyntheticDataLoader.LoadFromDirectory(directory);
    }
}

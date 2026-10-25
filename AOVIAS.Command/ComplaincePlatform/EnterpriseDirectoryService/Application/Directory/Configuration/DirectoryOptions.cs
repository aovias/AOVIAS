namespace EnterpriseDirectoryService.Application.Directory.Configuration;

public sealed class DirectoryOptions
{
    public const string SectionName = "Directory";
    public int IndexRebuildIntervalMinutes { get; set; } = 360;
    public int SuggestionCacheTtlMinutes { get; set; } = 5;
    public int ProfileCacheTtlSeconds { get; set; } = 60;
    public int DefaultPageSize { get; set; } = 20;
    public int MaxPageSize { get; set; } = 100;
}

namespace Aovias.Publishing.Contracts;

/// <summary>
/// Supported publishing project types.
/// </summary>
public enum PublishingProjectType
{
    Unknown = 0,
    AspNetCore = 1,
    WorkerService = 2,
    Angular = 3,
    StaticWeb = 4
}

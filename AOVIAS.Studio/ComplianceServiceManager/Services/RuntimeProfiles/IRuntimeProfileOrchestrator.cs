using ComplianceServiceManager.Models.RuntimeProfiles;

namespace ComplianceServiceManager.Services.RuntimeProfiles;

public interface IRuntimeProfileOrchestrator
{
    IReadOnlyList<RuntimeProfileItem> GetProfiles();

    void Reload();

    ProfileValidationResult Validate(RuntimeProfileItem profile);

    void ValidateAll();

    Task StartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default);

    Task StopAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default);

    Task RestartAsync(RuntimeProfileItem profile, CancellationToken cancellationToken = default);

    void SaveProfile(RuntimeProfileItem profile);

    event Action? ProfilesChanged;
}

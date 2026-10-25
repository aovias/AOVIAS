using Microsoft.Extensions.Options;

namespace Aovias.DeploymentValidation.Tests.Reliability;

internal sealed class FixedOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue => value;

    public T Get(string? name) => value;

    public IDisposable? OnChange(Action<T, string?> callback) => null;
}

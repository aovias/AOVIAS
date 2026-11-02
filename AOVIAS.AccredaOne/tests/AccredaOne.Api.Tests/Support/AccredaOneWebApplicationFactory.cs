using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AccredaOne.Api.Tests.Support;

public sealed class AccredaOneWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IConfigurationBuilder>? _configure;
    private readonly IReadOnlyDictionary<string, string?> _useSettings;

    public AccredaOneWebApplicationFactory(
        Action<IConfigurationBuilder>? configure = null,
        IReadOnlyDictionary<string, string?>? useSettings = null)
    {
        _configure = configure;
        _useSettings = useSettings ?? new Dictionary<string, string?>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        foreach (var (key, value) in _useSettings)
        {
            if (value is not null)
            {
                builder.UseSetting(key, value);
            }
        }

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            _configure?.Invoke(configurationBuilder);
        });
    }

    public static AccredaOneWebApplicationFactory WithPersistenceConnectionString(
        string connectionString,
        bool enableConnectivity = false)
    {
        return new AccredaOneWebApplicationFactory(useSettings: new Dictionary<string, string?>
        {
            ["Persistence:ConnectionString"] = connectionString,
            ["Persistence:EnableConnectivity"] = enableConnectivity.ToString()
        });
    }
}

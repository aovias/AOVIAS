using Azure.Identity;

namespace Gateway.Configuration;

public static class KeyVaultConfigurationExtensions
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source when KeyVault:VaultUri is configured.
    /// Uses DefaultAzureCredential (managed identity / az login / env).
    /// </summary>
    public static WebApplicationBuilder AddAoviasKeyVault(this WebApplicationBuilder builder)
    {
        var vaultUri = builder.Configuration["KeyVault:VaultUri"];
        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return builder;
        }

        if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("KeyVault:VaultUri must be an absolute URI.");
        }

        builder.Configuration.AddAzureKeyVault(uri, new DefaultAzureCredential());
        return builder;
    }
}

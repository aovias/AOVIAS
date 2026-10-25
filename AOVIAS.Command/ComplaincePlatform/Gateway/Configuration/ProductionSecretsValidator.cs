namespace Gateway.Configuration;

/// <summary>
/// Phase 7.1 — Production fail-fast when required secrets are missing.
/// Azure Key Vault is wired via configuration provider when KeyVault:VaultUri is set.
/// </summary>
public static class ProductionSecretsValidator
{
    public static void ValidateOrThrow(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            return;
        }

        var missing = new List<string>();
        Require(configuration, "JwtSettings:SecretKey", missing, minLength: 32);
        Require(configuration, "Services:IdentityService:BaseUrl", missing);

        var vaultUri = configuration["KeyVault:VaultUri"];
        var requireVault = string.Equals(
            configuration["KeyVault:RequiredInProduction"],
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (requireVault && string.IsNullOrWhiteSpace(vaultUri))
        {
            missing.Add("KeyVault:VaultUri (KeyVault:RequiredInProduction=true)");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Production secrets validation failed. Missing or invalid: " + string.Join(", ", missing));
        }
    }

    private static void Require(IConfiguration configuration, string key, List<string> missing, int minLength = 1)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || value.Length < minLength)
        {
            missing.Add(key);
        }
    }
}

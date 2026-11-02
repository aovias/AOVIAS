namespace AccredaOne.Api.Persistence;

public static class DatabaseReadinessStatus
{
    public const string NotConfigured = "not_configured";

    public const string Disabled = "disabled";

    public const string Healthy = "healthy";

    public const string Unhealthy = "unhealthy";
}

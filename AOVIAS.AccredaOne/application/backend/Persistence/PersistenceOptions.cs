namespace AccredaOne.Api.Persistence;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    /// <summary>
    /// PostgreSQL connection string. Supply via configuration or environment only; never commit real values.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// When false (default), the API does not open live database connections or report the database as ready.
    /// </summary>
    public bool EnableConnectivity { get; set; }

    /// <summary>
    /// Npgsql connection open timeout in seconds.
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; } = 15;

    /// <summary>
    /// Command timeout passed to the EF Core Npgsql provider.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;

    public bool HasConnectionString =>
        !string.IsNullOrWhiteSpace(ConnectionString);
}

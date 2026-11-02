using Microsoft.Extensions.Options;
using Npgsql;

namespace AccredaOne.Api.Persistence;

public sealed class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions>
{
    private const int MinTimeoutSeconds = 1;

    private const int MaxTimeoutSeconds = 120;

    public ValidateOptionsResult Validate(string? name, PersistenceOptions options)
    {
        var failures = new List<string>();

        if (options.ConnectionTimeoutSeconds is < MinTimeoutSeconds or > MaxTimeoutSeconds)
        {
            failures.Add(
                $"Persistence:{nameof(PersistenceOptions.ConnectionTimeoutSeconds)} must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}.");
        }

        if (options.CommandTimeoutSeconds is < MinTimeoutSeconds or > MaxTimeoutSeconds)
        {
            failures.Add(
                $"Persistence:{nameof(PersistenceOptions.CommandTimeoutSeconds)} must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}.");
        }

        if (options.EnableConnectivity && !options.HasConnectionString)
        {
            failures.Add(
                "Persistence:EnableConnectivity requires a non-empty Persistence:ConnectionString.");
        }

        if (options.HasConnectionString && !TryParseConnectionString(options.ConnectionString!, out _))
        {
            failures.Add("Persistence:ConnectionString is not a valid PostgreSQL connection string.");
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    internal static bool TryParseConnectionString(string connectionString, out NpgsqlConnectionStringBuilder? builder)
    {
        builder = null;
        try
        {
            builder = new NpgsqlConnectionStringBuilder(connectionString);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

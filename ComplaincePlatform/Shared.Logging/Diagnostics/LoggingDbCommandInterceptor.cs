using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using Serilog.Events;
using Serilog.Events;

namespace Shared.Logging.Diagnostics;

/// <summary>
/// EF Core command interceptor that emits structured logs for database command
/// failures and slow queries. Register on every DbContext via
/// <c>options.AddInterceptors(LoggingDbCommandInterceptor.Instance)</c>.
/// </summary>
public sealed class LoggingDbCommandInterceptor : DbCommandInterceptor
{
    /// <summary>Shared, stateless instance.</summary>
    public static readonly LoggingDbCommandInterceptor Instance = new();

    /// <summary>Queries slower than this are logged as warnings.</summary>
    public static TimeSpan SlowQueryThreshold { get; set; } = TimeSpan.FromMilliseconds(1000);

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        LogFailure(eventData);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        LogFailure(eventData);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        LogSlow(eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        LogSlow(eventData);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        LogSlow(eventData);
        return base.ScalarExecuted(command, eventData, result);
    }

    private static void LogFailure(CommandErrorEventData eventData)
    {
        Log
            .ForContext(LoggingConstants.Properties.Category, LoggingConstants.Categories.Database)
            .ForContext(LoggingConstants.Properties.EventCode, EventCode.DbCommandFailed)
            .ForContext(LoggingConstants.Properties.ExceptionType, eventData.Exception.GetType().Name)
            .ForContext(LoggingConstants.Properties.Elapsed, eventData.Duration.TotalMilliseconds)
            .Error(eventData.Exception, "Database command failed after {ElapsedMs} ms. {EventCode}",
                eventData.Duration.TotalMilliseconds, EventCode.DbCommandFailed);
    }

    private static void LogSlow(CommandExecutedEventData eventData)
    {
        if (eventData.Duration < SlowQueryThreshold)
        {
            return;
        }

        var commandText = eventData.Command.CommandText;
        var preview = commandText.Length > 240 ? commandText[..240] + "…" : commandText;

        Log
            .ForContext(LoggingConstants.Properties.Category, LoggingConstants.Categories.Database)
            .ForContext(LoggingConstants.Properties.EventCode, EventCode.SlowQuery)
            .ForContext(LoggingConstants.Properties.Elapsed, eventData.Duration.TotalMilliseconds)
            .Write(LogEventLevel.Warning, "Slow database query detected: {ElapsedMs} ms. {EventCode} SqlPreview={SqlPreview}",
                eventData.Duration.TotalMilliseconds, EventCode.SlowQuery, preview);
    }
}

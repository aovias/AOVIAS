using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ComplianceService.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Tracks per-request executed SQL command count for lightweight request-cost telemetry.
/// </summary>
public sealed class QueryCountingDbCommandInterceptor : DbCommandInterceptor
{
    private const string QueryCountKey = "perf.queryCount";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryCountingDbCommandInterceptor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private void IncrementQueryCount()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        var current = 0;
        if (context.Items.TryGetValue(QueryCountKey, out var value) && value is int boxed)
        {
            current = boxed;
        }

        context.Items[QueryCountKey] = current + 1;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        IncrementQueryCount();
        return base.ReaderExecuting(command, eventData, result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        IncrementQueryCount();
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        IncrementQueryCount();
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        IncrementQueryCount();
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        IncrementQueryCount();
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        IncrementQueryCount();
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }
}


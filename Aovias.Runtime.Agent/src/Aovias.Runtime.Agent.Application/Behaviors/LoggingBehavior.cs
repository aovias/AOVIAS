using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aovias.Runtime.Agent.Application;

/// <summary>
/// MediatR pipeline behavior that logs request execution.
/// </summary>
/// <typeparam name="TRequest">The request type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        this.logger = logger;
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next(cancellationToken);

        logger.LogInformation("Handled {RequestName}", requestName);
        return response;
    }
}

/// <summary>
/// Application layer dependency injection extensions.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application layer services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        return services;
    }
}

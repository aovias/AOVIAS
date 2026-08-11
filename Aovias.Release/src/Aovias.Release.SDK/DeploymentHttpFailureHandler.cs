using Aovias.Release.Contracts.Deployment;
using Microsoft.Extensions.Logging;

namespace Aovias.Release.SDK;

internal static class DeploymentHttpFailureHandler
{
    public static DeploymentFailureException CreateFailure(
        string operation,
        string endpoint,
        int statusCode,
        string responseBody,
        string phase,
        string component,
        string? serviceId,
        string? correlationId,
        ILogger? logger)
    {
        var failure = DeploymentFailureMapper.FromHttpResponse(
            statusCode,
            responseBody,
            phase,
            component,
            operation,
            serviceId,
            correlationId);

        logger?.LogError(
            "Deployment HTTP failure operation={Operation} endpoint={Endpoint} status={StatusCode} category={Category} message={Message}",
            operation,
            endpoint,
            statusCode,
            failure.Category,
            failure.Message);

        return new DeploymentFailureException(failure);
    }
}

using ComplianceServiceManager.Logging;
using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public sealed class PortRecoveryService
{
    private readonly ILoggingService logger;

    public PortRecoveryService(ILoggingService logger)
    {
        this.logger = logger;
    }

    public bool TryReassign(ServiceInfoModel service)
    {
        var updated = false;
        if (service.HttpPort is int httpPort)
        {
            var next = FindAvailablePort(httpPort + 1);
            if (next != httpPort)
            {
                service.HttpPort = next;
                service.HttpUrl = $"http://localhost:{next}";
                updated = true;
            }
        }

        if (service.HttpsPort is int httpsPort)
        {
            var next = FindAvailablePort(httpsPort + 1);
            if (next != httpsPort)
            {
                service.HttpsPort = next;
                service.HttpsUrl = $"https://localhost:{next}";
                service.LaunchUrl = service.HttpsUrl;
                updated = true;
            }
        }

        if (updated)
        {
            logger.Warning(service.Name, $"Auto-port reassignment completed. New URLs: {service.HttpsUrl ?? service.HttpUrl}");
        }

        return updated;
    }

    private static int FindAvailablePort(int start)
    {
        var port = Math.Max(start, 1025);
        for (var i = 0; i < 200; i++)
        {
            if (!System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties()
                    .GetActiveTcpListeners()
                    .Any(endpoint => endpoint.Port == port))
            {
                return port;
            }

            port++;
        }

        return start;
    }
}

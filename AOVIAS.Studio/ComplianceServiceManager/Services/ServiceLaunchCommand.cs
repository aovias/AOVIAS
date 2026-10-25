using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

/// <summary>
/// Builds the process command and environment for a service start.
/// Workspace Runtime sets <see cref="ServiceInfoModel.SuppressLaunchSettings"/> so
/// ASPNETCORE_URLS is authoritative. Shared Runtime keeps launch-profile semantics.
/// </summary>
internal static class ServiceLaunchCommand
{
    public static (string FileName, string Arguments) BuildStartCommand(ServiceInfoModel service)
    {
        if (service.Kind == ServiceKind.AngularWebApp)
        {
            var port = service.Port ?? service.HttpPort ?? 4200;
            return ("cmd.exe", $"/c ng serve --host 0.0.0.0 --port {port}");
        }

        return ("dotnet", BuildRunArguments(service));
    }

    public static string BuildRunArguments(ServiceInfoModel service)
    {
        var project = $"run --project \"{service.ProjectPath}\" --no-build";

        // Workspace Runtime: never let launchSettings.json override the registered port.
        if (service.SuppressLaunchSettings)
        {
            return $"{project} --no-launch-profile";
        }

        if (!string.IsNullOrWhiteSpace(service.LaunchProfileName))
        {
            return $"{project} --launch-profile \"{service.LaunchProfileName}\"";
        }

        return project;
    }

    public static Dictionary<string, string> BuildEnvironmentVariables(ServiceInfoModel service)
    {
        var variables = new Dictionary<string, string>(service.LaunchEnvironmentVariables, StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(service.EnvironmentName))
        {
            variables["ASPNETCORE_ENVIRONMENT"] = service.EnvironmentName;
        }

        if (string.IsNullOrWhiteSpace(service.LaunchProfileName) || service.SuppressLaunchSettings)
        {
            var urls = string.Join(';', new[] { service.HttpsUrl, service.HttpUrl }.Where(url => !string.IsNullOrWhiteSpace(url)));
            if (!string.IsNullOrWhiteSpace(urls))
            {
                variables["ASPNETCORE_URLS"] = urls;
            }
        }

        return variables;
    }
}

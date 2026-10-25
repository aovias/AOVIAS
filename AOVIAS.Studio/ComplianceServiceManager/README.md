# Compliance Service Manager

Lightweight WPF desktop utility for managing local CompliancePlatform WebAPI services during development.

## Run

```powershell
dotnet restore .\ComplianceServiceManager.csproj --configfile .\NuGet.Config
dotnet run --project .\ComplianceServiceManager.csproj
```

The app discovers WebAPI projects from the sibling `ComplaincePlatform` folder. It detects projects using `Microsoft.NET.Sdk.Web` or ASP.NET Core references, reads HTTPS profiles from `Properties\launchSettings.json`, builds before start, runs with `dotnet run --no-build --launch-profile "<profile>"`, captures build/runtime output, and polls `/health` then `/swagger` for service health.

Startup intentionally follows Visual Studio launch profile behavior. HTTPS profiles are preferred, profile environment variables are passed to the child process, and fallback startup clearly logs when `launchSettings.json` is unavailable.

## Structure

- `Models`: service, health, status, and log models.
- `ViewModels`: MVVM commands and filtering.
- `Services`: discovery, build, process management, service orchestration, and health checks.
- `Logging`: thread-safe UI log sink.
- `Helpers`: WPF converters and auto-scroll behavior.

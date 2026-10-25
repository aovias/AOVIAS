using Aovias.Configuration.Deployment;
using Aovias.Configuration.Extensions;
using Aovias.Configuration.Features;
using Aovias.Configuration.Loading;
using Aovias.Configuration.Settings;
using Aovias.Runtime.Agent.Api.Configuration;
using Aovias.Runtime.Agent.Api.Diagnostics;
using Aovias.Runtime.Agent.Api.Filters;
using Aovias.Runtime.Agent.Api.Middleware;
using Aovias.Runtime.Agent.Api.OpenApi;
using Aovias.Runtime.Agent.Application;
using Aovias.Runtime.Agent.Infrastructure;
using Aovias.Runtime.Agent.Infrastructure.Logging;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using Aovias.Configuration.SignalR;
using Microsoft.Extensions.Options;
using Serilog;

var bootstrapBuilder = WebApplication.CreateBuilder(args);
var bootstrapConfiguration = bootstrapBuilder.Configuration;
var bootstrapReport = new RuntimeBootstrapReport();

AoviasPlatformBootstrapDiagnostics.WriteBootstrapStart(Console.Out);

AoviasPlatformBootstrapOptions bootstrapOptions;

try
{
    bootstrapOptions = AoviasPlatformBootstrapResolver.Resolve(bootstrapConfiguration);
    AoviasPlatformBootstrapDiagnostics.WriteResolvedConfiguration(Console.Out, bootstrapOptions);
    AoviasPlatformBootstrapResolver.Validate(bootstrapOptions);
    bootstrapReport.RecordPass("Configuration Repository");
}
catch (AoviasConfigurationBootstrapException exception)
{
    bootstrapReport.RecordFailure("Configuration Repository", exception.Message);
    AoviasPlatformBootstrapDiagnostics.WriteFailure(Console.Out, exception);
    AoviasPlatformBootstrapDiagnostics.WriteReport(Console.Out, bootstrapReport);
    throw;
}

var hostEnvironmentName = bootstrapBuilder.Environment.EnvironmentName;
var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
var appSettingsEnvironmentPath = Path.Combine(AppContext.BaseDirectory, $"appsettings.{hostEnvironmentName}.json");

IConfiguration configuration;

try
{
    configuration = Aovias.Configuration.Extensions.ServiceCollectionExtensions.BuildAoviasConfiguration(
        bootstrapConfiguration,
        hostEnvironmentName,
        appSettingsPath,
        appSettingsEnvironmentPath);

    AoviasPlatformBootstrapDiagnostics.WriteSuccess(Console.Out, bootstrapOptions);
    bootstrapReport.RecordPass("Profile Loading");
}
catch (AoviasConfigurationBootstrapException exception)
{
    bootstrapReport.RecordFailure("Profile Loading", exception.Message);
    AoviasPlatformBootstrapDiagnostics.WriteFailure(Console.Out, exception);
    AoviasPlatformBootstrapDiagnostics.WriteReport(Console.Out, bootstrapReport);
    throw;
}

try
{
    Log.Logger = SerilogBootstrapper.CreateLogger(
        LoggingSettingsBinder.BindAndPrepare(configuration, bootstrapOptions));
    bootstrapReport.RecordPass("Logging");
}
catch (AoviasConfigurationBootstrapException exception)
{
    bootstrapReport.RecordFailure("Logging", exception.Message);
    AoviasPlatformBootstrapDiagnostics.WriteFailure(Console.Out, exception);
    AoviasPlatformBootstrapDiagnostics.WriteReport(Console.Out, bootstrapReport);
    throw;
}

bootstrapBuilder.Host.UseSerilog();

var apiSettings = configuration.GetSection(ApiSettings.SectionName).Get<ApiSettings>();
if (!string.IsNullOrWhiteSpace(apiSettings?.InternalUrl))
{
    bootstrapBuilder.WebHost.UseUrls(apiSettings.InternalUrl);
}

bootstrapBuilder.Services.AddSingleton<IConfiguration>(configuration);
bootstrapBuilder.Services.AddApplication();
bootstrapBuilder.Services.AddInfrastructure(configuration);
bootstrapBuilder.Services.AddRuntimeAgentAuthenticationArchitecture();
bootstrapBuilder.Services.AddRuntimeAgentOpenApi();
bootstrapBuilder.Services.AddControllers(options =>
{
    options.Filters.Add<DeploymentBoundaryDiagnosticsFilter>();
});
bootstrapBuilder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        if (!DeploymentBoundaryDiagnostics.IsDeploymentBoundaryPath(context.HttpContext.Request.Path))
        {
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(context.ModelState);
        }

        var endpoint = context.HttpContext.Request.Path.Value ?? string.Empty;
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("DeploymentBoundaryModelBinding");

        DeploymentBoundaryDiagnostics.LogModelState(logger, "ModelBinding", context.ModelState);

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(
            DeploymentBoundaryDiagnostics.FromModelState(
                context.ModelState,
                endpoint,
                expectedRequestType: "deployment request DTO"));
    };
});
bootstrapBuilder.Services.AddSignalR(options =>
{
    var signalRSettings = configuration.GetSection(SignalRSettings.SectionName).Get<SignalRSettings>();
    if (signalRSettings is not null)
    {
        options.KeepAliveInterval = TimeSpan.FromSeconds(signalRSettings.KeepAliveSeconds);
        options.ClientTimeoutInterval = TimeSpan.FromSeconds(signalRSettings.ClientTimeoutSeconds);
    }
});

WebApplication app;

try
{
    app = bootstrapBuilder.Build();

    var startupFeatures = app.Services.GetRequiredService<PlatformFeatureActivation>();
    if (startupFeatures.ReleaseManagement)
    {
        var pathResolver = app.Services.GetRequiredService<IDeploymentPathResolver>();
        var releaseSettings = app.Services.GetRequiredService<IOptionsMonitor<ReleaseManagementSettings>>().CurrentValue;
        DeploymentExecutionDiagnostics.WriteAssemblyIdentity(Console.Out);
        DeploymentExecutionDiagnostics.LogConfigurationSnapshot(releaseSettings, pathResolver, "Startup");
        var storageValidation = pathResolver.ValidateStorageLayout(createMissing: true);
        DeploymentStorageStartupDiagnostics.WriteReport(Console.Out, storageValidation);
        bootstrapReport.RecordPass("Deployment Storage");
        if (!storageValidation.IsValid)
        {
            bootstrapReport.RecordFailure("Deployment Storage", storageValidation.FailureMessage ?? "Validation failed.");
            throw new InvalidOperationException(storageValidation.FailureMessage ?? "Deployment storage validation failed.");
        }
    }
}
catch (Exception exception)
{
    WriteStartupFailure(configuration, bootstrapReport, exception);
    throw;
}

var configuredPathBase = apiSettings?.PathBase;
if (!string.IsNullOrWhiteSpace(configuredPathBase))
{
    app.UsePathBase(configuredPathBase);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseRuntimeAgentOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var signalRConfiguration = app.Services.GetRequiredService<IConfiguration>().GetSection(SignalRSettings.SectionName).Get<SignalRSettings>();
if (signalRConfiguration?.Enabled == true)
{
    foreach (var hubPath in SignalRHubEndpointResolver.GetHubRegistrationPaths(signalRConfiguration.HubPath))
    {
        app.MapHub<RuntimeHub>(hubPath);
    }
}

try
{
    Log.Information(
        "Starting AOVIAS Runtime Agent for profile {Profile} using config root {ConfigRoot}",
        bootstrapOptions.ActiveProfile,
        bootstrapOptions.ConfigRoot);

    await app.StartAsync();

    var features = app.Services.GetRequiredService<PlatformFeatureActivation>();
    AoviasPlatformBootstrapDiagnostics.PopulateModuleStatuses(bootstrapReport, features);
    bootstrapReport.MarkStartupSucceeded();
    AoviasPlatformBootstrapDiagnostics.WriteReport(Console.Out, bootstrapReport);

    await app.WaitForShutdownAsync();
}
catch (Exception exception) when (TryGetOptionsValidationException(exception, out var validationException))
{
    WriteStartupFailure(configuration, bootstrapReport, validationException);
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void WriteStartupFailure(
    IConfiguration configuration,
    RuntimeBootstrapReport bootstrapReport,
    Exception exception)
{
    var features = PlatformFeatureActivation.ResolveRuntimeAgent(configuration);
    AoviasPlatformBootstrapDiagnostics.PopulateModuleStatuses(bootstrapReport, features);
    bootstrapReport.RecordFailure("Startup", exception.Message);
    bootstrapReport.MarkStartupFailed();
    AoviasPlatformBootstrapDiagnostics.WriteReport(Console.Out, bootstrapReport);
}

static bool TryGetOptionsValidationException(Exception exception, out OptionsValidationException validationException)
{
    if (exception is OptionsValidationException direct)
    {
        validationException = direct;
        return true;
    }

    if (exception is AggregateException aggregate)
    {
        validationException = aggregate.InnerExceptions
            .OfType<OptionsValidationException>()
            .FirstOrDefault()!;

        if (validationException is not null)
        {
            return true;
        }
    }

    if (exception.InnerException is OptionsValidationException inner)
    {
        validationException = inner;
        return true;
    }

    validationException = null!;
    return false;
}

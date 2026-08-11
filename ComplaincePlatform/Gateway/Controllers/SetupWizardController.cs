using FluentValidation;
using Gateway.DTOs.Setup;
using Gateway.Services.Installation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Shared.Infrastructure.Api;

namespace Gateway.Controllers;

/// <summary>
/// First-run installation wizard APIs. Mutating endpoints are disabled once
/// <c>App_Data/installation.lock</c> (or Installation:Completed) is present.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/setup")]
[AllowAnonymous]
public sealed class SetupWizardController : ControllerBase
{
    private readonly IInstallationStateService _installation;
    private readonly IValidator<SetupConfigureRequest> _configureValidator;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SetupWizardController> _logger;

    public SetupWizardController(
        IInstallationStateService installation,
        IValidator<SetupConfigureRequest> configureValidator,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<SetupWizardController> logger)
    {
        _installation = installation;
        _configureValidator = configureValidator;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(Shared.Infrastructure.DTO.ApiResponse<SetupStatusDto>), StatusCodes.Status200OK)]
    public ActionResult<Shared.Infrastructure.DTO.ApiResponse<SetupStatusDto>> GetStatus()
    {
        var traceId = HttpContext.TraceIdentifier;
        var status = _installation.GetStatus();
        var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, status, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
        return StatusCode(meta.HttpStatus, envelope);
    }

    [HttpPost("database/test")]
    [ProducesResponseType(typeof(Shared.Infrastructure.DTO.ApiResponse<DatabaseConnectionTestResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestDatabase(
        [FromBody] DatabaseConnectionTestRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (TryLockedConflict(traceId, out var locked))
            return locked!;

        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            var failure = ApiResponseFactory.Failure<DatabaseConnectionTestResultDto>(ApiResponseCode.VALIDATION_FAILED, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }

        try
        {
            await using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            var major = connection.ServerVersion?.Split('.').FirstOrDefault();
            var result = new DatabaseConnectionTestResultDto
            {
                Success = true,
                Message = "SQL connection succeeded.",
                ServerVersionMajor = int.TryParse(major, out var m) ? m : null
            };
            var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, envelope);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Setup database connection test failed");
            var result = new DatabaseConnectionTestResultDto
            {
                Success = false,
                Message = "SQL connection failed: " + ex.Message
            };
            var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, envelope);
        }
    }

    [HttpPost("configure")]
    [ProducesResponseType(typeof(Shared.Infrastructure.DTO.ApiResponse<SetupConfigureResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Configure(
        [FromBody] SetupConfigureRequest request,
        CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (TryLockedConflict(traceId, out var locked))
            return locked!;

        var validation = await _configureValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .Select(e => new Shared.Infrastructure.DTO.ApiError { Field = e.PropertyName, Message = e.ErrorMessage })
                .ToList();
            var failure = ApiResponseFactory.Failure<SetupConfigureResultDto>(ApiResponseCode.VALIDATION_FAILED, errors, traceId);
            return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED].HttpStatus, failure);
        }

        var saved = await _installation.SaveConfigurationAsync(request, cancellationToken);
        var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, saved, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, envelope);
    }

    [HttpPost("complete")]
    [ProducesResponseType(typeof(Shared.Infrastructure.DTO.ApiResponse<SetupCompleteResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(CancellationToken cancellationToken)
    {
        var traceId = HttpContext.TraceIdentifier;
        if (TryLockedConflict(traceId, out var locked))
            return locked!;

        var pings = await RunHealthPingsAsync(cancellationToken);
        var completed = await _installation.CompleteAsync(pings, cancellationToken);
        var envelope = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, completed, traceId);
        return StatusCode(ApiResponseCodes.Registry[ApiResponseCode.SUCCESS].HttpStatus, envelope);
    }

    private bool TryLockedConflict(string traceId, out IActionResult? result)
    {
        result = null;
        if (!_installation.IsCompleted())
            return false;

        var failure = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_CONFLICT, traceId);
        result = StatusCode(ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_CONFLICT].HttpStatus, failure);
        return true;
    }

    private async Task<IReadOnlyList<HealthPingResultDto>> RunHealthPingsAsync(CancellationToken cancellationToken)
    {
        var services = _configuration.GetSection("Services").GetChildren()
            .Where(s => !string.Equals(s.Key, "GlobalProxy", StringComparison.OrdinalIgnoreCase))
            .Select(s => (Name: s.Key, BaseUrl: s["BaseUrl"]))
            .Where(s => !string.IsNullOrWhiteSpace(s.BaseUrl))
            .Take(8)
            .ToList();

        var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        var results = new List<HealthPingResultDto>();

        foreach (var (name, baseUrl) in services)
        {
            var url = CombineHealthUrl(baseUrl!);
            try
            {
                using var response = await client.GetAsync(url, cancellationToken);
                results.Add(new HealthPingResultDto
                {
                    Service = name,
                    Url = url,
                    Success = response.IsSuccessStatusCode,
                    StatusCode = (int)response.StatusCode,
                    Message = response.IsSuccessStatusCode ? "OK" : response.ReasonPhrase ?? "Failed"
                });
            }
            catch (Exception ex)
            {
                results.Add(new HealthPingResultDto
                {
                    Service = name,
                    Url = url,
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        // Always include Gateway self health if mapped.
        results.Insert(0, new HealthPingResultDto
        {
            Service = "Gateway",
            Url = "/health",
            Success = true,
            StatusCode = 200,
            Message = "Setup complete marker written; Gateway process is running."
        });

        return results;
    }

    private static string CombineHealthUrl(string baseUrl)
    {
        var trimmed = baseUrl.TrimEnd('/');
        return trimmed + "/health";
    }
}

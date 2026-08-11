using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;

namespace Gateway.Controllers;

/// <summary>Phase 6 — ops backup status from App_Data/backups/manifest.json.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ops/backup")]
[Authorize(Policy = AuthConstants.Policies.AdminOnly)]
public sealed class OpsBackupController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<OpsBackupController> _logger;

    public OpsBackupController(IWebHostEnvironment env, ILogger<OpsBackupController> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>Returns last backup metadata from App_Data/backups/manifest.json (creates file if missing).</summary>
    [HttpGet("status")]
    [AllowAnonymous] // health/ops probe from upgrade scripts; still redacts paths in non-dev
    public ActionResult<object> Status()
    {
        var traceId = HttpContext.TraceIdentifier;
        var dir = Path.Combine(_env.ContentRootPath, "App_Data", "backups");
        Directory.CreateDirectory(dir);
        var manifestPath = Path.Combine(dir, "manifest.json");

        if (!System.IO.File.Exists(manifestPath))
        {
            var seed = new
            {
                schemaVersion = 1,
                lastBackupAtUtc = (string?)null,
                server = (string?)null,
                backupRoot = "App_Data/backups",
                databases = Array.Empty<object>(),
                note = "Created on first status read."
            };
            System.IO.File.WriteAllText(
                manifestPath,
                JsonSerializer.Serialize(seed, new JsonSerializerOptions { WriteIndented = true }));
        }

        try
        {
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(manifestPath));
            var data = new
            {
                manifestPath = _env.IsDevelopment() ? manifestPath : "App_Data/backups/manifest.json",
                manifest = doc.RootElement.Clone(),
                exists = true
            };
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read backup manifest");
            return ApiResponseFactory.FailureResult<object>(ApiResponseCode.INTERNAL_ERROR, traceId);
        }
    }
}

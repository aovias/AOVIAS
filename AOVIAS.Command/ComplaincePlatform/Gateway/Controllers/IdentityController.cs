using Gateway.Application.Commands;
using Gateway.Application.Queries;
using Gateway.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/identity")]
public class IdentityController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityController> _logger;

    public IdentityController(IMediator mediator, ILogger<IdentityController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public Task<IActionResult> Login([FromBody] object requestBody, CancellationToken cancellationToken)
        => _mediator.Send(new LoginQuery(requestBody), cancellationToken);

    [HttpPost("login/mfa")]
    [AllowAnonymous]
    public Task<IActionResult> LoginMfa([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/login/mfa", requestBody, cancellationToken);

    [HttpPost("refresh")]
    [AllowAnonymous]
    public Task<IActionResult> Refresh([FromBody] object requestBody, CancellationToken cancellationToken)
        => _mediator.Send(new RefreshTokenQuery(requestBody), cancellationToken);

    [HttpPost("logout")]
    [Authorize]
    public Task<IActionResult> Logout([FromBody] object? requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/logout", requestBody, cancellationToken);

    [HttpPost("logout-all")]
    [Authorize]
    public Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/logout-all", new { }, cancellationToken);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Signup([FromBody] SignupRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var command = new SignupCommand(request);
            var response = await _mediator.Send(command, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Signup failed: {Message}", ex.Message);
            if (ex.Message.Contains("already exists"))
                return Conflict(new { error = ex.Message });
            if (ex.Message.Contains("Email verification is required", StringComparison.OrdinalIgnoreCase))
                return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during signup");
            return StatusCode(500, new { error = "An unexpected error occurred during signup. Please try again later." });
        }
    }

    /// <summary>Identity-only self registration (no organization). Proxies IdentityService auth/register.</summary>
    [HttpPost("self-register")]
    [AllowAnonymous]
    public Task<IActionResult> SelfRegister([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/register", requestBody, cancellationToken);

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    public Task<IActionResult> ResendVerification([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/resend-verification", requestBody, cancellationToken);

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public Task<IActionResult> VerifyEmail([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/verify-email", requestBody, cancellationToken);

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public Task<IActionResult> ForgotPassword([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/forgot-password", requestBody, cancellationToken);

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public Task<IActionResult> ResetPassword([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/reset-password", requestBody, cancellationToken);

    [HttpPut("change-password")]
    [Authorize]
    public Task<IActionResult> ChangePassword([FromBody] object requestBody, CancellationToken cancellationToken)
        => Proxy(HttpMethod.Put, "/api/v1/auth/change-password", requestBody, cancellationToken);

    [HttpPost("mfa/setup")]
    [Authorize]
    public Task<IActionResult> MfaSetup(CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/mfa/setup", new { }, cancellationToken);

    [HttpPost("mfa/verify")]
    [Authorize]
    public Task<IActionResult> MfaVerify([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/mfa/verify", requestBody, cancellationToken);

    [HttpPost("mfa/disable")]
    [Authorize]
    public Task<IActionResult> MfaDisable([FromBody] object requestBody, CancellationToken cancellationToken)
        => ProxyPost("/api/v1/auth/mfa/disable", requestBody, cancellationToken);

    [HttpGet("mfa/recovery-codes")]
    [Authorize]
    public Task<IActionResult> MfaRecoveryCodes(CancellationToken cancellationToken)
        => Proxy(HttpMethod.Get, "/api/v1/auth/mfa/recovery-codes", null, cancellationToken);

    [HttpGet("sessions")]
    [Authorize]
    public Task<IActionResult> ListSessions(CancellationToken cancellationToken)
        => Proxy(HttpMethod.Get, "/api/v1/auth/sessions", null, cancellationToken);

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
        => Proxy(HttpMethod.Delete, $"/api/v1/auth/sessions/{sessionId}", null, cancellationToken);

    private Task<IActionResult> ProxyPost(string endpoint, object? body, CancellationToken cancellationToken)
        => Proxy(HttpMethod.Post, endpoint, body, cancellationToken);

    private Task<IActionResult> Proxy(HttpMethod method, string endpoint, object? body, CancellationToken cancellationToken)
        => _mediator.Send(new IdentityProxyQuery(endpoint, method, body), cancellationToken);
}

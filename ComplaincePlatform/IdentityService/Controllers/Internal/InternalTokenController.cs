using IdentityService.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;

namespace IdentityService.Controllers.Internal;

[ApiController]
[Route("internal")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalTokenController : ControllerBase
{
    private readonly ITokenValidationService _tokenValidation;

    public InternalTokenController(ITokenValidationService tokenValidation)
    {
        _tokenValidation = tokenValidation;
    }

    [HttpPost("token/validate")]
    public async Task<IActionResult> ValidateToken(
        [FromBody] ValidateTokenRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty || string.IsNullOrWhiteSpace(request.Jti))
            return BadRequest(new { valid = false, error = "UserId and Jti are required." });

        var valid = await _tokenValidation.ValidateAsync(
            request.UserId,
            request.Jti,
            request.TokenVersion,
            request.SecurityStamp ?? string.Empty,
            cancellationToken);

        return Ok(new { valid });
    }
}

public class ValidateTokenRequest
{
    public Guid UserId { get; set; }
    public string Jti { get; set; } = string.Empty;
    public int TokenVersion { get; set; }
    public string? SecurityStamp { get; set; }
}

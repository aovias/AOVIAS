using IdentityService.Application.Contracts;
using IdentityService.Application.DTOs;
using IdentityService.Application.Services;
using IdentityService.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Auth;
using System.Net;

namespace IdentityService.Controllers.Internal;

[ApiController]
[Route("internal")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class InternalUserController : ControllerBase
{
    private readonly IRegistrationService _registrationService;
    private readonly ISessionService _sessionService;
    private readonly ITokenService _tokenService;
    private readonly IUserAdminAppService _userAdmin;
    private readonly UserManager<Domain.AppUser> _userManager;
    private readonly ILogger<InternalUserController> _logger;

    public InternalUserController(
        IRegistrationService registrationService,
        ISessionService sessionService,
        ITokenService tokenService,
        IUserAdminAppService userAdmin,
        UserManager<Domain.AppUser> userManager,
        ILogger<InternalUserController> logger)
    {
        _registrationService = registrationService;
        _sessionService = sessionService;
        _tokenService = tokenService;
        _userAdmin = userAdmin;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpPost("users/register")]
    public async Task<IActionResult> RegisterUser([FromBody] InternalRegisterUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var (result, status) = await _registrationService.RegisterInternalAsync(
                request.Email, request.Password, request.Role, emailConfirmed: true, HttpContext, cancellationToken);

            if (!status.Success)
            {
                var code = status.ErrorCode == "RESOURCE_CONFLICT" ? HttpStatusCode.Conflict : HttpStatusCode.BadRequest;
                return StatusCode((int)code, new { error = status.Message, details = status.Details });
            }

            return Ok(new { userId = result!.UserId, email = result.Email, platformId = result.PlatformId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during internal user registration for {Email}", request?.Email);
            return StatusCode((int)HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred during user registration." });
        }
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var (success, error) = await _userAdmin.SoftDeleteAsync(id, Guid.Empty);
        if (!success)
            return NotFound(new { error = error ?? "User not found" });
        return NoContent();
    }

    [HttpPost("token/generate")]
    public async Task<IActionResult> GenerateToken([FromBody] GenerateTokenRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = "UserId and Email are required" });

        if (!Guid.TryParse(request.UserId, out var userId))
            return BadRequest(new { error = "Invalid UserId" });

        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
            return NotFound(new { error = "User not found" });

        var (_, refreshToken) = await _sessionService.CreateSessionAsync(userId, null, null, null, cancellationToken);
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        if (roles.Count == 0 && !string.IsNullOrWhiteSpace(request.Role))
            roles.Add(RoleLegacyMap.ToCanonical(request.Role));

        var authContext = new UserAuthContextDto
        {
            OrganizationId = request.OrganizationId,
            HospitalId = request.HospitalId,
            MembershipId = Guid.Empty,
            DepartmentId = null,
            Role = roles.FirstOrDefault() ?? ApplicationRoles.USER
        };

        var stamp = await _userManager.GetSecurityStampAsync(user) ?? string.Empty;
        var session = await _sessionService.GetSessionAsync(
            (await _sessionService.ListSessionsAsync(userId, null, cancellationToken)).First().SessionId,
            cancellationToken);

        var tokenPair = await _tokenService.GenerateTokenPairAsync(new TokenClaimsContext
        {
            UserId = userId,
            Email = request.Email,
            PlatformId = user.PlatformId,
            PersonId = user.PersonId,
            UserName = user.UserName ?? request.Email,
            Roles = roles,
            AuthContext = authContext,
            SessionId = session!.SessionId,
            TokenVersion = user.TokenVersion,
            SecurityStamp = stamp,
            Scopes = new List<string>
            {
                AuthConstants.Scopes.ComplianceApi,
                AuthConstants.Scopes.OrganizationApi,
                AuthConstants.Scopes.AiApi
            }
        }, refreshToken, cancellationToken);

        return Ok(new { token = tokenPair.AccessToken, refreshToken = tokenPair.RefreshToken, expiresIn = tokenPair.ExpiresIn });
    }

    [HttpPost("users/{id}/suspend")]
    public async Task<IActionResult> SuspendUser(Guid id, CancellationToken cancellationToken)
    {
        var (success, error) = await _userAdmin.SuspendAsync(id, Guid.Empty, cancellationToken);
        if (!success) return NotFound(new { error });
        return Ok(new { message = "User suspended" });
    }

    [HttpPost("users/{id}/force-password-change")]
    public async Task<IActionResult> ForcePasswordChange(Guid id, CancellationToken cancellationToken)
    {
        var (success, error) = await _userAdmin.ForcePasswordChangeAsync(id, Guid.Empty, cancellationToken);
        if (!success) return NotFound(new { error });
        return Ok(new { message = "Password change required on next login" });
    }

    [HttpPost("users/{id}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        var (success, error) = await _userAdmin.ActivateAsync(id, Guid.Empty);
        if (!success) return NotFound(new { error });
        return Ok(new { message = "User activated" });
    }

    [HttpPost("users/{id}/unlock")]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        var (success, error) = await _userAdmin.UnlockAsync(id, Guid.Empty, cancellationToken);
        if (!success) return NotFound(new { error });
        return Ok(new { message = "User unlocked" });
    }

    [HttpPost("users/{id}/disable")]
    public async Task<IActionResult> DisableUser(Guid id, CancellationToken cancellationToken)
    {
        var (success, error) = await _userAdmin.DisableAsync(id, Guid.Empty, cancellationToken);
        if (!success) return NotFound(new { error });
        return Ok(new { message = "User disabled" });
    }

    [HttpPost("users/{id}/terminate-sessions")]
    public async Task<IActionResult> TerminateSessions(Guid id, CancellationToken cancellationToken)
    {
        await _sessionService.RevokeAllSessionsAsync(id, cancellationToken);
        return Ok(new { message = "Sessions terminated" });
    }

    [HttpGet("users/{id:guid}/contact")]
    public async Task<IActionResult> GetUserContact(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        return Ok(new { userId = user.Id, personId = user.PersonId, email = user.Email, status = user.Status.ToString(), emailConfirmed = user.EmailConfirmed, lockoutEnd = user.LockoutEnd });
    }

    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var users = _userManager.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            users = users.Where(u =>
                (u.Email != null && u.Email.Contains(term)) ||
                (u.UserName != null && u.UserName.Contains(term)) ||
                (u.PlatformId != null && u.PlatformId.Contains(term)));
        }

        var total = users.Count();
        var items = users.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                userId = u.Id,
                email = u.Email,
                userName = u.UserName,
                platformId = u.PlatformId,
                status = u.Status.ToString(),
                emailConfirmed = u.EmailConfirmed,
                lockoutEnd = u.LockoutEnd,
                lastLoginAt = u.LastLoginAt
            })
            .ToList();
        return Ok(new { items, total, page, pageSize });
    }
}

public class InternalRegisterUserRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class GenerateTokenRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
}

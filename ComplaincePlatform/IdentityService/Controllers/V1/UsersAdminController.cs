using IdentityService.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.DTO;

namespace IdentityService.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = InternalApiAuthorizationExtensions.InternalOnlyPolicy)]
public class UsersAdminController(IMediator mediator) : ControllerBase
{
    private string TraceId() => HttpContext.TraceIdentifier;

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserAdminDetailDto>>> Get(Guid id, CancellationToken ct)
    {
        var user = await mediator.Send(new GetUserAdminByIdQuery(id), ct);
        if (user == null)
            return NotFound(ApiResponseFactory.Failure<UserAdminDetailDto>(ApiResponseCode.RESOURCE_NOT_FOUND, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, user, TraceId()));
    }

    [HttpPost("batch")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserAdminDetailDto>>>> Batch([FromBody] BatchGetUsersRequest request, CancellationToken ct)
    {
        var users = await mediator.Send(new BatchGetUsersAdminQuery(request.UserIds), ct);
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, users, TraceId()));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserAdminDetailDto>>> Create([FromBody] CreateUserAdminInternalRequest request, CancellationToken ct)
    {
        var (user, error) = await mediator.Send(new CreateUserAdminCommand(request.ToRequest(), request.ActorUserId), ct);
        if (user == null)
            return BadRequest(ApiResponseFactory.Failure<UserAdminDetailDto>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Create failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, user, TraceId()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserAdminDetailDto>>> Update(Guid id, [FromBody] UpdateUserAdminInternalRequest request, CancellationToken ct)
    {
        var (user, error) = await mediator.Send(new UpdateUserAdminCommand(id, request.ToRequest(), request.ActorUserId), ct);
        if (user == null)
            return BadRequest(ApiResponseFactory.Failure<UserAdminDetailDto>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Update failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, user, TraceId()));
    }

    [HttpPost("{id:guid}/suspend")]
    public async Task<IActionResult> Suspend(Guid id, [FromBody] UserAdminActionRequest request, CancellationToken ct)
    {
        var (success, error) = await mediator.Send(new SuspendUserAdminCommand(id, request.ActorUserId), ct);
        if (!success) return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Suspend failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, TraceId()));
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, [FromBody] UserAdminActionRequest request, CancellationToken ct)
    {
        var (success, error) = await mediator.Send(new ActivateUserAdminCommand(id, request.ActorUserId), ct);
        if (!success) return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Activate failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, TraceId()));
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, [FromBody] UserAdminActionRequest request, CancellationToken ct)
    {
        var (success, error) = await mediator.Send(new DisableUserAdminCommand(id, request.ActorUserId), ct);
        if (!success) return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Disable failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, TraceId()));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid actorUserId, CancellationToken ct)
    {
        var (success, error) = await mediator.Send(new SoftDeleteUserAdminCommand(id, actorUserId), ct);
        if (!success) return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Delete failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, TraceId()));
    }

    [HttpPost("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(Guid id, [FromBody] UserAdminActionRequest request, CancellationToken ct)
    {
        var (success, error) = await mediator.Send(new UnlockUserAdminCommand(id, request.ActorUserId), ct);
        if (!success) return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Unlock failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, TraceId()));
    }

    [HttpPost("{id:guid}/force-password-change")]
    public async Task<IActionResult> ForcePasswordChange(Guid id, [FromBody] UserAdminActionRequest request, CancellationToken ct)
    {
        var (success, error) = await mediator.Send(new ForcePasswordChangeAdminCommand(id, request.ActorUserId), ct);
        if (!success) return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, new List<ApiError> { new() { Message = error ?? "Force password change failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, TraceId()));
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordInternalRequest request, CancellationToken ct)
    {
        var (success, message, errors) = await mediator.Send(new AdminResetPasswordCommand(id, request.NewPassword, request.ActorUserId), ct);
        if (!success)
            return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, errors?.Select(e => new ApiError { Message = e }).ToList() ?? new List<ApiError> { new() { Message = message ?? "Reset failed." } }, TraceId()));
        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { message }, TraceId()));
    }
}

public sealed class UserAdminActionRequest
{
    public Guid ActorUserId { get; set; }
}

public sealed class CreateUserAdminInternalRequest
{
    public Guid ActorUserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public Guid? PersonId { get; set; }

    public CreateUserAdminRequest ToRequest() => new()
    {
        Email = Email,
        UserName = UserName,
        TemporaryPassword = TemporaryPassword,
        PhoneNumber = PhoneNumber,
        Role = Role,
        PersonId = PersonId
    };
}

public sealed class UpdateUserAdminInternalRequest
{
    public Guid ActorUserId { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }

    public UpdateUserAdminRequest ToRequest() => new()
    {
        Email = Email,
        UserName = UserName,
        PhoneNumber = PhoneNumber,
        Role = Role,
        Status = Status
    };
}

public sealed class AdminResetPasswordInternalRequest
{
    public Guid ActorUserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

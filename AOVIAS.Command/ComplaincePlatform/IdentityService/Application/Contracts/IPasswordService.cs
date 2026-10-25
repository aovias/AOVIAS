namespace IdentityService.Application.Contracts;

public interface IPasswordService
{
    Task<(bool success, string? message)> ForgotPasswordAsync(string email, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool success, string? message, List<string>? errors)> ResetPasswordAsync(string token, string newPassword, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool success, string? message, List<string>? errors)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task<(bool success, string? message, List<string>? errors)> AdminResetPasswordAsync(Guid userId, string newPassword, Guid actorUserId, HttpContext? httpContext, CancellationToken cancellationToken = default);
}

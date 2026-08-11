namespace Shared.Infrastructure.Auth;

/// <summary>
/// Standardized error codes for authentication operations
/// </summary>
public static class AuthErrorCodes
{
    // Validation errors (400)
    public const string ValidationError = "AUTH_VALIDATION_ERROR";
    public const string MissingCredentials = "AUTH_MISSING_CREDENTIALS";
    public const string InvalidEmailFormat = "AUTH_INVALID_EMAIL_FORMAT";
    public const string WeakPassword = "AUTH_WEAK_PASSWORD";

    // Authentication errors (401)
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    public const string UserNotFound = "AUTH_USER_NOT_FOUND";
    public const string InvalidToken = "AUTH_INVALID_TOKEN";
    public const string ExpiredToken = "AUTH_EXPIRED_TOKEN";
    public const string InvalidRefreshToken = "AUTH_INVALID_REFRESH_TOKEN";
    public const string TokenRevoked = "AUTH_TOKEN_REVOKED";

    // Authorization errors (403)
    public const string InsufficientPermissions = "AUTH_INSUFFICIENT_PERMISSIONS";
    public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
    public const string AccountDisabled = "AUTH_ACCOUNT_DISABLED";

    // Conflict errors (409)
    public const string UserAlreadyExists = "AUTH_USER_ALREADY_EXISTS";
    public const string EmailAlreadyRegistered = "AUTH_EMAIL_ALREADY_REGISTERED";

    // Rate limiting (429)
    public const string RateLimitExceeded = "AUTH_RATE_LIMIT_EXCEEDED";
    public const string TooManyLoginAttempts = "AUTH_TOO_MANY_LOGIN_ATTEMPTS";

    // Server errors (500)
    public const string InternalServerError = "AUTH_INTERNAL_SERVER_ERROR";
    public const string TokenGenerationFailed = "AUTH_TOKEN_GENERATION_FAILED";
    public const string DatabaseError = "AUTH_DATABASE_ERROR";
    public const string ServiceUnavailable = "AUTH_SERVICE_UNAVAILABLE";
}

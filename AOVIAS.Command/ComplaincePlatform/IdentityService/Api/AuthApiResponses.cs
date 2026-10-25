using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace IdentityService.Api;

internal static class AuthApiResponses
{
    public static ObjectResult Failure<T>(
        ApiResponseCode code,
        string message,
        string? requestId = null,
        List<string>? detailMessages = null)
    {
        var errors = detailMessages is { Count: > 0 }
            ? detailMessages.Select(m => new ApiError { Message = m }).ToList()
            : new List<ApiError> { new() { Message = message } };

        var envelope = ApiResponseFactory.Failure<T>(code, errors, requestId);
        var httpStatus = ApiResponseCodes.Registry[code].HttpStatus;

        return new ObjectResult(envelope) { StatusCode = httpStatus };
    }

    public static ApiResponse<T> Success<T>(T data, string? requestId = null)
        => ApiResponseFactory.Success(ApiResponseCode.SUCCESS, data, requestId);
}

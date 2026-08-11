using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.DTO;

namespace Shared.Infrastructure.Api;

/// <summary>
/// Factory helpers for building standardized ApiResponse envelopes.
/// </summary>
public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(ApiResponseCode code, T data, string? requestId = null)
    {
        var meta = ApiResponseCodes.Registry[code];

        return new ApiResponse<T>
        {
            Success = true,
            Code = code.ToString(),
            Message = meta.Message,
            Data = data,
            Errors = new List<ApiError>(),
            Meta = new ApiMeta
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            }
        };
    }

    public static ApiResponse<T> Failure<T>(ApiResponseCode code, List<ApiError> errors, string? requestId = null)
    {
        var meta = ApiResponseCodes.Registry[code];

        return new ApiResponse<T>
        {
            Success = false,
            Code = code.ToString(),
            Message = meta.Message,
            Data = default,
            Errors = errors,
            Meta = new ApiMeta
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow
            }
        };
    }

    /// <summary>
    /// Convenience overload that creates a default error list from ApiResponseCodes metadata.
    /// </summary>
    public static ApiResponse<T> Failure<T>(ApiResponseCode code, string? requestId = null)
    {
        var meta = ApiResponseCodes.Registry[code];

        var errors = new List<ApiError>
        {
            new ApiError
            {
                Message = meta.Message
            }
        };

        return Failure<T>(code, errors, requestId);
    }

    /// <summary>
    /// Convenience helper that returns an ObjectResult with the correct HTTP status code
    /// and a standardized ApiResponse envelope.
    /// </summary>
    public static ObjectResult FailureResult<T>(ApiResponseCode code, string? requestId = null)
    {
        var meta = ApiResponseCodes.Registry[code];
        var envelope = Failure<T>(code, requestId);

        return new ObjectResult(envelope)
        {
            StatusCode = meta.HttpStatus
        };
    }

    /// <summary>
    /// Returns a failure <see cref="ObjectResult"/> with field-level validation errors (same envelope as <see cref="Failure{T}(ApiResponseCode, List{ApiError}, string?)"/>).
    /// </summary>
    public static ObjectResult FailureResult<T>(ApiResponseCode code, List<ApiError> errors, string? requestId = null)
    {
        var meta = ApiResponseCodes.Registry[code];
        var envelope = Failure<T>(code, errors, requestId);

        return new ObjectResult(envelope)
        {
            StatusCode = meta.HttpStatus
        };
    }
}


namespace Shared.Infrastructure.DTO;

/// <summary>
/// Standardized API response wrapper for all API responses.
/// Enforces consistent response format across all microservices.
/// Target envelope: success, code, message, data, errors, meta.
/// </summary>
/// <typeparam name="T">The type of data being returned (null for error responses)</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// true only when HTTP status is 2xx.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Machine-readable response code (e.g. JOURNEY_INITIALIZED, VALIDATION_FAILED).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable message associated with the response code.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// The response data. Null when Success is false.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Collection of structured errors (validation, field-level, etc).
    /// Empty when Success is true.
    /// </summary>
    public List<ApiError> Errors { get; set; } = new();

    /// <summary>
    /// Meta information (request id, timestamp, etc).
    /// </summary>
    public ApiMeta Meta { get; set; } = new();

    #region Legacy properties for backward compatibility

    /// <summary>
    /// [LEGACY] Single error object. Prefer using Errors collection instead.
    /// </summary>
    [Obsolete("Use Errors collection instead.")]
    public ApiError? Error { get; set; }

    /// <summary>
    /// [LEGACY] Correlation/Trace identifier for request tracking.
    /// Prefer Meta.RequestId.
    /// </summary>
    [Obsolete("Use Meta.RequestId instead.")]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// [LEGACY] UTC timestamp when the response was generated.
    /// Prefer Meta.Timestamp.
    /// </summary>
    [Obsolete("Use Meta.Timestamp instead.")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// [LEGACY] Creates a successful response with data.
    /// Maintained for backward compatibility with existing code.
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string traceId)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = Api.ApiResponseCode.SUCCESS.ToString(),
            Message = Api.ApiResponseCodes.Registry[Api.ApiResponseCode.SUCCESS].Message,
            Data = data,
            Errors = new List<ApiError>(),
            Meta = new ApiMeta
            {
                RequestId = traceId,
                Timestamp = DateTime.UtcNow
            },
            Error = null,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// [LEGACY] Creates an error response.
    /// Maintained for backward compatibility with existing code.
    /// </summary>
    public static ApiResponse<T> ErrorResponse(ApiError error, string traceId)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Code = error.Code,
            Message = error.Message,
            Data = default,
            Errors = new List<ApiError> { error },
            Meta = new ApiMeta
            {
                RequestId = traceId,
                Timestamp = DateTime.UtcNow
            },
            Error = error,
            TraceId = traceId,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// [LEGACY] Creates a successful response with data and optional message.
    /// Maintained for backward compatibility with existing code.
    /// </summary>
    [Obsolete("Use ApiResponseFactory.Success instead. This method is maintained for backward compatibility.")]
    public static ApiResponse<T> CreateSuccess(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Code = Api.ApiResponseCode.SUCCESS.ToString(),
            Message = message ?? Api.ApiResponseCodes.Registry[Api.ApiResponseCode.SUCCESS].Message,
            Data = data,
            Errors = new List<ApiError>(),
            Meta = new ApiMeta
            {
                Timestamp = DateTime.UtcNow
            }
        };
    }

    #endregion
}

/// <summary>
/// Standardized error information for API responses.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Name of the field this error relates to (optional for non-field errors).
    /// </summary>
    public string? Field { get; set; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    #region Legacy properties and factory helpers (backward compatibility)

    /// <summary>
    /// [LEGACY] Machine-readable error code in UPPER_SNAKE_CASE format.
    /// </summary>
    [Obsolete("Use ApiResponseCode + ApiResponseCodes instead.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// [LEGACY] HTTP status code associated with this error.
    /// </summary>
    [Obsolete("Use ApiResponseCodes metadata instead.")]
    public int Status { get; set; }

    /// <summary>
    /// [LEGACY] Additional error details (optional).
    /// </summary>
    [Obsolete("Use Meta or domain-specific DTOs instead.")]
    public string? Details { get; set; }

    /// <summary>
    /// [LEGACY] Validation errors dictionary (field name -> array of error messages).
    /// Only populated for validation errors (400 BadRequest).
    /// </summary>
    [Obsolete("Use Errors collection with field names instead.")]
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    [Obsolete("Use ApiResponseFactory.Failure with ApiResponseCode.VALIDATION_FAILED instead.")]
    public static ApiError ValidationError(string message, Dictionary<string, string[]> validationErrors, string? details = null)
    {
        return new ApiError
        {
            Code = "VALIDATION_ERROR",
            Message = message,
            Status = 400,
            Details = details,
            ValidationErrors = validationErrors
        };
    }

    [Obsolete("Use ApiResponseFactory.Failure with ApiResponseCode.RESOURCE_NOT_FOUND instead.")]
    public static ApiError NotFound(string code, string message, string? details = null)
    {
        return new ApiError
        {
            Code = code,
            Message = message,
            Status = 404,
            Details = details
        };
    }

    [Obsolete("Use ApiResponseFactory.Failure with ApiResponseCode.UNAUTHORIZED instead.")]
    public static ApiError Unauthorized(string message, string? details = null)
    {
        return new ApiError
        {
            Code = "UNAUTHORIZED",
            Message = message,
            Status = 401,
            Details = details
        };
    }

    [Obsolete("Use ApiResponseFactory.Failure with ApiResponseCode.FORBIDDEN instead.")]
    public static ApiError Forbidden(string message, string? details = null)
    {
        return new ApiError
        {
            Code = "FORBIDDEN",
            Message = message,
            Status = 403,
            Details = details
        };
    }

    [Obsolete("Use ApiResponseFactory.Failure with appropriate ApiResponseCode instead.")]
    public static ApiError Conflict(string code, string message, string? details = null)
    {
        return new ApiError
        {
            Code = code,
            Message = message,
            Status = 409,
            Details = details
        };
    }

    [Obsolete("Use ApiResponseFactory.Failure with appropriate ApiResponseCode instead.")]
    public static ApiError UnprocessableEntity(string code, string message, string? details = null)
    {
        return new ApiError
        {
            Code = code,
            Message = message,
            Status = 422,
            Details = details
        };
    }

    [Obsolete("Use ApiResponseFactory.Failure with ApiResponseCode.INTERNAL_ERROR instead.")]
    public static ApiError InternalServerError(string? details = null)
    {
        return new ApiError
        {
            Code = "INTERNAL_SERVER_ERROR",
            Message = "An unexpected error occurred. Please try again later.",
            Status = 500,
            Details = details
        };
    }

    #endregion
}

/// <summary>
/// Meta information returned with every API response.
/// </summary>
public class ApiMeta
{
    /// <summary>
    /// Correlation/request identifier (e.g. HttpContext.TraceIdentifier).
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// UTC timestamp when the response was generated.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// [LEGACY] Backward compatibility class for existing code using ApiErrorResponse.
/// This class wraps error information in a non-generic envelope.
/// </summary>
[Obsolete("Use ApiResponse<T> + ApiResponseFactory instead. This class is maintained for backward compatibility.")]
public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Status { get; set; } = "error";
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<string>? Errors { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiErrorResponse CreateError(string errorCode, string message, List<string>? errors = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Status = "error",
            ErrorCode = errorCode,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow
        };
    }
}

# API Response Standardization

## Constitution Reference

This document is a backend implementation standard under:

- `V:\DevelopmentRepo\V1.0\docs\PRAMAN-Architecture-Constitution.md`

If this document conflicts with the constitution, the constitution takes precedence.

---

production-ready pattern used in large .NET platforms that fits your architecture:

consistent response wrapper

response code registry (no hardcoded strings)

enum + metadata mapping

easy Angular interpretation

future localization ready

1. Standard API Response Model

All APIs should return one consistent envelope.

Example success:

{
  "success": true,
  "code": "JOURNEY_INITIALIZED",
  "message": "Compliance journey initialized successfully",
  "data": { },
  "errors": [],
  "meta": {
    "timestamp": "2026-03-12T10:23:00Z",
    "requestId": "req-342343"
  }
}

Example validation error:

{
  "success": false,
  "code": "VALIDATION_FAILED",
  "message": "Input validation failed",
  "errors": [
    {
      "field": "email",
      "message": "Email already exists"
    }
  ],
  "data": null
}

Example system error:

{
  "success": false,
  "code": "INTERNAL_ERROR",
  "message": "Unexpected server error",
  "errors": [],
  "data": null
}
2. Generic Response Wrapper

Create a single response wrapper class.

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Code { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public List<ApiError> Errors { get; set; }
    public ApiMeta Meta { get; set; }
}

Error structure:

public class ApiError
{
    public string Field { get; set; }
    public string Message { get; set; }
}

Meta information:

public class ApiMeta
{
    public string RequestId { get; set; }
    public DateTime Timestamp { get; set; }
}
3. Response Code Metadata Registry

Now the important part.

Create centralized response metadata so messages are never hardcoded.

Response Code Enum
public enum ApiResponseCode
{
    SUCCESS,

    VALIDATION_FAILED,
    UNAUTHORIZED,
    FORBIDDEN,
    RESOURCE_NOT_FOUND,

    INTERNAL_ERROR,

    HOSPITAL_CREATED,
    JOURNEY_INITIALIZED,
    TASK_GENERATED,
    EVIDENCE_UPLOADED,
    REPORT_GENERATED
}
Response Code Metadata

Create a metadata registry.

public class ApiResponseCodeMetadata
{
    public ApiResponseCode Code { get; set; }
    public string Message { get; set; }
    public int HttpStatus { get; set; }
}
Response Code Catalog

Single source of truth.

public static class ApiResponseCodes
{
    public static readonly Dictionary<ApiResponseCode, ApiResponseCodeMetadata> Registry =
        new()
        {
            { ApiResponseCode.SUCCESS,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.SUCCESS,
                    Message = "Operation completed successfully",
                    HttpStatus = 200
                }
            },

            { ApiResponseCode.HOSPITAL_CREATED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.HOSPITAL_CREATED,
                    Message = "Hospital created successfully",
                    HttpStatus = 201
                }
            },

            { ApiResponseCode.VALIDATION_FAILED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.VALIDATION_FAILED,
                    Message = "Input validation failed",
                    HttpStatus = 400
                }
            },

            { ApiResponseCode.UNAUTHORIZED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.UNAUTHORIZED,
                    Message = "Authentication required",
                    HttpStatus = 401
                }
            },

            { ApiResponseCode.INTERNAL_ERROR,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.INTERNAL_ERROR,
                    Message = "Unexpected server error",
                    HttpStatus = 500
                }
            }
        };
}
4. Response Factory

Now controllers never construct responses manually.

Use a factory.

public static class ApiResponseFactory
{
    public static ApiResponse<T> Success<T>(ApiResponseCode code, T data)
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
                Timestamp = DateTime.UtcNow
            }
        };
    }

    public static ApiResponse<T> Failure<T>(ApiResponseCode code, List<ApiError> errors)
    {
        var meta = ApiResponseCodes.Registry[code];

        return new ApiResponse<T>
        {
            Success = false,
            Code = code.ToString(),
            Message = meta.Message,
            Data = default,
            Errors = errors
        };
    }
}
5. Controller Usage

Controller becomes very clean.

[HttpPost]
public async Task<IActionResult> CreateHospital(CreateHospitalRequest request)
{
    var hospital = await _service.CreateHospital(request);

    var response = ApiResponseFactory.Success(
        ApiResponseCode.HOSPITAL_CREATED,
        hospital);

    return StatusCode(
        ApiResponseCodes.Registry[ApiResponseCode.HOSPITAL_CREATED].HttpStatus,
        response);
}
6. Angular Interpretation

Operator Portal interceptor can do:

if (!response.success) {
   showToast(response.message);
}

Or handle codes:

switch(response.code) {
   case "VALIDATION_FAILED":
   case "UNAUTHORIZED":
}
7. Response Code Categories

Recommended categories:

General
SUCCESS
VALIDATION_FAILED
INTERNAL_ERROR
RESOURCE_NOT_FOUND
Auth
UNAUTHORIZED
INVALID_CREDENTIALS
TOKEN_EXPIRED
Compliance
JOURNEY_INITIALIZED
TASK_GENERATED
EVIDENCE_UPLOADED
COMPLIANCE_SCORE_UPDATED
Hospital
HOSPITAL_CREATED
HOSPITAL_UPDATED
Reporting
REPORT_GENERATED
REPORT_EXPORT_READY
8. Optional (Advanced)

Add response codes JSON config.

Example:

{
  "HOSPITAL_CREATED": {
    "message": "Hospital created successfully",
    "status": 201
  }
}

Then load into registry at startup.

But enum + dictionary is simpler and safer.

9. Recommended Folder Structure
Shared
 ├ Api
 │  ├ ApiResponse.cs
 │  ├ ApiError.cs
 │  ├ ApiMeta.cs
 │  ├ ApiResponseFactory.cs
 │  ├ ApiResponseCode.cs
 │  └ ApiResponseCodes.cs
10. Result

You now get:

no hardcoded messages

single source of truth

consistent responses

clean controllers

Angular-friendly API
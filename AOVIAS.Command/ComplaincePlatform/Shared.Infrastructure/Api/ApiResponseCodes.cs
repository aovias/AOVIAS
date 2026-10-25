using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Http;

namespace Shared.Infrastructure.Api;

/// <summary>
/// Central registry of response codes and their metadata.
/// Controllers and middleware should NOT hardcode messages or status codes.
/// </summary>
public static class ApiResponseCodes
{
    public static readonly ReadOnlyDictionary<ApiResponseCode, ApiResponseCodeMetadata> Registry =
        new(new Dictionary<ApiResponseCode, ApiResponseCodeMetadata>
        {
            {
                ApiResponseCode.SUCCESS,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.SUCCESS,
                    Message = "Operation completed successfully",
                    HttpStatus = StatusCodes.Status200OK
                }
            },
            {
                ApiResponseCode.VALIDATION_FAILED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.VALIDATION_FAILED,
                    Message = "Input validation failed",
                    HttpStatus = StatusCodes.Status400BadRequest
                }
            },
            {
                ApiResponseCode.UNAUTHORIZED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.UNAUTHORIZED,
                    Message = "Authentication required",
                    HttpStatus = StatusCodes.Status401Unauthorized
                }
            },
            {
                ApiResponseCode.FORBIDDEN,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.FORBIDDEN,
                    Message = "Access forbidden",
                    HttpStatus = StatusCodes.Status403Forbidden
                }
            },
            {
                ApiResponseCode.RESOURCE_NOT_FOUND,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.RESOURCE_NOT_FOUND,
                    Message = "Requested resource was not found",
                    HttpStatus = StatusCodes.Status404NotFound
                }
            },
            {
                ApiResponseCode.RESOURCE_CONFLICT,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.RESOURCE_CONFLICT,
                    Message = "Resource conflict: resource already exists or conflicts with existing data",
                    HttpStatus = StatusCodes.Status409Conflict
                }
            },
            {
                ApiResponseCode.INTERNAL_ERROR,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.INTERNAL_ERROR,
                    Message = "Unexpected server error",
                    HttpStatus = StatusCodes.Status500InternalServerError
                }
            },
            {
                ApiResponseCode.HOSPITAL_CREATED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.HOSPITAL_CREATED,
                    Message = "Hospital created successfully",
                    HttpStatus = StatusCodes.Status201Created
                }
            },
            {
                ApiResponseCode.HOSPITAL_ONBOARDED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.HOSPITAL_ONBOARDED,
                    Message = "Hospital onboarding completed successfully",
                    HttpStatus = StatusCodes.Status201Created
                }
            },
            {
                ApiResponseCode.ONBOARDING_WIZARD_COMPLETED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.ONBOARDING_WIZARD_COMPLETED,
                    Message = "Onboarding wizard completed successfully",
                    HttpStatus = StatusCodes.Status200OK
                }
            },
            {
                ApiResponseCode.ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY,
                    Message = "Onboarding wizard response replayed from idempotency cache",
                    HttpStatus = StatusCodes.Status200OK
                }
            },
            {
                ApiResponseCode.ONBOARDING_WIZARD_PARTIAL_FAILURE,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.ONBOARDING_WIZARD_PARTIAL_FAILURE,
                    Message = "Onboarding wizard did not complete successfully; partial changes may exist",
                    HttpStatus = StatusCodes.Status422UnprocessableEntity
                }
            },
            {
                ApiResponseCode.JOURNEY_INITIALIZED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.JOURNEY_INITIALIZED,
                    Message = "Compliance journey initialized successfully",
                    HttpStatus = StatusCodes.Status201Created
                }
            },
            {
                ApiResponseCode.TASK_GENERATED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.TASK_GENERATED,
                    Message = "Compliance tasks generated successfully",
                    HttpStatus = StatusCodes.Status200OK
                }
            },
            {
                ApiResponseCode.EVIDENCE_UPLOADED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.EVIDENCE_UPLOADED,
                    Message = "Evidence uploaded successfully",
                    HttpStatus = StatusCodes.Status201Created
                }
            },
            {
                ApiResponseCode.COMPLIANCE_SCORE_UPDATED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.COMPLIANCE_SCORE_UPDATED,
                    Message = "Compliance score calculated successfully",
                    HttpStatus = StatusCodes.Status200OK
                }
            },
            {
                ApiResponseCode.REPORT_GENERATED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.REPORT_GENERATED,
                    Message = "Report generated successfully",
                    HttpStatus = StatusCodes.Status200OK
                }
            },
            {
                ApiResponseCode.HOSPITAL_CLAIM_INVALID,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.HOSPITAL_CLAIM_INVALID,
                    Message = "Missing or invalid hospital_id claim in token",
                    HttpStatus = StatusCodes.Status401Unauthorized
                }
            },
            {
                ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.ORGANIZATION_CONTEXT_MISSING,
                    Message = "Organization context is missing from the current request",
                    HttpStatus = StatusCodes.Status401Unauthorized
                }
            },
            {
                ApiResponseCode.HOSPITAL_ID_REQUIRED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.HOSPITAL_ID_REQUIRED,
                    Message = "HospitalId is required",
                    HttpStatus = StatusCodes.Status400BadRequest
                }
            },
            {
                ApiResponseCode.TOO_MANY_REQUESTS,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.TOO_MANY_REQUESTS,
                    Message = "Too many requests. Please try again later.",
                    HttpStatus = StatusCodes.Status429TooManyRequests
                }
            },
            {
                ApiResponseCode.SERVICE_UNAVAILABLE,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.SERVICE_UNAVAILABLE,
                    Message = "Downstream service is currently unavailable. Please try again later.",
                    HttpStatus = StatusCodes.Status502BadGateway
                }
            },
            {
                ApiResponseCode.SERVICE_TIMEOUT,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.SERVICE_TIMEOUT,
                    Message = "Downstream service timeout. Please try again later.",
                    HttpStatus = StatusCodes.Status502BadGateway
                }
            },
            {
                ApiResponseCode.SERVICE_COMMUNICATION_ERROR,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.SERVICE_COMMUNICATION_ERROR,
                    Message = "Error communicating with downstream service. Please try again later.",
                    HttpStatus = StatusCodes.Status502BadGateway
                }
            },
            {
                ApiResponseCode.STAGE_LOCKED,
                new ApiResponseCodeMetadata
                {
                    Code = ApiResponseCode.STAGE_LOCKED,
                    Message = "This maturity stage is locked. Complete the prerequisite stage first.",
                    HttpStatus = 423   // 423 Locked (WebDAV / RFC 4918)
                }
            }
        });
}


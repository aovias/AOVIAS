namespace Shared.Infrastructure.Api;

/// <summary>
/// Canonical response codes for all PRAMAN APIs.
/// Backed by metadata in <see cref="ApiResponseCodes"/>.
/// </summary>
public enum ApiResponseCode
{
    // General
    SUCCESS,
    VALIDATION_FAILED,
    UNAUTHORIZED,
    FORBIDDEN,
    RESOURCE_NOT_FOUND,
    RESOURCE_CONFLICT,
    INTERNAL_ERROR,

    // Domain / Business
    HOSPITAL_CREATED,
    HOSPITAL_ONBOARDED,
    /// <summary>Second-step onboarding wizard completed (hospital profile, frameworks, assessment, invites).</summary>
    ONBOARDING_WIZARD_COMPLETED,
    /// <summary>Same logical POST was completed earlier; response body is replayed from idempotency cache (same <c>Idempotency-Key</c>).</summary>
    ONBOARDING_WIZARD_IDEMPOTENCY_REPLAY,
    /// <summary>Wizard orchestration failed after one or more steps may have succeeded; client may retry with care (see logs).</summary>
    ONBOARDING_WIZARD_PARTIAL_FAILURE,
    JOURNEY_INITIALIZED,
    TASK_GENERATED,
    EVIDENCE_UPLOADED,
    COMPLIANCE_SCORE_UPDATED,
    REPORT_GENERATED,

    // Context / Validation specifics
    HOSPITAL_CLAIM_INVALID,
    ORGANIZATION_CONTEXT_MISSING,
    HOSPITAL_ID_REQUIRED,

    // Rate limiting / proxy
    TOO_MANY_REQUESTS,
    SERVICE_UNAVAILABLE,
    SERVICE_TIMEOUT,
    SERVICE_COMMUNICATION_ERROR,

    // Maturity stage-gate
    /// <summary>
    /// The requested Objective Element belongs to a maturity stage that is currently locked.
    /// The prerequisite stage threshold must be met before accessing this stage. HTTP 423 Locked.
    /// </summary>
    STAGE_LOCKED
}


## PRAMAN Backend API Inventory (Draft)

This document catalogs the main HTTP API endpoints across the PRAMAN backend services and provides a structure you can use to generate and maintain automated Postman collections.

> Status: This draft focuses on **OrganizationService** and **ComplianceService** in full detail for their primary controllers, and outlines patterns for Gateway, IdentityService, and AuditService. It is structured so it can be incrementally completed for all controllers.

---

## 1. OrganizationService

### 1.1 `V1/OrganizationsController`

- **Base Route**: `/api/v1/organizations`
- **Auth**: `[Authorize(Policy = "OrganizationScope")]`

#### Endpoint: `Ping`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/ping`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `string` (message: `"Organization API v1 is running"`)
- Failure
  - Not explicitly mapped (handled by global middleware if configured).

**Authentication**
- Authorized (`OrganizationScope`)

---

#### Endpoint: `Create`
- **Method**: `POST`
- **Route**: `/api/v1/organizations/Create`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `CreateOrganizationDto`
  - Example:
    ```json
    {
      "name": "Acme Hospital",
      "code": "ACME"
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: whatever `_orgService.CreateAsync` returns (Organization DTO)
- Failure
  - Not explicitly mapped in controller.

**Authentication**
- Authorized (`OrganizationScope`)

---

#### Endpoint: `GetOrganizationDetail`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/{id}/detail`

**Request**
- Route Parameters
  - `id` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `OrganizationDetailResponseDto`
- Failure
  - `RESOURCE_NOT_FOUND` – returned when detail is `null`

**Authentication**
- Authorized (`OrganizationScope`)

---

### 1.2 `V1/CommonController`

- **Base Route**: `/api/v1/common`
- **Auth**: `[Authorize(Policy = "OrganizationScope")]`

#### Endpoint: `GetDepartments`
- **Method**: `GET`
- **Route**: `/api/v1/common/departments`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `IReadOnlyList<DepartmentDto>`
- Failure
  - Not explicitly mapped.

**Authentication**
- Authorized (`OrganizationScope`)

---

#### Endpoint: `GetServices`
- **Method**: `GET`
- **Route**: `/api/v1/common/services`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `IReadOnlyList<ServiceDto>`
- Failure
  - Not explicitly mapped.

**Authentication**
- Authorized (`OrganizationScope`)

---

### 1.3 `Internal/InternalSubscriptionsController`

- **Base Route**: `/internal/subscriptions`
- **Auth**: `[AllowAnonymous]` (secured at network/gateway level)

#### Endpoint: `GetCurrent`
- **Method**: `GET`
- **Route**: `/internal/subscriptions/current`

**Request**
- Route Parameters: none
- Query Parameters
  - `organizationId` : `Guid` (required, non-empty)
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: current subscription DTO
- Failure
  - `VALIDATION_FAILED` – empty `organizationId`
  - `RESOURCE_NOT_FOUND` – no active subscription found

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `Assign`
- **Method**: `POST`
- **Route**: `/internal/subscriptions`

**Request**
- Body
  - DTO Type: `AssignSubscriptionRequest`
  - Example:
    ```json
    {
      "organizationId": "00000000-0000-0000-0000-000000000001",
      "subscriptionPlanId": "00000000-0000-0000-0000-000000000002",
      "isTrial": true
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: subscription DTO
- Failure
  - `VALIDATION_FAILED` – invalid IDs / subscription rule violation

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `Upgrade`
- **Method**: `PATCH`
- **Route**: `/internal/subscriptions/upgrade`

**Request**
- Body
  - DTO Type: `UpgradeSubscriptionRequest`
  - Example:
    ```json
    {
      "organizationId": "00000000-0000-0000-0000-000000000001",
      "targetPlanId": "00000000-0000-0000-0000-000000000003"
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: subscription DTO
- Failure
  - `VALIDATION_FAILED` – invalid IDs or business rule failure

**Authentication**
- Anonymous (internal)

---

### 1.4 `Internal/InternalOrganizationController`

- **Base Route**: `/internal`
- **Auth**: `[AllowAnonymous]`

#### Endpoint: `CreateOrganization`
- **Method**: `POST`
- **Route**: `/internal/organizations`

**Request**
- Body
  - DTO Type: `CreateOrganizationDto`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ id, name, platformId }`
- Failure
  - `VALIDATION_FAILED` – business-rule failure
  - `INTERNAL_ERROR` – unhandled exceptions

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `DeleteOrganization`
- **Method**: `DELETE`
- **Route**: `/internal/organizations/{id}`

**Request**
- Route Parameters
  - `id` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS` (204 NoContent)
- Failure
  - `RESOURCE_NOT_FOUND` – organization not found
  - `INTERNAL_ERROR` – unexpected errors

**Authentication**
- Anonymous (internal)

---

## 2. Gateway

> Note: Gateway primarily acts as a BFF / proxy layer in front of backend microservices. Many actions return raw `IActionResult` proxied from handlers or `IProxyService` rather than directly using `ApiResponse<T>`. The catalog below focuses on HTTP surface area, parameters, and high-level response behavior.

### 2.1 `AIController`

- **Base Route**: `/api/v1/ai`
- **Auth**: `[Authorize]` with `[EnableRateLimiting("AIPolicy")]`

#### Endpoint: `ExplainTask`
- **Method**: `POST`
- **Route**: `/api/v1/ai/explain-task`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `object` (dynamic payload forwarded to AI orchestrator)
  - Example:
    ```json
    {
      "taskId": "00000000-0000-0000-0000-000000000001",
      "question": "Explain what this compliance task requires and typical evidence."
    }
    ```

**Response**
- Success
  - ApiResponseCode: depends on downstream AIOrchestratorService; typically `SUCCESS` or domain-specific codes.
  - Data Type: `ApiResponse<object>` from downstream service, forwarded by the handler.
- Failure
  - `HOSPITAL_CLAIM_INVALID` – when `hospital_id` claim is missing or invalid in JWT.
  - Other failures are mapped by downstream AI service and global exception middleware.

**Authentication**
- Authorized (`[Authorize]`, rate-limited via `"AIPolicy"`)

---

### 2.2 `SubscriptionsController`

- **Base Route**: `/api/v1/subscriptions`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `GetCurrent`
- **Method**: `GET`
- **Route**: `/api/v1/subscriptions/current`

**Request**
- Route Parameters: none
- Query Parameters
  - `organizationId` : `Guid` (required, non-empty)
- Body: none

**Response**
- Success
  - ApiResponseCode: via internal handler; typically `SUCCESS`
  - Data Type: `OrganizationSubscriptionSummaryDto`
- Failure
  - Validation failures (e.g., missing `organizationId`) returned as `ApiResponse<OrganizationSubscriptionSummaryDto>.ErrorResponse` with HTTP 400.
  - Additional domain-specific error codes from OrganizationService proxy (e.g., `RESOURCE_NOT_FOUND`).

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `Create`
- **Method**: `POST`
- **Route**: `/api/v1/subscriptions`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `CreateSubscriptionRequestDto`
  - Example:
    ```json
    {
      "organizationId": "00000000-0000-0000-0000-000000000001",
      "subscriptionPlanId": "00000000-0000-0000-0000-000000000010",
      "isTrial": true,
      "startsAt": "2025-01-01T00:00:00Z"
    }
    ```

**Response**
- Success
  - ApiResponseCode: via handler; typically `SUCCESS`
  - Data Type: `OrganizationSubscriptionSummaryDto`
- Failure
  - Validation / business rule errors mapped by handler and OrganizationService.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `Upgrade`
- **Method**: `PATCH`
- **Route**: `/api/v1/subscriptions/upgrade`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `UpgradeSubscriptionRequestDto`
  - Example:
    ```json
    {
      "organizationId": "00000000-0000-0000-0000-000000000001",
      "targetPlanId": "00000000-0000-0000-0000-000000000020"
    }
    ```

**Response**
- Success
  - ApiResponseCode: via handler; typically `SUCCESS`
  - Data Type: `OrganizationSubscriptionSummaryDto`
- Failure
  - Validation / business rule errors mapped by handler and OrganizationService.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.3 `DashboardController` (Gateway)

- **Base Route**: `/api/v1/dashboard`
- **Auth**: `[Authorize]`

#### Endpoint: `GetSummary`
- **Method**: `GET`
- **Route**: `/api/v1/dashboard/summary`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none (hospital context read from JWT claim `hospital_id`)

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `DashboardSummaryDto`
- Failure
  - `UNAUTHORIZED` – when `hospital_id` claim is missing/invalid.
  - Other failures via global exception middleware or downstream ComplianceService.

**Authentication**
- Authorized (JWT required with `hospital_id` claim)

---

#### Endpoint: `GetProgramOverviewSnapshot`
- **Method**: `GET`
- **Route**:
  - `/api/v1/dashboard/program-overview-snapshot`
  - `/api/v1/dashboard/{hospitalId}/program-overview-snapshot`

**Request**
- Route Parameters
  - `hospitalId` : `Guid?` (optional when using path-based variant)
- Query Parameters
  - `hospitalIdQuery` : `Guid?` (optional)
  - `frameworkId` : `Guid?` (optional)
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `DashboardSnapshotDto` (nullable)
    - Provides a consolidated readiness view including:
      - `summary` metrics (completion rate, upcoming expiries, framework count)
      - `frameworks` list with status counts, milestones, and recent activities
      - `tasksSummary` (totals, overdue/due-soon buckets, priority/type breakdowns)
      - `hospitals` overview with journey cards and task counts
      - `scoreTrends`, `milestones`, `alerts`, and `filters` for UI wiring
- Failure
  - `VALIDATION_FAILED` – missing/empty hospital ID.
  - `UNAUTHORIZED` – missing/invalid `hospital_id` claim.
  - `FORBIDDEN` – hospital mismatch between route/query and token claim.
  - `RESOURCE_NOT_FOUND` – snapshot not found.

**Authentication**
- Authorized (JWT with `hospital_id` claim)

---

### 2.4 `JourneysController` (Gateway)

- **Base Route**: `/api/v1/journeys`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `CreateJourney`
- **Method**: `POST`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/journeys`

**Request**
- Route Parameters
  - `hospitalId` : `Guid` (required, non-empty)
  - `frameworkId` : `Guid` (required, non-empty)
- Query Parameters: none
- Body: none (context carried via route; details handled inside `CreateJourneyCommand`)

**Response**
- Success
  - ApiResponseCode: determined by downstream ComplianceService; typically `JOURNEY_INITIALIZED` or `SUCCESS`.
  - Data Type: journey DTO from ComplianceService.
- Failure
  - Validation errors for missing IDs – returned as HTTP 400 with `{ "error": "..." }`.
  - Other failures handled by handlers and middleware.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetJourneyDetails`
- **Method**: `GET`
- **Route**: `/api/v1/journeys/{journeyIdOrComposite}`

**Request**
- Route Parameters
  - `journeyIdOrComposite` : `string` (GUID or `hospitalId_frameworkId`)
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService (e.g., `SUCCESS`).
  - Data Type: journey detail DTO.
- Failure
  - HTTP 400 with `{ error = "..." }` for invalid/missing IDs or bad composite format.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetJourneyByHospitalAndFramework`
- **Method**: `GET`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/journey`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
  - `frameworkId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService (`SUCCESS` / domain-specific).
  - Data Type: journey DTO.
- Failure
  - HTTP 400 with `{ error = "..." }` when route IDs missing/empty.
  - HTTP 401 with `{ error = "Missing or invalid hospital_id claim in token" }` when token claim invalid.
  - HTTP 403 when hospital mismatch between route and token.

**Authentication**
- Authorized (`ComplianceScope` + JWT `hospital_id` claim)

---

#### Endpoint: `GetValidTransitions`
- **Method**: `GET`
- **Route**: `/api/v1/journeys/{journeyId}/valid-transitions`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService.
  - Data Type: list of allowed transitions / states.
- Failure
  - HTTP 400 with `{ error = "JourneyId is required" }` for empty GUID.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `TransitionJourney`
- **Method**: `POST`
- **Route**: `/api/v1/journeys/{journeyId}/transition`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Query Parameters: none
- Body
  - DTO Type: `object` representing transition payload (e.g., `targetState`, metadata).

**Response**
- Success
  - ApiResponseCode: lifecycle state change code from ComplianceService.
  - Data Type: journey DTO / transition result.
- Failure
  - HTTP 400 with `{ error = "JourneyId is required" }` for empty GUID.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `TransitionJourneyPlural`
- **Method**: `POST`
- **Route**: `/api/v1/journeys/{journeyId}/transitions`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Query Parameters: none
- Body
  - DTO Type: `object` (same as `TransitionJourney`).

**Response**
- As per `TransitionJourney`.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `ApproveJourney`
- **Method**: `POST`
- **Route**: `/api/v1/journeys/{journeyId}/approve`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Query Parameters: none
- Body
  - DTO Type: `ApproveJourneyRequest`

**Response**
- Success
  - ApiResponseCode: approval / lifecycle domain code.
  - Data Type: journey DTO after approval.
- Failure
  - HTTP 400 when `journeyId` is empty.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.5 `AuditLogsController` (Gateway)

- **Base Route**: `/api/v1/audit-logs`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `GetAuditLogs`
- **Method**: `GET`
- **Route**: `/api/v1/audit-logs`

**Request**
- Route Parameters: none
- Query Parameters
  - `entityType` : `string?`
  - `entityId` : `Guid?`
  - `from` : `DateTime?`
  - `to` : `DateTime?`
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService (e.g., `SUCCESS`).
  - Data Type: list/page of audit log DTOs.
- Failure
  - As per ComplianceService and middleware.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetAuditLogsForEntity`
- **Method**: `GET`
- **Route**: `/api/v1/audit-logs/{entityType}/{entityId}`

**Request**
- Route Parameters
  - `entityType` : `string`
  - `entityId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService.
  - Data Type: list of audit logs for the entity.
- Failure
  - HTTP 400 with `{ error = "EntityType and EntityId are required" }` when invalid.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.6 `ReportingController` (Gateway)

- **Base Route**: `/api/v1/reports`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `GetComplianceScoreReport`
- **Method**: `GET`
- **Route**: `/api/v1/reports/compliance-score`

**Request**
- Route Parameters: none
- Query Parameters
  - `hospitalId` : `Guid` (required)
  - `frameworkId` : `Guid?`
  - `from` : `DateTime?`
  - `to` : `DateTime?`
- Body: none

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED` / `SUCCESS` from ComplianceService.
  - Data Type: `ComplianceScoreReportDto` (proxied).
- Failure
  - HTTP 400 with `{ error = "HospitalId is required" }`.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetAuditFindingsReport`
- **Method**: `GET`
- **Route**: `/api/v1/reports/audit-findings`

**Request**
- Route Parameters: none
- Query Parameters
  - `hospitalId` : `Guid` (required)
  - `frameworkId` : `Guid?`
  - `from` : `DateTime?`
  - `to` : `DateTime?`
- Body: none

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED` / `SUCCESS` from ComplianceService.
  - Data Type: `AuditFindingsReportDto` (proxied).
- Failure
  - HTTP 400 with `{ error = "HospitalId is required" }`.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetNabhReadinessReport`
- **Method**: `GET`
- **Route**: `/api/v1/reports/nabh-readiness`

**Request**
- Route Parameters: none
- Query Parameters
  - `hospitalId` : `Guid` (required)
- Body: none

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED` / `SUCCESS`.
  - Data Type: `NabhReadinessReportDto` (proxied).
- Failure
  - HTTP 400 with `{ error = "HospitalId is required" }`.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.7 `AuditFindingsController` (Gateway)

- **Base Route**: `/api/v1/` (controller-level route is a bare prefix; actions specify full sub-routes)
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `GetAuditFindings`
- **Method**: `GET`
- **Route**: `/api/v1/journeys/{journeyId}/audit-findings`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService (`SUCCESS`).
  - Data Type: list of `ComplianceAuditFindingDto` (proxied).
- Failure
  - HTTP 400 with `{ error = "JourneyId is required" }`.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `CreateAuditFinding`
- **Method**: `POST`
- **Route**: `/api/v1/audit/findings`

**Request**
- Route Parameters: none
- Query Parameters
  - `journeyId` : `Guid` (required)
- Body
  - DTO Type: `object` (finding payload)
  - Example:
    ```json
    {
      "clauseId": "00000000-0000-0000-0000-000000000001",
      "type": "Major",
      "description": "Missing documented procedure",
      "dueDate": "2025-02-01T00:00:00Z"
    }
    ```

**Response**
- Success
  - ApiResponseCode: from ComplianceService (`AUDIT_FINDING_CREATED` or similar).
  - Data Type: created finding DTO.
- Failure
  - HTTP 400 with `{ error = "journeyId query parameter is required" }`.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.8 `OnboardingController`

- **Base Route**: `/api/v1/onboarding`
- **Auth**: `[AllowAnonymous]`

#### Endpoint: `OnboardHospital`
- **Method**: `POST`
- **Route**: `/api/v1/onboarding/hospital`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `SignupRequestDto`
  - Example:
    ```json
    {
      "organization": {
        "name": "Acme Health System",
        "code": "ACME"
      },
      "hospital": {
        "name": "Acme City Hospital",
        "city": "Metropolis"
      },
      "adminUser": {
        "email": "admin@acme.com",
        "password": "P@ssw0rd!"
      }
    }
    ```

**Response**
- Success
  - ApiResponseCode: not wrapped; returns `SignupResponseDto` directly with HTTP 200.
  - Data Type: `SignupResponseDto`
- Failure
  - HTTP 409 with `{ error = ex.Message }` when entity already exists.
  - HTTP 400 with `{ error = ex.Message }` for other `InvalidOperationException`.
  - HTTP 500 with generic `{ error = "An unexpected error occurred during hospital onboarding. Please try again later." }` for unhandled exceptions.

**Authentication**
- Anonymous (public onboarding)

---

### 2.9 `CommonController` (Gateway)

- **Base Route**: `/api/v1/common`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `GetDepartments`
- **Method**: `GET`
- **Route**: `/api/v1/common/departments`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: depends on handler; typically `SUCCESS`
  - Data Type: `ApiResponse<IReadOnlyList<DepartmentDto>>` (wrapped by handler)
- Failure
  - Error codes carried in `ApiResponse.Error` from handler.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetServices`
- **Method**: `GET`
- **Route**: `/api/v1/common/services`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<IReadOnlyList<ServiceDto>>`
- Failure
  - Error codes via handler.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.10 `OrganizationsRegulatoryController`

- **Base Route**: `/api/v1/organizations/{orgId}`
- **Auth**:
  - `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`
  - `[Authorize(Policy = "OrgAccess")]` for multi-tenant access control

#### Endpoint: `GetRegulatoryOverview`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/{orgId}/regulatory-overview`

**Request**
- Route Parameters
  - `orgId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<RegulatoryOverviewDto>`
- Failure
  - Validation / access errors handled via policies and middleware.

**Authentication**
- Authorized, roles: `SUPER_ADMIN, REGULATORY_ADMIN, COMPLIANCE_VIEWER, GroupAdmin`

---

#### Endpoint: `UpdateJurisdiction`
- **Method**: `PUT`
- **Route**: `/api/v1/organizations/{orgId}/jurisdiction`

**Request**
- Route Parameters
  - `orgId` : `Guid`
- Query Parameters: none
- Body
  - DTO Type: `UpdateJurisdictionRequestDto`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<UpdateJurisdictionResponseDto>`
- Failure
  - Validation / domain errors encapsulated in `ApiResponse.Error`.

**Authentication**
- Authorized, roles: `SUPER_ADMIN, GroupAdmin`

---

#### Endpoint: `GetAccreditations`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/{orgId}/accreditations`

**Request**
- Route Parameters
  - `orgId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<IReadOnlyList<AccreditationSummaryDto>>`

**Authentication**
- Authorized, default roles; organization access enforced by `OrgAccess` policy.

---

#### Endpoint: `GetRenewals`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/{orgId}/renewals`

**Request**
- Route Parameters
  - `orgId` : `Guid`
- Query Parameters
  - `withinDays` : `int` (default 90)
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<IReadOnlyList<RenewalDto>>`

**Authentication**
- Authorized

---

#### Endpoint: `GetFrameworkCatalog`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/{orgId}/frameworks/catalog`

**Request**
- Route Parameters
  - `orgId` : `Guid`
- Query Parameters
  - `country` : `string?`
  - `accreditationBody` : `string?`
  - `type` : `string?`
  - `version` : `string?`
  - `page` : `int` (>= 1, default 1)
  - `pageSize` : `int` (clamped to [1, 100], default 20)
  - `search` : `string?`
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<FrameworkCatalogPageDto>`

**Authentication**
- Authorized, roles: `SUPER_ADMIN, REGULATORY_ADMIN, COMPLIANCE_VIEWER, GroupAdmin`

---

#### Endpoint: `GetActivationMetadata`
- **Method**: `GET`
- **Route**: `/api/v1/organizations/{orgId}/frameworks/{frameworkId}/activation-metadata`

**Request**
- Route Parameters
  - `orgId` : `Guid`
  - `frameworkId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<ActivationMetadataDto>`

**Authentication**
- Authorized, roles: `SUPER_ADMIN, REGULATORY_ADMIN, GroupAdmin`

---

#### Endpoint: `ActivateAccreditation`
- **Method**: `POST`
- **Route**: `/api/v1/organizations/{orgId}/accreditations`

**Request**
- Route Parameters
  - `orgId` : `Guid`
- Query Parameters: none
- Body
  - DTO Type: `ActivateAccreditationRequestDto`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ApiResponse<ActivateAccreditationResponseDto>`

**Authentication**
- Authorized, roles: `SUPER_ADMIN, REGULATORY_ADMIN, GroupAdmin`
- Supports `Idempotency-Key` header at infrastructure level.

---

### 2.11 `FrameworkController` (Gateway)

- **Base Route**: `/api/v1/frameworks`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Endpoint: `GetAllFrameworks`
- **Method**: `GET`
- **Route**: `/api/v1/frameworks/all-frameworks` (proxies to ComplianceService `GET /api/v1/frameworks`)

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService (`SUCCESS`)
  - Data Type: `ApiResponse` with `data`: list of `FrameworkDto` items (`frameworkId`, `name`, `authority` — issuing/accreditation body, e.g. QCI — `code`, `validityPeriodMonths`, `isActive`).

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `GetHospitalFrameworks`
- **Method**: `GET`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: from ComplianceService.
  - Data Type: list of frameworks.
- Failure
  - HTTP 400 with `{ error = "HospitalId is required" }` for empty GUID.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Endpoint: `EnrollHospitalInFramework`
- **Method**: `POST`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
  - `frameworkId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: `JOURNEY_INITIALIZED` / domain-specific from ComplianceService.
  - Data Type: enrollment / journey DTO.
- Failure
  - HTTP 400 for missing `hospitalId` or `frameworkId`.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.12 `TenantController`

- **Base Route**: `/api/v1/tenant`
- **Auth**: `[Authorize(Policy = "TenantScope")]`

#### Endpoint: `GetOrganizationDetail`
- **Method**: `GET`
- **Route**: `/api/v1/tenant/{tenantId}/organization-detail`

**Request**
- Route Parameters
  - `tenantId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: not wrapped in standardized envelope; returns `OrganizationDetailDto` directly.
- Failure
  - HTTP 400 with `{ errors = [...] }` for validation failures.
  - HTTP 404 with `{ error = ex.Message }` when not found.
  - HTTP 500 with generic error payload for unhandled exceptions.

**Authentication**
- Authorized (`TenantScope`)

---

### 2.13 `IdentityController` (Gateway)

- **Base Route**: `/api/v1/identity`
- **Auth**: Public-facing for auth flows with `[AllowAnonymous]` on actions.

#### Endpoint: `Login`
- **Method**: `POST`
- **Route**: `/api/v1/identity/login`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `object` (raw body proxied to IdentityService `login`)
  - Example:
    ```json
    {
      "email": "admin@acme.com",
      "password": "P@ssw0rd!"
    }
    ```

**Response**
- Success
  - ApiResponseCode: as defined by IdentityService (`SUCCESS` / `LOGIN_SUCCESS`).
  - Data Type: auth token DTO forwarded from IdentityService.

**Authentication**
- Anonymous

---

## 3. IdentityService

### 3.1 `V1/AuthController`

- **Base Route**: `/api/v1/auth`
- **Auth**: Mixed; most endpoints are `[AllowAnonymous]` except `logout` which is `[Authorize]`.

#### Endpoint: `Register`
- **Method**: `POST`
- **Route**: `/api/v1/auth/register`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `RegisterRequest`
  - Example:
    ```json
    {
      "email": "user@example.com",
      "password": "P@ssw0rd!",
      "role": "Admin"
    }
    ```

**Response**
- Success
  - ApiResponseCode: legacy `ApiResponse<RegisterResponse>.CreateSuccess(...)` style; conceptually `SUCCESS`.
  - Data Type: `RegisterResponse` wrapped in `ApiResponse<RegisterResponse>`.
- Failure
  - Validation failures (missing email/password, invalid email) returned as `ApiErrorResponse.CreateError(AuthErrorCodes.* , message)` with HTTP 400.
  - `EmailAlreadyRegistered` -> HTTP 409 with error payload.
  - Internal errors -> HTTP 500 with `AuthErrorCodes.InternalServerError`.

**Authentication**
- Anonymous

---

#### Endpoint: `Login`
- **Method**: `POST`
- **Route**: `/api/v1/auth/login`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `LoginRequest`
  - Example:
    ```json
    {
      "email": "user@example.com",
      "password": "P@ssw0rd!"
    }
    ```

**Response**
- Success
  - ApiResponseCode: legacy `ApiResponse<LoginResponse>.CreateSuccess(...)`; conceptually `SUCCESS` / `LOGIN_SUCCESS`.
  - Data Type: `LoginResponse` wrapped in `ApiResponse<LoginResponse>` (access token, refresh token, expiry, user and tenant context).
- Failure
  - `MissingCredentials` -> HTTP 400.
  - `InvalidCredentials` -> HTTP 401 with generic invalid credentials message.
  - `AccountLocked` -> HTTP 403 with lockout description.
  - `TokenGenerationFailed` or `InternalServerError` -> HTTP 500.

**Authentication**
- Anonymous

---

#### Endpoint: `Refresh`
- **Method**: `POST`
- **Route**: `/api/v1/auth/refresh`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `RefreshTokenRequest`
  - Example:
    ```json
    {
      "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS` (legacy pattern via `ApiResponse<RefreshTokenResponse>.CreateSuccess(...)`).
  - Data Type: `RefreshTokenResponse` wrapped in `ApiResponse<RefreshTokenResponse>`.
- Failure
  - `MissingCredentials` (no token) -> HTTP 400.
  - `InvalidRefreshToken` or `UserNotFound` -> HTTP 401.
  - `TokenGenerationFailed` / `InternalServerError` -> HTTP 500.

**Authentication**
- Anonymous

---

#### Endpoint: `Logout`
- **Method**: `POST`
- **Route**: `/api/v1/auth/logout`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `LogoutRequest` (optional; may contain `refreshToken`)
  - Example:
    ```json
    {
      "refreshToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
    ```

**Response**
- Success
  - ApiResponseCode: conceptually `SUCCESS`.
  - Data Type: `LogoutResponse` wrapped in `ApiResponse<LogoutResponse>` with messages like `"Logout successful"`.
- Failure
  - Internal errors -> HTTP 500 with `AuthErrorCodes.InternalServerError`.

**Authentication**
- Authorized (`[Authorize]` – valid access token required)

---

### 3.2 `Internal/InternalUserController`

- **Base Route**: `/internal`
- **Auth**: `[AllowAnonymous]` (intended to be protected by network / API key in production)

#### Endpoint: `RegisterUser`
- **Method**: `POST`
- **Route**: `/internal/users/register`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `InternalRegisterUserRequest`
  - Example:
    ```json
    {
      "email": "internal.user@example.com",
      "password": "P@ssw0rd!",
      "role": "SUPER_ADMIN"
    }
    ```

**Response**
- Success
  - ApiResponseCode: not using standardized envelope; returns anonymous object.
  - Data Type: `{ userId, email, platformId }`
- Failure
  - HTTP 400 with `{ error: "Email and password are required" }` or validation errors from Identity.
  - HTTP 409 with `{ error: "User with this email already exists." }`.
  - HTTP 500 with `{ error, details, type }` for unhandled exceptions.

**Authentication**
- Anonymous (internal-only channel)

---

#### Endpoint: `DeleteUser`
- **Method**: `DELETE`
- **Route**: `/internal/users/{id}`

**Request**
- Route Parameters
  - `id` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: conceptually `SUCCESS`.
  - Data Type: none (`204 NoContent` on success).
- Failure
  - HTTP 404 `{ error: "User not found" }`.
  - HTTP 400 `{ error: "Failed to delete user.", details: [...] }`.
  - HTTP 500 `{ error: "Failed to delete user" }` on exception.

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `GenerateToken`
- **Method**: `POST`
- **Route**: `/internal/token/generate`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `GenerateTokenRequest`
  - Example:
    ```json
    {
      "userId": "00000000-0000-0000-0000-000000000001",
      "email": "user@example.com",
      "role": "Admin",
      "organizationId": "00000000-0000-0000-0000-000000000010",
      "hospitalId": "00000000-0000-0000-0000-000000000020"
    }
    ```

**Response**
- Success
  - ApiResponseCode: not standardized; returns anonymous object with tokens.
  - Data Type:
    ```json
    {
      "token": "<access_token>",
      "refreshToken": "<refresh_token>",
      "expiresIn": 3600
    }
    ```
- Failure
  - HTTP 400 `{ error: "UserId and Email are required" }`.
  - HTTP 404 `{ error: "User not found" }`.
  - HTTP 500 `{ error: "Failed to generate authentication token." }` or generic generation failure.

**Authentication**
- Anonymous (internal)

---

### 3.3 `TokenController`

- **Base Route**: `/api/token`
- **Auth**: `[Authorize]` (any authenticated user)

#### Endpoint: `Me`
- **Method**: `GET`
- **Route**: `/api/token/me`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: not wrapped; raw collection of claims.
  - Data Type: `IEnumerable<{ Type, Value }>`
- Failure
  - Auth failures handled by ASP.NET and middleware (401/403).

**Authentication**
- Authorized

---

### 3.4 `WeatherForecastController` (IdentityService)

- **Base Route**: `/WeatherForecast`
- **Auth**: none (demo/test-only controller)

#### Endpoint: `Get`
- **Method**: `GET`
- **Route**: `/WeatherForecast`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: none (legacy sample).
  - Data Type: `IEnumerable<WeatherForecast>` with simple weather data.
- Failure
  - Not explicitly mapped.

**Authentication**
- Anonymous

---

## 4. AuditService

### 4.1 `V1/AuditController`

- **Base Route**: `/api/v1/audit`
- **Auth**: `[Authorize(Policy = "AuditScope")]`

#### Endpoint: `Generate`
- **Method**: `POST`
- **Route**: `/api/v1/audit/generate`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `GenerateAuditRequest`
  - Example:
    ```json
    {
      "organizationId": "00000000-0000-0000-0000-000000000001",
      "complianceProgramId": "00000000-0000-0000-0000-000000000010"
    }
    ```

**Response**
- Success
  - ApiResponseCode: not standardized; returns audit session entity/DTO directly.
  - Data Type: `AuditSession` (or equivalent audit session DTO) from `AuditAppService.GenerateAuditAsync`.
- Failure
  - HTTP 400 with message `"OrganizationId and ComplianceProgramId are required."` when either GUID is empty.
  - Other failures surfaced as 500 via middleware.

**Authentication**
- Authorized (`AuditScope`)

---

#### Endpoint: `Get`
- **Method**: `GET`
- **Route**: `/api/v1/audit/{sessionId}`

**Request**
- Route Parameters
  - `sessionId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: none (legacy).
  - Data Type: audit session DTO/entity.
- Failure
  - HTTP 400 with `"SessionId is required."` when empty.
  - HTTP 404 with `"Audit session with ID {sessionId} not found."` when missing.

**Authentication**
- Authorized (`AuditScope`)

---

#### Endpoint: `GetByOrganization`
- **Method**: `GET`
- **Route**: `/api/v1/audit/organizations/{organizationId}`

**Request**
- Route Parameters
  - `organizationId` : `Guid`
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: none (legacy).
  - Data Type: list of audit sessions retrieved by organization.
- Failure
  - HTTP 400 with `"OrganizationId is required."` when empty.

**Authentication**
- Authorized (`AuditScope`)

---

### 4.2 `WeatherForecastController` (AuditService)

- **Base Route**: `/WeatherForecast`
- **Auth**: none (sample)

#### Endpoint: `Get`
- **Method**: `GET`
- **Route**: `/WeatherForecast`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body: none

**Response**
- Success
  - ApiResponseCode: none (sample).
  - Data Type: `IEnumerable<WeatherForecast>` with simple weather data.
- Failure
  - Not explicitly mapped.

**Authentication**
- Anonymous

---

#### Endpoint: `Refresh`
- **Method**: `POST`
- **Route**: `/api/v1/identity/refresh`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `object` (refresh token payload)

**Response**
- Success
  - ApiResponseCode: token refresh code from IdentityService.
  - Data Type: new token DTO.

**Authentication**
- Anonymous

---

#### Endpoint: `Signup`
- **Method**: `POST`
- **Route**: `/api/v1/identity/register`

**Request**
- Route Parameters: none
- Query Parameters: none
- Body
  - DTO Type: `SignupRequestDto`

**Response**
- Success
  - ApiResponseCode: not standardized; returns `SignupResponseDto` directly via `Ok(response)`.
- Failure
  - HTTP 409 with `{ error = ex.Message }` when resource already exists.
  - HTTP 400 with `{ error = ex.Message }` for other known `InvalidOperationException`.
  - HTTP 500 with `{ error = "An unexpected error occurred during signup. Please try again later." }` for unhandled exceptions.

**Authentication**
- Anonymous

---

#### Endpoint: `CreateHospital`
- **Method**: `POST`
- **Route**: `/internal/hospitals`

**Request**
- Body
  - DTO Type: `CreateHospitalDto`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ id, name, code }`
- Failure
  - `VALIDATION_FAILED` – invalid operation / business-rule
  - `INTERNAL_ERROR` – unexpected error

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `DeleteHospital`
- **Method**: `DELETE`
- **Route**: `/internal/hospitals/{id}`

**Request**
- Route Parameters
  - `id` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS` (204 NoContent)
- Failure
  - `RESOURCE_NOT_FOUND` – hospital not found
  - `INTERNAL_ERROR` – unexpected error

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `CreateOrganizationMember`
- **Method**: `POST`
- **Route**: `/internal/organization-members`

**Request**
- Body
  - DTO Type: `CreateOrganizationMemberDto`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ id, userId, organizationId, role }`
- Failure
  - `VALIDATION_FAILED` – conflict or rule error
  - `INTERNAL_ERROR` – unexpected errors

**Authentication**
- Anonymous (internal)

---

#### Endpoint: `GetUserOrganizationAndHospital`
- **Method**: `GET`
- **Route**: `/internal/users/{userId}/organization-hospital`

**Request**
- Route Parameters
  - `userId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ organizationId, hospitalId }` (both `Guid.Empty` if no membership found)
- Failure
  - `INTERNAL_ERROR` – unexpected errors when querying DB

**Authentication**
- Anonymous (internal)

---

## 2. ComplianceService

> All controllers in this service use the standardized `ApiResponse<T>` envelope and `ApiResponseFactory` + `ApiResponseCodes.Registry` for success and known failures. Below are the primary controllers and their endpoints.

### 2.1 `V1/JourneysController`

- **Base Route**: `/api/v1/compliance/journeys`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### CreateJourney
- **Method**: `POST`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/journeys`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
  - `frameworkId` : `Guid`
- Body
  - none (journey is derived from hospital + framework + org context)

**Response**
- Success
  - ApiResponseCode: `JOURNEY_INITIALIZED`
  - Data Type: anonymous `{ JourneyId, InitialStatus }`
- Failure
  - `HOSPITAL_ID_REQUIRED`, `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetJourneyByHospitalAndFramework
- **Method**: `GET`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/journey`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
  - `frameworkId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `JourneyDetailsDto`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `FORBIDDEN`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetJourneyDetails
- **Method**: `GET`
- **Route**: `/api/v1/compliance/journeys/{journeyId}`

**Request**
- Route Parameters
  - `journeyId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `JourneyDetailsDto`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `FORBIDDEN`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetValidTransitions
- **Method**: `GET`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/valid-transitions`

**Request**
- Route Parameters
  - `journeyId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `List<ValidTransitionDto>`
- Failure
  - `VALIDATION_FAILED`, `UNAUTHORIZED`, `ORGANIZATION_CONTEXT_MISSING`, `FORBIDDEN`, `RESOURCE_NOT_FOUND`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### TransitionJourney
- **Method**: `POST`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/transition` and `/transitions`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Body
  - DTO Type: `TransitionJourneyRequest` (includes `TargetStatus`, `Remarks`)

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ JourneyId, TargetStatus }`
- Failure
  - `VALIDATION_FAILED`, `UNAUTHORIZED`, `ORGANIZATION_CONTEXT_MISSING`, `FORBIDDEN`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetJourneyChapters
- **Method**: `GET`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/chapters`

**Request**
- Route Parameters
  - `journeyId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `List<JourneyChapterDto>`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `FORBIDDEN`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetNonConformances
- **Method**: `GET`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/non-conformances`

**Request**
- Route Parameters
  - `journeyId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `NonConformanceSummaryDto`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `FORBIDDEN`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetJourneyHistory
- **Method**: `GET`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/history`

**Request**
- Route Parameters
  - `journeyId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `List<JourneyHistoryItemDto>`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `FORBIDDEN`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetTransitionAuditReport (`AuditController` region)
- **Method**: `GET`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/transition-audit`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
  - `frameworkId` : `Guid`
- Query Parameters
  - `fromDate`, `toDate` : `DateTime?`
  - `state` : `string?`
  - `user` : `Guid?`
  - `role` : `string?`
  - `pageNumber` : `int` (default 1)
  - `pageSize` : `int` (default 20)

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `PaginatedTransitionAuditDto`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetJourneySummary (`AuditController` region)
- **Method**: `GET`
- **Route**: `/api/v1/hospitals/{hospitalId}/journey-summary`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
- Query Parameters
  - `frameworkId` : `Guid?`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `JourneySummaryDto`
- Failure
  - `HOSPITAL_ID_REQUIRED`, `ORGANIZATION_CONTEXT_MISSING`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

> Additional `JourneysController` endpoints (compliance and regulatory overviews, framework catalog, accreditations, renewals) follow the same pattern and can be expanded similarly.

### 2.2 `V1/EvidenceController`

- **Base Route**: `/api/v1/compliance`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### Create
- **Method**: `POST`
- **Route**: `/api/v1/compliance/tasks/{taskId}/evidence`

**Request**
- Route Parameters
  - `taskId` : `Guid`
- Body
  - DTO Type: `CreateEvidenceRequest`

**Response**
- Success
  - ApiResponseCode: `EVIDENCE_UPLOADED`
  - Data Type: anonymous `{ EvidenceId }`
- Failure
  - `RESOURCE_NOT_FOUND` – task not found
  - `VALIDATION_FAILED` (via exception middleware when evidence is locked)

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.3 `V1/ReportingController`

- **Base Route**: `/api/v1/reports`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### GetComplianceScoreReport
- **Method**: `GET`
- **Route**: `/api/v1/reports/compliance-score`

**Request**
- Query Parameters
  - `hospitalId` : `Guid` (required)
  - `frameworkId` : `Guid?`
  - `from`, `to` : `DateTime?`

**Response**
- Success
  - ApiResponseCode: `COMPLIANCE_SCORE_UPDATED`
  - Data Type: `ComplianceScoreReportDto`
- Failure
  - `VALIDATION_FAILED` – empty `hospitalId`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetAuditFindingsReport
- **Method**: `GET`
- **Route**: `/api/v1/reports/audit-findings`

**Request**
- Query Parameters
  - `hospitalId` : `Guid` (required)
  - `frameworkId` : `Guid?`
  - `from`, `to` : `DateTime?`

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED`
  - Data Type: `AuditFindingsReportDto`
- Failure
  - `VALIDATION_FAILED` – empty `hospitalId`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetNabhReadinessReport
- **Method**: `GET`
- **Route**: `/api/v1/reports/nabh-readiness`

**Request**
- Query Parameters
  - `hospitalId` : `Guid` (required)

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED`
  - Data Type: `NabhReadinessReportDto`
- Failure
  - `VALIDATION_FAILED` – empty `hospitalId`

**Authentication**
- Authorized (`ComplianceScope`)

---

> Similar endpoint sections can be added for `DashboardController`, `AuditLogsController`, `AuditFindingsController`, `TasksController`, `ProgramsController`, `ControlsController`, `SnapshotsController`, `FrameworkController`, and `Internal/InternalJourneysController` following these patterns.

### 2.4 `V1/DashboardController`

- **Base Route**: `/api/v1/dashboard`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### GetSummary
- **Method**: `GET`
- **Route**: `/api/v1/dashboard/summary`

**Request**
- Query Parameters
  - `hospitalId` : `Guid` (required)

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED`
  - Data Type: `DashboardSummaryDto`
- Failure
  - `HOSPITAL_ID_REQUIRED`, `ORGANIZATION_CONTEXT_MISSING`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetHospitalDashboard
- **Method**: `GET`
- **Route**: `/api/v1/dashboard/hospital`

**Request**
- Query Parameters
  - `hospitalId` : `Guid` (required)

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED`
  - Data Type: `HospitalDashboardDto`
- Failure
  - `HOSPITAL_ID_REQUIRED`, `ORGANIZATION_CONTEXT_MISSING`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetComplianceTrend
- **Method**: `GET`
- **Route**: `/api/v1/dashboard/compliance-trend`

**Request**
- Query Parameters
  - `hospitalId` : `Guid` (required)
  - `from` : `DateTime?`
  - `to` : `DateTime?`

**Response**
- Success
  - ApiResponseCode: `REPORT_GENERATED`
  - Data Type: `ComplianceTrendDto`
- Failure
  - `HOSPITAL_ID_REQUIRED`, `ORGANIZATION_CONTEXT_MISSING`

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.5 `V1/AuditLogsController`

- **Base Route**: `/api/v1/audit-logs`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### GetAuditLogs
- **Method**: `GET`
- **Route**: `/api/v1/audit-logs`

**Request**
- Query Parameters
  - `entityType` : `string?`
  - `entityId` : `Guid?`
  - `from` : `DateTime?`
  - `to` : `DateTime?`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `List<AuditLog>`
- Failure
  - `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetAuditLogsForEntity
- **Method**: `GET`
- **Route**: `/api/v1/audit-logs/{entityType}/{entityId}`

**Request**
- Route Parameters
  - `entityType` : `string`
  - `entityId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `List<AuditLog>`
- Failure
  - `VALIDATION_FAILED`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.6 `V1/AuditFindingsController`

- **Base Route**: `/api/v1/compliance/journeys`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### GetAuditFindings
- **Method**: `GET`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/audit-findings`

**Request**
- Route Parameters
  - `journeyId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `List<AuditFindingDto>`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### CreateAuditFinding
- **Method**: `POST`
- **Route**: `/api/v1/compliance/journeys/{journeyId}/audit-findings`

**Request**
- Route Parameters
  - `journeyId` : `Guid`
- Body
  - DTO Type: `CreateAuditFindingRequest`
  - Example:
    ```json
    {
      "clauseId": "00000000-0000-0000-0000-000000000001",
      "type": "MajorNC",
      "description": "Major non-conformance in documentation",
      "dueDate": "2026-06-30T00:00:00Z"
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `AuditFindingDto`
- Failure
  - `VALIDATION_FAILED`, `ORGANIZATION_CONTEXT_MISSING`, `RESOURCE_NOT_FOUND`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.7 `V1/TasksController`

- **Base Route**: `/api/v1/compliance`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`, `[ValidateHospitalId]`

#### Assign
- **Method**: `POST`
- **Route**: `/api/v1/compliance/programs/{programId}/tasks`

**Request**
- Route Parameters
  - `programId` : `Guid`
- Body
  - DTO Type: `AssignTasksRequest`
  - Example:
    ```json
    {
      "hospitalId": "00000000-0000-0000-0000-000000000001"
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ Count }`
- Failure
  - `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetByProgramId
- **Method**: `GET`
- **Route**: `/api/v1/compliance/programs/{programId}/tasks`

**Request**
- Route Parameters
  - `programId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `IReadOnlyList<ComplianceTask>`
- Failure
  - `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### CompleteTask
- **Method**: `PUT`
- **Route**: `/api/v1/compliance/frameworks/{frameworkId}/tasks/{taskId}/complete`

**Request**
- Route Parameters
  - `frameworkId` : `Guid`
  - `taskId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ TaskId, FrameworkId, HospitalId }`
- Failure
  - `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.8 `V1/ProgramsController`

- **Base Route**: `/api/v1/compliance/programs`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`, `[ValidateHospitalId]`

#### Create
- **Method**: `POST`
- **Route**: `/api/v1/compliance/programs`

**Request**
- Body
  - DTO Type: `CreateProgramRequest`
  - Example:
    ```json
    {
      "regulation": "NABH"
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ ProgramId }`
- Failure
  - `VALIDATION_FAILED`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### Get
- **Method**: `GET`
- **Route**: `/api/v1/compliance/programs/{programId}`

**Request**
- Route Parameters
  - `programId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `ComplianceProgram`
- Failure
  - `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetComplianceData
- **Method**: `GET`
- **Route**: `/api/v1/compliance/programs`

**Request**
- Query Parameters: none

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ Message, UserId, Email, HospitalId, Role, Timestamp }`
- Failure
  - Not explicitly mapped.

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.9 `V1/ControlsController`

- **Base Route**: `/api/v1/compliance`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`, `[ValidateHospitalId]`

#### Load
- **Method**: `POST`
- **Route**: `/api/v1/compliance/programs/{programId}/controls`

**Request**
- Route Parameters
  - `programId` : `Guid`
- Body
  - DTO Type: `LoadControlsRequest`
  - Example:
    ```json
    {
      "regulation": "NABH"
    }
    ```

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: anonymous `{ Count }`
- Failure
  - `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetByProgramId
- **Method**: `GET`
- **Route**: `/api/v1/compliance/programs/{programId}/controls`

**Request**
- Route Parameters
  - `programId` : `Guid`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `IReadOnlyList<Control>`
- Failure
  - `VALIDATION_FAILED`, `RESOURCE_NOT_FOUND`

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.10 `V1/SnapshotsController`

- **Base Route**: `/api/v1/compliance/snapshots`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`, `[ValidateHospitalId]`

#### GetSnapshotByHospitalId
- **Method**: `GET`
- **Route**: `/api/v1/compliance/snapshots/hospital/{hospitalId}`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
- Query Parameters
  - `frameworkId` : `Guid?`

**Response**
- Success
  - ApiResponseCode: `SUCCESS`
  - Data Type: `DashboardSnapshotDto`
- Failure
  - `HOSPITAL_ID_REQUIRED`, `RESOURCE_NOT_FOUND`, `INTERNAL_ERROR`

**Authentication**
- Authorized (`ComplianceScope`)

---

### 2.11 `V1/RegulationsController` (Deprecated)

- **Base Route**: `/api/v1/regulations`
- **Auth**: none (deprecated internal usage)

#### Enroll
- **Method**: `POST`
- **Route**: `/api/v1/regulations/enroll`

**Request**
- Body
  - DTO Type: `EnrollRegulationRequest`

**Response**
- Success
  - Data Type: anonymous `{ RegulationId }` (legacy pattern; not standardized)

**Authentication**
- None (deprecated)

---

### 2.12 `V1/FrameworkController`

- **Base Route**: `/api/v1/frameworks`
- **Auth**: `[Authorize(Policy = AuthConstants.Policies.ComplianceScope)]`

#### GetAllFrameworks
- **Method**: `GET`
- **Route**: `/api/v1/frameworks`

**Request**
- Query Parameters: none

**Response**
- Success
  - Data Type: `List<FrameworkDto>` (wrapped via controller extensions into `ApiResponse`). Each `FrameworkDto`: `frameworkId`, `name`, `authority` (issuing/accreditation body), `code`, `validityPeriodMonths`, `isActive`.

**Authentication**
- Authorized (`ComplianceScope`)

---

#### GetHospitalFrameworks
- **Method**: `GET`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`

**Response**
- Success
  - Data Type: `List<HospitalFrameworkDto>`
- Failure
  - Validation errors mapped via `BadRequestResponse` extension

**Authentication**
- Authorized (`ComplianceScope`)

---

#### EnrollHospitalInFramework
- **Method**: `POST`
- **Route**: `/api/v1/hospitals/{hospitalId}/frameworks/{frameworkId}/enroll`

**Request**
- Route Parameters
  - `hospitalId` : `Guid`
  - `frameworkId` : `Guid`

**Response**
- Success
  - Location header: `/api/v{version}/hospitals/{hospitalId}/frameworks/{frameworkId}`
  - Data Type: `EnrollFrameworkResponseDto`
- Failure
  - Validation and conflict errors mapped via controller extensions (`BadRequestResponse`, `ConflictResponse`, `UnauthorizedResponse`)

**Authentication**
- Authorized (`ComplianceScope`)

---

## 3. Gateway, IdentityService, AuditService (Overview)

### 3.1 Gateway

Gateway controllers under `Gateway/Controllers` (e.g. `AIController`, `SubscriptionsController`, `DashboardController`, `JourneysController`, `ReportingController`, `AuditLogsController`, `AuditFindingsController`, `OnboardingController`, `FrameworkController`, `TenantController`, `IdentityController`, `CommonController`) act primarily as BFF/proxy layers:

- They receive HTTP requests from clients.
- Authorize via `[Authorize]` attributes with policies (e.g. `ComplianceScope`).
- Use MediatR + `IProxyService` to forward to backend services.
- For standardized endpoints, they either:
  - Pass through `ApiResponse<T>` from downstream, or
  - Wrap responses using the same `ApiResponseFactory` pattern.

When expanding this document, list each Gateway controller and its endpoints in the same template as Section 1, noting:
- Upstream route (`/api/v1/...`),
- Downstream service/route it calls (if known),
- Expected `ApiResponseCode` surface (often `SUCCESS` + domain codes from backend). 

### 3.2 IdentityService

IdentityService controllers (`V1/AuthController`, `TokenController`, internal controllers) provide:
- Login/token issuance,
- User/profile retrieval,
- Internal user management.

These should be cataloged similarly, with attention to:
- Auth flows (`POST /api/v1/auth/login`, etc.),
- Token endpoints,
- Any standardized `ApiResponse<T>` usage or legacy patterns to be upgraded.

### 3.3 AuditService

AuditService controllers (`V1/AuditController`, `WeatherForecastController`) support:
- Compliance audit reporting/diagnostics,
- Possibly non-production/sample endpoints.

When completing this document, focus on the production-facing endpoints and annotate sample controllers separately.

---

## 4. Summary / Next Steps

- **Controllers scanned in detail here**: All OrganizationService controllers.
- **Controllers summarized for later expansion**: All major ComplianceService and Gateway controllers, IdentityService, and AuditService.
- **Standardization status**:
  - OrganizationService: all active controllers now use `ApiResponseFactory` for success and, for internal controllers, for main error paths as well.
  - ComplianceService & Gateway: most controllers already use `ApiResponseFactory` + `ApiResponseCodes`. Remaining legacy patterns should be gradually brought in line and documented.

**Next steps to complete this inventory:**
1. For each remaining ComplianceService controller, add a section like **1.1–1.4**, listing all `[Http*]` actions and their request/response/auth details.
2. Do the same for each Gateway controller, IdentityService controller, and AuditService controller.
3. Use this document as the single source of truth for generating Postman collections and automated tests.


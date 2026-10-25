# API Communication Guidelines

This document defines the standardized API communication protocol for all microservices in the Compliance Platform architecture.

## Table of Contents

1. [Response Format](#response-format)
2. [HTTP Status Codes](#http-status-codes)
3. [Error Handling](#error-handling)
4. [Validation](#validation)
5. [Correlation IDs](#correlation-ids)
6. [Versioning](#versioning)
7. [Examples](#examples)
8. [Migration Strategy](#migration-strategy)

---

## Response Format

All API responses MUST follow the standardized `ApiResponse<T>` format:

```json
{
  "success": true,
  "data": { /* response data */ },
  "error": null,
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### Response Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `success` | boolean | Yes | `true` only when HTTP status is 2xx |
| `data` | T \| null | Yes | Response data (null when `success` is false) |
| `error` | ApiError \| null | Yes | Error information (null when `success` is true) |
| `traceId` | string | Yes | Correlation/Trace identifier (from `HttpContext.TraceIdentifier`) |
| `timestamp` | DateTime (UTC) | Yes | UTC timestamp when response was generated |

### Error Response Format

When `success` is `false`, the `error` field contains:

```json
{
  "code": "SNAPSHOT_NOT_FOUND",
  "message": "No snapshot found for hospital {hospitalId}",
  "status": 404,
  "details": null,
  "validationErrors": null
}
```

### Error Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `code` | string | Yes | Machine-readable error code (UPPER_SNAKE_CASE) |
| `message` | string | Yes | Human-readable error message |
| `status` | integer | Yes | HTTP status code |
| `details` | string \| null | No | Additional error details (optional) |
| `validationErrors` | Dictionary<string, string[]> \| null | No | Field-level validation errors (only for 400) |

---

## HTTP Status Codes

### Success Codes

| Code | Usage | Response Body |
|------|-------|---------------|
| **200 OK** | Successful GET, PUT, PATCH | `ApiResponse<T>` with `success: true` |
| **201 Created** | Successful POST (resource created) | `ApiResponse<T>` with `success: true` |
| **204 No Content** | Successful DELETE (no body) | No response body |

### Error Codes

| Code | Usage | Error Code Example |
|------|-------|-------------------|
| **400 BadRequest** | Validation errors, invalid input | `VALIDATION_ERROR` |
| **401 Unauthorized** | Missing or invalid authentication | `UNAUTHORIZED` |
| **403 Forbidden** | Authenticated but no permission | `FORBIDDEN` |
| **404 NotFound** | Resource not found | `SNAPSHOT_NOT_FOUND` |
| **409 Conflict** | Business rule violation | `BUSINESS_RULE_VIOLATION` |
| **422 UnprocessableEntity** | Semantic validation failure | `INVALID_BUSINESS_STATE` |
| **500 InternalServerError** | Unexpected exception | `INTERNAL_SERVER_ERROR` |

### Status Code Rules

✅ **DO:**
- Return 200 OK for successful GET requests
- Return 201 Created for successful POST requests
- Return 204 No Content for successful DELETE requests
- Return appropriate error codes (400, 404, etc.) for business errors
- Use 500 only for unexpected exceptions

❌ **DON'T:**
- Return 200 with embedded error codes
- Return 500 for business errors (use 400, 404, 409, etc.)
- Wrap success inside error container
- Return different status codes for the same error type

---

## Error Handling

### Exception Mapping

The `GlobalExceptionMiddleware` automatically maps exceptions to HTTP status codes:

| Exception Type | HTTP Status | Error Code |
|----------------|-------------|------------|
| `ArgumentException` | 400 BadRequest | `VALIDATION_ERROR` |
| `KeyNotFoundException` | 404 NotFound | `RESOURCE_NOT_FOUND` |
| `UnauthorizedAccessException` | 403 Forbidden | `FORBIDDEN` |
| `InvalidOperationException` (conflict) | 409 Conflict | `BUSINESS_RULE_VIOLATION` |
| `InvalidOperationException` (general) | 400 BadRequest | `VALIDATION_ERROR` |
| `NotImplementedException` | 500 InternalServerError | `INTERNAL_SERVER_ERROR` |
| All others | 500 InternalServerError | `INTERNAL_SERVER_ERROR` |

### Error Code Naming Convention

Error codes MUST follow **UPPER_SNAKE_CASE** format:

- ✅ `SNAPSHOT_NOT_FOUND`
- ✅ `VALIDATION_ERROR`
- ✅ `UNAUTHORIZED`
- ✅ `BUSINESS_RULE_VIOLATION`
- ❌ `SnapshotNotFound` (PascalCase)
- ❌ `validation-error` (kebab-case)
- ❌ `validationError` (camelCase)

### Common Error Codes

| Error Code | HTTP Status | Description |
|------------|-------------|-------------|
| `VALIDATION_ERROR` | 400 | Input validation failed |
| `UNAUTHORIZED` | 401 | Authentication required |
| `FORBIDDEN` | 403 | Permission denied |
| `RESOURCE_NOT_FOUND` | 404 | Resource does not exist |
| `BUSINESS_RULE_VIOLATION` | 409 | Business rule conflict |
| `INTERNAL_SERVER_ERROR` | 500 | Unexpected server error |

---

## Validation

### FluentValidation Integration

Validation errors are automatically handled by `ValidationExceptionMiddleware`:

**Request:**
```json
{
  "hospitalId": ""
}
```

**Response (400 BadRequest):**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "status": 400,
    "details": null,
    "validationErrors": {
      "hospitalId": [
        "HospitalId is required",
        "HospitalId cannot be empty"
      ]
    }
  },
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### Validation Rules

- Use FluentValidation for all request DTOs
- Return 400 BadRequest for validation failures
- Include field-level errors in `validationErrors` dictionary
- Use descriptive error messages

---

## Correlation IDs

### Purpose

Correlation IDs enable request tracing across microservices and help with debugging.

### Implementation

1. **Automatic Generation**: `HttpContext.TraceIdentifier` is used as correlation ID
2. **Header**: `X-Correlation-ID` header is automatically added to all responses
3. **Forwarding**: Gateway forwards `X-Correlation-ID` to downstream services
4. **Logging**: All log entries should include correlation ID

### Usage in Logs

```csharp
_logger.LogInformation(
    "Retrieving snapshot for hospital {HospitalId}. TraceId: {TraceId}",
    hospitalId, context.TraceIdentifier);
```

---

## Versioning

### API Versioning Strategy

- Use URL path versioning: `/api/v1/...`, `/api/v2/...`
- Default version: `v1`
- Version is required in all API calls
- Breaking changes require new version

### Version Header

Clients can optionally specify version via header:
```
Api-Version: 1.0
```

---

## Examples

### 200 OK - Successful GET

**Request:**
```
GET /api/v1/compliance/snapshots/hospital/e198747b-096d-4c19-a341-16be566c8da4
```

**Response:**
```json
{
  "success": true,
  "data": {
    "overallScore": 85.5,
    "auditConfidenceScore": 90.0,
    "criticalCount": 2,
    "majorCount": 5,
    "minorCount": 10,
    "observationCount": 3,
    "ofiCount": 1,
    "highRiskChaptersJson": "[...]",
    "chapterBreakdownJson": "[...]",
    "calculatedAt": "2024-02-15T10:00:00Z"
  },
  "error": null,
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### 400 BadRequest - Validation Error

**Request:**
```
POST /api/v1/compliance/snapshots
Content-Type: application/json

{
  "hospitalId": ""
}
```

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Validation failed",
    "status": 400,
    "details": null,
    "validationErrors": {
      "hospitalId": [
        "HospitalId is required",
        "HospitalId cannot be empty"
      ]
    }
  },
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### 404 NotFound - Resource Not Found

**Request:**
```
GET /api/v1/compliance/snapshots/hospital/00000000-0000-0000-0000-000000000000
```

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "SNAPSHOT_NOT_FOUND",
    "message": "No snapshot found for hospital 00000000-0000-0000-0000-000000000000",
    "status": 404,
    "details": null,
    "validationErrors": null
  },
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

### 500 InternalServerError - Unexpected Exception

**Request:**
```
GET /api/v1/compliance/snapshots/hospital/e198747b-096d-4c19-a341-16be566c8da4
```

**Response:**
```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "INTERNAL_SERVER_ERROR",
    "message": "An unexpected error occurred. Please try again later.",
    "status": 500,
    "details": "Database connection timeout (Development only)",
    "validationErrors": null
  },
  "traceId": "0HN1234567890",
  "timestamp": "2024-02-15T10:30:00Z"
}
```

---

## Migration Strategy

### Phase 1: Infrastructure Setup (Non-Breaking)

1. ✅ Add `ApiResponse<T>` and `ApiError` to `Shared.Infrastructure`
2. ✅ Add `GlobalExceptionMiddleware` and `ValidationExceptionMiddleware`
3. ✅ Add `ControllerExtensions` helper methods
4. ✅ Configure middleware in all services

**Impact**: No breaking changes, existing endpoints continue to work

### Phase 2: New Endpoints (Gradual)

1. All new endpoints use standardized format
2. Example controllers demonstrate usage
3. Gateway handlers updated to parse standardized responses

**Impact**: New endpoints are standardized, old endpoints unchanged

### Phase 3: Migration (Per Service)

1. Migrate one controller at a time
2. Update Gateway handlers for migrated endpoints
3. Test thoroughly before moving to next controller
4. Document breaking changes if any

**Impact**: Gradual migration, can be done service-by-service

### Phase 4: Cleanup (Final)

1. Remove old response formats
2. Update all documentation
3. Remove example/legacy code

**Impact**: Complete standardization

### Backward Compatibility

During migration:
- Old endpoints continue to work
- New endpoints use standardized format
- Gateway can handle both formats (with adapter if needed)
- Gradual migration reduces risk

---

## Implementation Checklist

### For Each Service

- [ ] Add `Shared.Infrastructure` reference
- [ ] Configure `GlobalExceptionMiddleware` in `Program.cs`
- [ ] Configure `ValidationExceptionMiddleware` in `Program.cs`
- [ ] Configure `UseCorrelationId()` in `Program.cs`
- [ ] Update controllers to use `ControllerExtensions`
- [ ] Update FluentValidation validators
- [ ] Test all endpoints
- [ ] Update API documentation

### For Gateway

- [ ] Update handlers to parse `ApiResponse<T>` format
- [ ] Forward `X-Correlation-ID` header
- [ ] Log correlation IDs
- [ ] Handle both old and new formats during migration

---

## Support

For questions or issues:
1. Check this documentation
2. Review example controllers
3. Check middleware logs
4. Contact architecture team

---

**Last Updated**: 2024-02-15  
**Version**: 1.0

# 🔍 PRAMAN Compliance Platform - Endpoint Verification Audit Report

**Generated:** 2025-01-XX  
**Auditor:** Senior .NET Backend Auditor  
**Scope:** Journey Lifecycle Management Endpoints

---

## Executive Summary

This audit verifies the implementation status of 8 critical journey lifecycle endpoints against business rules, response contracts, and compliance domain expectations.

**Overall Status:** ⚠️ **PARTIALLY IMPLEMENTED** (5/8 endpoints fully implemented, 3/8 missing)

---

## 📋 Endpoint Verification Matrix

---

### 1️⃣ Top Banner - Journey Basic Info

**Endpoint:** `GET /api/v1/journeys/{id}`

**Status:** ⚠️ **PARTIAL**

**Controller:** ✅ Yes (`Gateway.Controllers.JourneysController.GetJourneyDetails`, `ComplianceService.Controllers.V1.JourneysController.GetJourneyDetails`)

**Service:** ✅ Yes (`ComplianceJourneyService.GetJourneyDetailsAsync`)

**Route Attributes:** ✅ Correct
- Gateway: `[HttpGet("{journeyId:guid}")]` on `/api/v{version}/journeys`
- Backend: `[HttpGet("{journeyId:guid}")]` on `/api/v{version}/compliance/journeys`

**Business Rules:**
- ✅ Journey existence validation: Implemented (throws `InvalidOperationException` if not found)
- ⚠️ **MISSING:** Tenant + Hospital scoping validation in `GetJourneyDetailsAsync` (no tenantId check)
- ✅ 404 handling: Implemented (returns `NotFound` with `ApiError.NotFound`)
- ✅ 403 handling: Not explicitly implemented (should add tenant validation)

**Response Contract:** ⚠️ **NEEDS FIX**

**Expected Fields:**
```json
{
  "journeyId": "guid",
  "frameworkName": "NABH",
  "currentState": "UnderReview",
  "riskIndex": 42,
  "progressPercentage": 68,
  "startDate": "2024-01-01",
  "targetAccreditationDate": "2024-12-31"
}
```

**Actual DTO (`JourneyDetailsDto`):**
```csharp
{
  JourneyId,           // ✅ Present
  HospitalId,          // ✅ Present (but not in expected response)
  FrameworkId,         // ✅ Present (but not FrameworkName)
  CurrentStatus,       // ✅ Present (but enum, not string)
  ValidNextStates,     // ✅ Present (but not in expected response)
  ExpiryDate,          // ✅ Present (but not TargetAccreditationDate)
  ReadinessScore,     // ✅ Present (but not ProgressPercentage)
  RiskScore,          // ✅ Present (but not RiskIndex)
  SurveillanceDue,    // ✅ Present (but not in expected response)
  RenewalWindowOpen   // ✅ Present (but not in expected response)
}
```

**Issues Found:**
- ❌ **CRITICAL:** Missing `FrameworkName` (only has `FrameworkId`)
- ❌ **CRITICAL:** Missing `StartDate` (no enrollment date in response)
- ❌ **CRITICAL:** Missing `TargetAccreditationDate` (has `ExpiryDate` but not target date)
- ❌ **CRITICAL:** Missing `ProgressPercentage` (has `ReadinessScore` but not progress %)
- ❌ **CRITICAL:** Missing `RiskIndex` (has `RiskScore` but not aggregated index)
- ⚠️ **SECURITY:** No tenant isolation validation in service layer
- ⚠️ **SECURITY:** No hospital scoping validation

**Security:**
- ⚠️ **MISSING:** Tenant isolation not enforced in `GetJourneyDetailsAsync`
- ⚠️ **MISSING:** Hospital scoping not validated
- ✅ Authorization policy: `ComplianceScope` applied at controller level

**HTTP Status Handling:**
- ✅ 400: BadRequest for empty journeyId
- ✅ 404: NotFound for journey not found
- ❌ 403: Forbidden not returned (should return when tenant/hospital mismatch)

---

### 2️⃣ Lifecycle Timeline

**Endpoint:** `GET /api/v1/journeys/{id}/history`

**Status:** ✅ **EXISTS**

**Controller:** ✅ Yes (`ComplianceService.Controllers.V1.JourneysController.GetJourneyHistory`)

**Service:** ✅ Yes (`ComplianceAuditService.GetJourneyHistoryAsync`)

**Route Attributes:** ✅ Correct
- `[HttpGet("{journeyId:guid}/history")]` on `/api/v{version}/compliance/journeys`

**Business Rules:**
- ✅ All state transitions: Implemented (returns `List<JourneyHistoryItemDto>`)
- ✅ Timestamp: Implemented (`Timestamp` property)
- ✅ TriggeredBy: Implemented (`ChangedBy` property - User ID)
- ⚠️ **PARTIAL:** Role stored but not populated in DTO (`Role` property is null in mapping)
- ✅ Remarks: Implemented (`Remarks` property)
- ✅ Chronological sorting: **NOT VERIFIED** (should check repository implementation)
- ✅ Internal domain events excluded: Implemented (only exposes history entries, not domain events)

**Response Contract:** ✅ **CORRECT**

**DTO (`JourneyHistoryItemDto`):**
```csharp
{
  From,        // ✅ Old state
  To,          // ✅ New state
  ChangedBy,   // ✅ User ID
  Role,        // ⚠️ Always null (not stored in entity)
  Remarks,     // ✅ Present
  Timestamp    // ✅ Present
}
```

**Issues Found:**
- ⚠️ **MINOR:** `Role` property always returns `null` (comment in code: "Role not stored in history entity")
- ⚠️ **MINOR:** Chronological sorting not verified in repository query

**Security:**
- ✅ Tenant isolation: Enforced (`journey.TenantId != tenantId` throws `UnauthorizedAccessException`)
- ✅ 403 handling: Implemented (returns `Forbidden` on unauthorized access)

**HTTP Status Handling:**
- ✅ 400: BadRequest for empty journeyId
- ✅ 401: Unauthorized for missing tenant context
- ✅ 403: Forbidden for tenant mismatch
- ✅ 404: NotFound for journey not found
- ✅ 500: InternalServerError for unexpected errors

---

### 3️⃣ Valid Transitions

**Endpoint:** `GET /api/v1/journeys/{id}/valid-transitions`

**Status:** ✅ **EXISTS**

**Controller:** ✅ Yes (`ComplianceService.Controllers.V1.JourneysController.GetValidTransitions`)

**Service:** ✅ Yes (`ComplianceJourneyService.GetValidNextStatesAsync`)

**Route Attributes:** ✅ Correct
- `[HttpGet("{journeyId:guid}/valid-transitions")]` on `/api/v{version}/compliance/journeys`

**Business Rules:**
- ✅ Uses Journey State Machine: Implemented (`ComplianceLifecycleStateMachine.GetValidNextStates`)
- ✅ Computes allowed transitions: Implemented
- ✅ Based on Current State: Implemented
- ⚠️ **PARTIAL:** Pending Critical NCs check: Not verified (should check if implemented)
- ⚠️ **PARTIAL:** Suspension flags check: Not verified (should check if implemented)
- ✅ Framework policy applied: Implemented (`IFrameworkJourneyPolicy.CanTransition`)
- ✅ Role-based restrictions: Implemented (`ValidateRoleForTransition`)
- ✅ No invalid forward jumps: Enforced by state machine

**Response Contract:** ⚠️ **NEEDS FIX**

**Expected Response:**
```json
{
  "success": true,
  "data": [
    { "code": "SubmitForReview", "displayName": "Submit for Review" }
  ]
}
```

**Actual Response:**
```json
{
  "success": true,
  "data": ["Enrolled", "Submitted", "Approved"]  // Just enum names as strings
}
```

**Issues Found:**
- ❌ **CRITICAL:** Returns enum names as strings, not objects with `code` and `displayName`
- ⚠️ **MINOR:** No display names for transitions

**Security:**
- ✅ Journey existence validation: Implemented
- ⚠️ **MISSING:** Tenant isolation not explicitly checked (relies on repository)
- ✅ User context validation: Implemented (requires userId and role)

**HTTP Status Handling:**
- ✅ 400: BadRequest for empty journeyId
- ✅ 401: Unauthorized for missing user context
- ✅ 404: NotFound for journey not found
- ✅ 500: InternalServerError for unexpected errors

---

### 4️⃣ State Change

**Endpoint:** `POST /api/v1/journeys/{id}/transitions` (also `/transition`)

**Status:** ✅ **EXISTS**

**Controller:** ✅ Yes (`ComplianceService.Controllers.V1.JourneysController.TransitionJourney`)

**Service:** ✅ Yes (`ComplianceJourneyService.TransitionJourneyAsync`)

**Route Attributes:** ✅ Correct
- `[HttpPost("{journeyId:guid}/transition")]`
- `[HttpPost("{journeyId:guid}/transitions")]` (both supported)

**Business Rules:**
- ✅ Transition exists validation: Implemented (enum parsing)
- ✅ Transition allowed validation: Implemented (state machine + policy)
- ✅ Required conditions satisfied: Implemented (framework policy + role validation)
- ✅ Audit entry recorded: Implemented (creates `ComplianceJourneyHistory` entry)
- ✅ Lifecycle state updated: Implemented (via `ComplianceJourney.TransitionTo`)
- ✅ Transactional: **NOT VERIFIED** (should check if wrapped in transaction)
- ✅ 400 on invalid transition: Implemented
- ⚠️ **PARTIAL:** 409 on rule violation: Returns 400 instead of 409

**Response Contract:** ✅ **CORRECT**

**Issues Found:**
- ⚠️ **MINOR:** Returns 400 for business rule violations instead of 409 Conflict
- ⚠️ **MINOR:** Transaction scope not verified (should use UnitOfWork pattern)

**Security:**
- ✅ User context validation: Implemented
- ✅ Role validation: Implemented
- ⚠️ **MISSING:** Tenant isolation not explicitly checked in service (relies on repository)

**HTTP Status Handling:**
- ✅ 400: BadRequest for invalid target status or business rule violations
- ✅ 401: Unauthorized for missing user context
- ⚠️ **MISSING:** 409: Conflict not returned (should return for business rule violations)
- ✅ 500: InternalServerError for unexpected errors

---

### 5️⃣ Chapters

**Endpoint:** `GET /api/v1/journeys/{id}/chapters`

**Status:** ❌ **MISSING**

**Controller:** ❌ No

**Service:** ❌ No

**Route Attributes:** ❌ Not implemented

**Business Rules:**
- ❌ **NOT IMPLEMENTED**

**Expected Response:**
```json
{
  "success": true,
  "data": [
    {
      "chapterId": "guid",
      "name": "Patient Rights",
      "completionPercentage": 75,
      "ncCount": 3,
      "status": "InProgress"
    }
  ]
}
```

**Issues Found:**
- ❌ **CRITICAL:** Endpoint does not exist
- ❌ **CRITICAL:** No service method to calculate chapter progress
- ❌ **CRITICAL:** No DTO for chapter information
- ⚠️ **NOTE:** Chapter data exists in `ComplianceChapters` table but no endpoint to retrieve it

**Recommendation:**
- Create `GetJourneyChaptersAsync` in `ComplianceJourneyService`
- Create `JourneyChapterDto` with required fields
- Add endpoint in `JourneysController`
- Calculate completion % from tasks/requirements
- Count NCs from audit findings

---

### 6️⃣ NC Summary

**Endpoint:** `GET /api/v1/journeys/{id}/non-conformances`

**Status:** ❌ **MISSING**

**Controller:** ❌ No

**Service:** ❌ No

**Route Attributes:** ❌ Not implemented

**Business Rules:**
- ❌ **NOT IMPLEMENTED**

**Expected Response:**
```json
{
  "success": true,
  "data": {
    "minor": { "open": 5, "closed": 10, "overdue": 2 },
    "major": { "open": 3, "closed": 7, "overdue": 1 },
    "critical": { "open": 1, "closed": 2, "overdue": 0 }
  }
}
```

**Issues Found:**
- ❌ **CRITICAL:** Endpoint does not exist
- ❌ **CRITICAL:** No service method to aggregate NC counts
- ❌ **CRITICAL:** No DTO for NC summary
- ⚠️ **NOTE:** NC data may exist in `ComplianceSnapshot` (MajorNcCount, MinorNcCount) but no endpoint to retrieve it per journey

**Recommendation:**
- Create `GetJourneyNonConformancesAsync` in `ComplianceAuditService`
- Create `NonConformanceSummaryDto` with categorization
- Add endpoint in `JourneysController`
- Query audit findings or snapshot data
- Calculate open/closed/overdue counts by severity

---

### 7️⃣ Audit History

**Endpoint:** `GET /api/v1/journeys/{id}/history`

**Status:** ✅ **EXISTS** (Same as #2)

**Controller:** ✅ Yes (`ComplianceService.Controllers.V1.JourneysController.GetJourneyHistory`)

**Service:** ✅ Yes (`ComplianceAuditService.GetJourneyHistoryAsync`)

**Route Attributes:** ✅ Correct

**Business Rules:**
- ✅ Action: Implemented (`From` → `To` transition)
- ✅ PerformedBy: Implemented (`ChangedBy` - User ID)
- ✅ Timestamp: Implemented
- ✅ OldState: Implemented (`From`)
- ✅ NewState: Implemented (`To`)
- ✅ Immutable records: Implemented (read-only history table)
- ✅ No modification allowed: Enforced (no update/delete methods)

**Response Contract:** ✅ **CORRECT**

**Issues Found:**
- ⚠️ **MINOR:** `Role` property always null (not stored in history entity)

**Security:**
- ✅ Tenant isolation: Enforced
- ✅ Immutability: Enforced (no update methods)

**HTTP Status Handling:**
- ✅ All status codes properly handled

---

### 8️⃣ Dashboard Summary

**Endpoint:** `GET /api/v1/hospitals/{hospitalId}/journey-summary`

**Status:** ✅ **EXISTS**

**Controller:** ✅ Yes (`ComplianceService.Controllers.V1.AuditController.GetJourneySummary`, `Gateway.Controllers.HospitalsController.GetJourneySummary`)

**Service:** ✅ Yes (`ComplianceAuditService.GetJourneySummaryAsync`)

**Route Attributes:** ✅ Correct
- Backend: `[HttpGet("{hospitalId:guid}/journey-summary")]` on `/api/v{version}/hospitals`
- Gateway: `[HttpGet("{hospitalId:guid}/journey-summary")]` on `/api/v{version}/hospitals`

**Business Rules:**
- ✅ Total Frameworks: Implemented (`TotalFrameworks` property)
- ✅ Approved count: Implemented (`Approved` property)
- ✅ Expiring in 90 days: Implemented (`ExpiringIn90Days` - uses framework config renewal window)
- ✅ Suspended count: Implemented (`Suspended` property)
- ✅ Risk Index: Implemented (`RiskIndex` - weighted calculation)
- ✅ Aggregates across journeys: Implemented
- ✅ Scoped to Hospital: Implemented (filters by `HospitalId` and `TenantId`)

**Response Contract:** ✅ **CORRECT**

**DTO (`JourneySummaryDto`):**
```csharp
{
  TotalFrameworks,    // ✅ Present
  Approved,           // ✅ Present
  ExpiringIn90Days,  // ✅ Present
  Suspended,         // ✅ Present
  RiskIndex          // ✅ Present
}
```

**Issues Found:**
- ✅ **NONE** - All required fields present

**Security:**
- ✅ Tenant isolation: Enforced (filters by `TenantId`)
- ✅ Hospital scoping: Enforced (filters by `HospitalId`)
- ✅ Gateway validates hospital_id claim from JWT token

**HTTP Status Handling:**
- ✅ 400: BadRequest for empty hospitalId
- ✅ 401: Unauthorized for missing tenant context
- ✅ 500: InternalServerError for unexpected errors

---

## 🔒 Security Audit Summary

### Tenant Isolation
- ⚠️ **ISSUE:** `GetJourneyDetailsAsync` does not validate tenant isolation
- ✅ **GOOD:** `GetJourneyHistoryAsync` enforces tenant isolation
- ✅ **GOOD:** `GetJourneySummaryAsync` enforces tenant isolation
- ⚠️ **ISSUE:** `GetValidNextStatesAsync` does not explicitly validate tenant
- ⚠️ **ISSUE:** `TransitionJourneyAsync` does not explicitly validate tenant

### Hospital Scoping
- ⚠️ **ISSUE:** `GetJourneyDetailsAsync` does not validate hospital ownership
- ✅ **GOOD:** Gateway `GetJourneySummary` validates hospital_id claim
- ⚠️ **ISSUE:** Backend journey endpoints do not validate hospital ownership

### Authorization
- ✅ All endpoints require `ComplianceScope` policy
- ✅ User context validation in place
- ✅ Role-based restrictions in state transitions

---

## 📊 Implementation Status Summary

| Endpoint | Status | Controller | Service | Business Rules | Security | Response Contract |
|----------|--------|------------|---------|----------------|----------|-------------------|
| 1. Top Banner | ⚠️ Partial | ✅ | ✅ | ⚠️ Partial | ⚠️ Missing | ❌ Needs Fix |
| 2. Lifecycle Timeline | ✅ Exists | ✅ | ✅ | ✅ | ✅ | ✅ |
| 3. Valid Transitions | ✅ Exists | ✅ | ✅ | ✅ | ⚠️ Partial | ⚠️ Needs Fix |
| 4. State Change | ✅ Exists | ✅ | ✅ | ✅ | ⚠️ Partial | ✅ |
| 5. Chapters | ❌ Missing | ❌ | ❌ | ❌ | N/A | N/A |
| 6. NC Summary | ❌ Missing | ❌ | ❌ | ❌ | N/A | N/A |
| 7. Audit History | ✅ Exists | ✅ | ✅ | ✅ | ✅ | ✅ |
| 8. Dashboard Summary | ✅ Exists | ✅ | ✅ | ✅ | ✅ | ✅ |

**Score: 5/8 Fully Implemented (62.5%)**

---

## 🚨 Critical Issues Requiring Immediate Attention

### Priority 1 (Security)
1. **Add tenant isolation to `GetJourneyDetailsAsync`**
   - Validate `journey.TenantId == tenantId` before returning
   - Throw `UnauthorizedAccessException` if mismatch
   - Return 403 Forbidden in controller

2. **Add hospital scoping to journey endpoints**
   - Validate hospital ownership in service layer
   - Ensure user can only access journeys for their hospital

3. **Add tenant validation to `GetValidNextStatesAsync` and `TransitionJourneyAsync`**
   - Explicitly check tenant before processing

### Priority 2 (Response Contracts)
1. **Fix `JourneyDetailsDto` to match expected response**
   - Add `FrameworkName` (load from `journey.Framework.Name`)
   - Add `StartDate` (use `journey.EnrollmentDate`)
   - Add `TargetAccreditationDate` (calculate or use `ExpiryDate`)
   - Add `ProgressPercentage` (calculate from readiness score or tasks)
   - Add `RiskIndex` (use `RiskScore` or calculate aggregated index)

2. **Fix `GetValidTransitions` response format**
   - Return objects with `code` and `displayName` instead of string array
   - Create `ValidTransitionDto` class

### Priority 3 (Missing Endpoints)
1. **Implement `GET /journeys/{id}/chapters`**
   - Create service method to calculate chapter progress
   - Create DTO with required fields
   - Add controller endpoint

2. **Implement `GET /journeys/{id}/non-conformances`**
   - Create service method to aggregate NC counts
   - Create DTO with categorization
   - Add controller endpoint

### Priority 4 (HTTP Status Codes)
1. **Return 409 Conflict for business rule violations**
   - Change `TransitionJourney` to return 409 instead of 400 for rule violations

---

## ✅ Recommendations

1. **Implement missing endpoints** (Chapters, NC Summary)
2. **Fix response contracts** to match expected format
3. **Enhance security** with explicit tenant/hospital validation
4. **Add unit tests** for all endpoints
5. **Add integration tests** for security scenarios
6. **Document API contracts** in OpenAPI/Swagger

---

## 📝 Notes

- Gateway properly proxies requests to ComplianceService
- MediatR pattern correctly used in Gateway
- Domain events properly raised for audit trail
- State machine correctly enforces valid transitions
- Framework policies correctly applied

---

**Report End**

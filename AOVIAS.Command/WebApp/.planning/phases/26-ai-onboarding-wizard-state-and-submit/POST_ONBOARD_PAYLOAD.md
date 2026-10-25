# POST `/onboarding/onboard` — request body (for backend implementation)

This document matches the **TypeScript** contract in:

- `src/app/features/onboarding/models/onboard-submit-request.model.ts` (`OnboardSubmitRequest`)
- `src/app/features/onboarding/services/onboard-submit.mapper.ts` (`buildOnboardSubmitRequest`)

The gateway serves JSON with the usual **`ApiResponse`** envelope on responses; the **request body** for this endpoint is the JSON object below (camelCase).

## Shape

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `organizationId` | string (UUID) | Yes | From session (`organization_id` / `SessionService`). |
| `hospitalId` | string (UUID) | Yes | Selected hospital in wizard (`HospitalProfileData.hospitalId`). |
| `hospitalProfile` | object | Yes | Mirrors wizard hospital profile step. |
| `hospitalProfile.hospitalId` | string | Yes | Same as top-level `hospitalId` in normal flow. |
| `hospitalProfile.hospitalType` | string | Yes | |
| `hospitalProfile.bedCapacity` | string | Yes | |
| `hospitalProfile.countryCode` | string | Yes | |
| `hospitalProfile.stateCode` | string | Yes | |
| `hospitalProfile.departmentIds` | string[] (UUID) | Yes | At least one in UI. |
| `frameworkCodes` | string[] | Yes | Compliance framework **business codes** (`FrameworkLookup.code`), not platform UUIDs. |
| `assessmentAnswers` | array | Yes | One entry per assessment question. |
| `assessmentAnswers[].questionId` | string | Yes | |
| `assessmentAnswers[].question` | string | Yes | Snapshot of question text. |
| `assessmentAnswers[].answer` | `"yes"` \| `"no"` \| `"not_sure"` | Yes | |
| `teamInvites` | array | Yes | May be **empty** if user skipped team step. |
| `teamInvites[].name` | string | Yes | When row is included. |
| `teamInvites[].email` | string | Yes | Lowercased in payload. |
| `teamInvites[].role` | string | Yes | **Canonical role id** (e.g. `ORG_ADMIN`, `MANAGER`) — not a display label. |
| `teamInvites[].departmentId` | string (UUID) | Yes | Must be one of `hospitalProfile.departmentIds`. |
| `wizardVersion` | number | Yes | Client draft version; currently **1**. |

## Example JSON body

```json
{
  "organizationId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "hospitalId": "b2c3d4e5-f678-9012-bcde-f12345678901",
  "hospitalProfile": {
    "hospitalId": "b2c3d4e5-f678-9012-bcde-f12345678901",
    "hospitalType": "Multi Specialty Hospital",
    "bedCapacity": "101-200",
    "countryCode": "IN",
    "stateCode": "MH",
    "departmentIds": [
      "a1111111-1111-1111-1111-111111111111",
      "a2222222-2222-2222-2222-222222222222"
    ]
  },
  "frameworkCodes": ["NABH", "JCI"],
  "assessmentAnswers": [
    {
      "questionId": "q1",
      "question": "Do you perform internal quality audits?",
      "answer": "yes"
    },
    {
      "questionId": "q2",
      "question": "Do you maintain infection control protocols?",
      "answer": "not_sure"
    }
  ],
  "teamInvites": [
    {
      "name": "Jane Doe",
      "role": "ORG_ADMIN",
      "email": "jane.doe@hospital.org",
      "departmentId": "a1111111-1111-1111-1111-111111111111"
    }
  ],
  "wizardVersion": 1
}
```

## HTTP

- **Method:** `POST`
- **Path:** `/onboarding/onboard` (relative to gateway `apiBaseUrl`)
- **Auth:** `Authorization: Bearer <access_token>` (existing interceptors)
- **Headers:** `Idempotency-Key: <uuid>` — sent by the client on submit (new UUID per completion attempt; same value retained on retry after error until success). `X-Hospital-Id` may be set by `auth` interceptor when hospital context exists.

## Response (suggested)

- **200** — onboarding completed; body per gateway envelope (`success`, `data`, …).
- **400** — validation (e.g. unknown `departmentId`, duplicate constraints).
- **401/403** — session handling per global auth (Phase 25).

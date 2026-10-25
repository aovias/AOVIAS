# Phase 60 — Wave 1 summary: Organization members API client

## Completed

- Added **`OrganizationMembersApiService`** (`getMembersForOrganization`) → `GET {apiBaseUrl}/organization-members/organization/{organizationId}` with GUID validation (invalid/missing id → empty list, no HTTP).
- Added **`organization-member-api.model.ts`** with **`mapOrganizationMemberApiRow`** mapping flexible API keys to existing **`OrganizationMember`** (assignee value = **`userId`** for downstream Compliance create).
- **Did not** change **`OrganizationService.loadOrganizationDetails`** — hospitals and embedded members remain on the existing **`/tenant/.../organization-detail`** flow; the task drawer loads assignees via the **dedicated** organization-members endpoint per Phase 60 plan.
- Unit tests: **`organization-members-api.service.spec.ts`** (success mapping, invalid id skips HTTP, 404 propagates). HttpTestingController receives **unwrapped** array bodies (interceptor not registered in test).

## Files

- `src/app/features/journey/services/organization-members-api.service.ts`
- `src/app/shared/models/organization-member-api.model.ts`
- `src/app/features/journey/services/organization-members-api.service.spec.ts`

## Notes

- If backend returns `{ items: [...] }` instead of a bare array, **`normalizeMemberArray`** unwraps `items` or nested `data`.

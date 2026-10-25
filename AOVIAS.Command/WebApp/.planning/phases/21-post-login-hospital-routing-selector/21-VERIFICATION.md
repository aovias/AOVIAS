---
phase: 21
status: passed
---

# Verification Phase 21: Post-login hospital routing and selector

## Must-have checks

- [x] Login flow checks `organization-detail.data.hospitals[]` for at least one `isOnboarded: true`
- [x] Routing rule implemented:
  - [x] onboarded hospital exists → `/app/dashboard`
  - [x] no onboarded hospital → `/onboarding`
- [x] Header hospital selector populated from API-backed organization hospitals
- [x] Default selected hospital is onboarded one when present (fallback first hospital)
- [x] Header selection updates `ComplianceContextService` hospital id
- [x] No hardcoded hospital options remain in header

## Edge-case handling

- Empty or missing hospitals list shows fallback text and disables hospital selector.
- Multiple onboarded hospitals use deterministic first-match selection.

## Quality checks

- Lints: no issues in touched files
- Architecture: API calls remain in service layer; components do not call HttpClient directly


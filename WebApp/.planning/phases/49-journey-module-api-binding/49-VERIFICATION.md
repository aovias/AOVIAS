# Phase 49 — Verification

## Automated

- `ng build` — passed (2026-04-11).
- `journey-workspace.mapper.spec.ts` — split merge + chapter index unwrap (run via project test runner when configured).

## Endpoint → UI (implementation)

| Endpoint | UI surface | Notes |
|----------|------------|--------|
| GET `…/session` | Framework context, mode progression, read-only flags, `lastSyncedAt` | Merged into `JourneyWorkspaceSnapshot` |
| GET `…/dashboard` | Metrics, charts, `accreditationReadinessPercent` | |
| GET `…/chapters` | Chapter list (`items` unwrapped) | |
| GET `…/chapters/{id}/standards` | Standards + OE rows | On chapter select / initial first chapter |
| GET `…/objective-elements/{id}` | Context panel, wizard, OE refresh | |
| PATCH `…/objective-elements/{id}` | OE drawer / scoring | `version` + 409 refresh |
| PATCH `…/session/operational-mode` | Mode cards | Then reload split data |
| POST multipart evidence | Journey tabs upload | Max 10 MB |

## Human needed

- [ ] Confirm Gateway base path: `api/v1` vs `api/v1.0` matches deployment.
- [ ] Logged-in user with `compliance.api` scope: full journey load without 403.
- [ ] Mode advance: only linear next step per API.
- [ ] Evidence upload and DELETE (if used) against real storage.

## Residual

| Item | Owner |
|------|--------|
| POST `…/wizard/progress` | Backend / product |
| Server-side chapter `search`/`sort` | Backend |

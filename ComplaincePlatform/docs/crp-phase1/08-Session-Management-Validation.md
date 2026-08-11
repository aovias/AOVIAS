# CRP Phase 1 — Session Management Validation

| Capability | Status |
|------------|--------|
| Session create on login | ✔ |
| Refresh rotation / token family | ✔ |
| Logout current + logout-all | ✔ |
| List / revoke sessions | ✔ AuthController |
| Cleanup hosted service | ✔ when `EnableBackgroundJobs` |
| Concurrent sessions | Allowed (no single-session force) |
| Device / trusted device | ✔ existing security stack |

Administrative termination: `DELETE /api/v1/auth/sessions/{sessionId}` and `POST logout-all`.

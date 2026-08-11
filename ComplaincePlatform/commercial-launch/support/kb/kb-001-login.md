# KB-001 — Sign-in and Access Issues

## Symptoms

- Login page rejects credentials  
- Infinite redirect / blank app shell  
- 401/403 on API calls after login  
- User lands on wrong role home  

## Quick checks (L1)

1. Confirm WebApp URL and Gateway base URL match the customer environment.  
2. Ask whether password was recently reset; retry after clear site data / private window.  
3. Verify user is active in Identity and has an organization membership.  
4. Confirm license seats: user may authenticate but entitlement soft-gates features — check **Settings → Platform → Licensing** usage.  
5. If entire site returns 503 with maintenance message → see [KB-005](kb-005-maintenance-mode.md).

## Admin steps

1. ORG_ADMIN: confirm user role mapping (Owner / QM / Dept Head / etc.).  
2. Re-assign seat if MaxSeats reached: `POST /api/v1/licenses/seats/assign`.  
3. Check Gateway logs for `X-Correlation-ID` from the failed request.  
4. MFA / JWT: ensure customer clock sync (JWT skew) and MFA enrollment if required.

## Escalate when

- Auth works for admins but not an entire IdP / directory sync cohort  
- Token issued but all `/api/v1/*` return 401 (Gateway auth config)  
- Suspected privilege misconfiguration → Access Governance privilege audit export  

Collect diagnostics: [../diagnostic-collection.md](../diagnostic-collection.md).

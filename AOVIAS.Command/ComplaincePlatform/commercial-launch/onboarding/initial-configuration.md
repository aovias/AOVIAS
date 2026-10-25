# Initial Configuration

**Product:** AOVIAS Command v1.0.0  
**Audience:** Hospital ORG_ADMIN / Implementation partner

Complete after deployment validation and license activation.

## 1. Installation overlay

If using the Gateway setup wizard:

1. Open WebApp `/setup`.
2. Test database connectivity.
3. Configure connection strings / service endpoints (secrets stay in `App_Data/aovias.installation.json`, not source `appsettings`).
4. Complete wizard → restart Gateway so overlay loads.
5. Confirm lock: further setup mutations return `RESOURCE_CONFLICT`.

## 2. Organization structure

| Step | Action | Notes |
|------|--------|-------|
| 1 | Confirm organization record | Match license `organizationId` |
| 2 | Create / verify hospitals | Campus hierarchy via Organizational Units if multi-site |
| 3 | Departments | Align to NABH / hospital org chart |
| 4 | Activate units | Inactive departments cannot receive OE ownership |

UI: Organization Explorer / settings organization routes in WebApp.  
APIs: Gateway organization / hospitals proxies (Phase 1).

## 3. Identity & roles

1. Create SUPER_ADMIN (platform) and ORG_ADMIN (hospital) accounts.
2. Map Quality Manager, Department Head, Consultant, Auditor personas as needed.
3. Assign license seats: `POST /api/v1/licenses/seats/assign` (respect MaxSeats).
4. Verify login and role home dashboards.

## 4. Platform settings

| Setting | Location | Pilot default |
|---------|----------|---------------|
| Licensing | Settings → Platform → Licensing | Active key + seats |
| Commercial Ops | Settings → Platform → Commercial Ops | Health / diagnostics (admin) |
| Maintenance | `MAINTENANCE_MODE` or `App_Data/maintenance.lock` | Off for go-live |
| Notifications | CommunicationService config | Console fallback OK for pilot if SMTP pending |
| AI | AIOrchestrator flags | Keep autonomous workflows **off** unless approved |

## 5. Correlation & support readiness

- Ensure `X-Correlation-ID` flows in Gateway logs.
- Walk admin through downloading diagnostics: `GET /api/v1/commercial/diagnostics?organizationId=`.

## Exit

Organization hierarchy, admins, seats, and platform settings are ready for framework import and first journey.

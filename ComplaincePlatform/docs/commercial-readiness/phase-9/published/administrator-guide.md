# Administrator Guide — AOVIAS Command v1.0.0

**Audience:** Hospital IT admins, platform operators, AOVIAS partner engineers  
**Related:** [deployment-guide.md](./deployment-guide.md), [troubleshooting-guide.md](./troubleshooting-guide.md)

## 1. Responsibilities

Administrators own tenant readiness: identity/org structure, license activation, seat assignment, environment configuration, backup/upgrade windows, and entitlement posture—not clinical content authorship.

## 2. Tenant bootstrap

1. Confirm Organization, Identity, and hospital records exist (onboarding wizard or ops scripts).
2. Issue or receive a license key with required entitlements (`core.compliance`, optional `ai.assist`, `governance.risk`, `ace.insights`).
3. Activate online or offline via Gateway licensing APIs (`/api/v1/licenses/activate`).
4. Assign seats to users who will consume licensed modules.
5. Verify `GET /api/v1/licenses/status` shows Active (or Grace with a clear renewal plan).

## 3. Roles and access

- Map hospital staff to platform roles (Hospital Owner, Quality Manager, Department Head, Consultant, Auditor) through Organization/Identity membership.
- Prefer least privilege: auditors receive read/audit workspace access; department heads only assigned standards.
- Soft entitlement gates use `X-Require-Entitlement` at the Gateway—confirm codes match the sold SKU.

## 4. Operational controls

| Control | Where |
|---------|--------|
| Maintenance mode | Phase 7 enterprise ops |
| Secrets | Key Vault or env (never commit) |
| Correlation | Preserve `X-Correlation-ID` across Gateway → services |
| Health | Service health endpoints + compose healthchecks |
| Backups | Follow deployment-guide backup section before upgrades |

## 5. Day-2 checklist

- [ ] Heartbeat succeeds for the active license
- [ ] Seat usage within MaxSeats
- [ ] Migrations current after each release
- [ ] Demo or staging data isolated from production PHI
- [ ] SBOM/checksum artifacts retained for the installed build

## 6. Escalation

Use [troubleshooting-guide.md](./troubleshooting-guide.md) first. For license expiry or entitlement mismatches, contact AOVIAS commercial ops with organization ID and correlation IDs from failed requests.

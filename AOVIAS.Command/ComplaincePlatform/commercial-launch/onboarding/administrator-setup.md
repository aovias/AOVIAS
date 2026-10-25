# Administrator Setup

**Product:** AOVIAS Command v1.0.0  
**Audience:** SUPER_ADMIN / ORG_ADMIN

## Accounts

| Role | Purpose | Notes |
|------|---------|-------|
| SUPER_ADMIN / SYSTEM | Platform ops, commercial provision | AdminOnly APIs |
| ORG_ADMIN | Hospital configuration | Day-2 ownership |
| Quality Manager | Program execution | Not a substitute for ORG_ADMIN |

Create admins in IdentityService via Gateway onboarding / user APIs. Store break-glass credentials in customer vault — never in tickets.

## Day-0 admin sequence

1. Complete `/setup` (if fresh install) and restart Gateway.
2. Activate license — [../licensing/activation-guide.md](../licensing/activation-guide.md).
3. Confirm `GET /api/v1/licenses/status?organizationId=` → operational / Active or Grace.
4. Invite hospital users; assign seats.
5. Configure org hierarchy ([initial-configuration.md](initial-configuration.md)).
6. Activate framework ([framework-import.md](framework-import.md)).
7. Verify Commercial Ops health: Settings → Platform → Commercial Ops, or `GET /api/v1/commercial/customer-health?organizationId=`.

## Entitlements to verify

| Feature code | Typical SKU | Check |
|--------------|-------------|-------|
| `core.compliance` | Core | Journey / evidence |
| `governance.risk` | Governance | Risk register |
| `ai.assist` | AI add-on | Assistant with citations |
| `ace.insights` | ACE | Insights surfaces |

`GET /api/v1/licenses/entitlements/check?organizationId=&featureCode=`

Soft gate: Gateway `LicenseEntitlementMiddleware` (`X-Require-Entitlement`).

## Security baseline for admins

- MFA enabled for admin accounts (Phase 7 JWT / MFA surfaces).
- No shared admin logins.
- Privilege audit export available via Access Governance when investigating access issues.
- Do not disable maintenance-mode middleware; use it intentionally during upgrades.

## Handoff package to Quality Manager

- Org + hospital IDs  
- Active framework version  
- License expiry / grace dates  
- Support contact + diagnostics instructions  
- Training schedule dates  

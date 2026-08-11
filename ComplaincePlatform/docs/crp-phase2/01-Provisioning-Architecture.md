# CRP Phase 2 — Provisioning Architecture

## Purpose

Deterministic commercial bootstrap for SaaS self-service signup: Organization + Hospital + Admin + Membership + **Trial LicenseKey** (issue → activate → seat) + org defaults + welcome email, then JWT and redirect to the onboarding wizard.

## Boundary

| In scope | Out of scope |
|----------|--------------|
| Extend `SignupCommandHandler` | Payment / Stripe |
| `TrialEnablement` config | Marketplace |
| LicenseKey SSOT via OrganizationService | SSO / IdP / SCIM |
| Idempotent ensure-defaults | Multi-region |
| Welcome email (best-effort) | Redesigning Identity |

## Orchestrator

`Gateway.Application.Handlers.SignupCommandHandler` remains the single commercial entry for `/api/v1/onboarding/signup` and identity register façade.

**Order (commercial):** CreateOrg → CreateHospital → EnsureDefaults → CreateUser → CreateMembership → **ProvisionCommercialTrial** → GenerateJWT → WelcomeEmail.

## Dependencies

- Phase 1 email verification modes (Async default) still gate anonymous provision when `RequiredBeforeOrganizationProvisioning`.
- OrganizationService internal APIs: licenses + ensure-defaults.
- `IEmailService` on Gateway (`Console` / `Smtp`).

## Frozen decisions (Phase 0)

- Organization = tenant  
- LicenseKey = commercial SSOT  
- Trial default 30 days; auto-issue + auto-activate  
- Wizard remains post-provision (`Hospital.IsOnboarded`)

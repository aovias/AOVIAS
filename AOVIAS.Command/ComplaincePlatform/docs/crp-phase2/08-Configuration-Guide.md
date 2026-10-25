# CRP Phase 2 — Configuration Guide

## Gateway `TrialEnablement`

```json
"TrialEnablement": {
  "AutoIssueOnSignup": true,
  "ValidDays": 30,
  "MaxSeats": 10,
  "GracePeriodDays": 7,
  "AutoActivate": true,
  "MachineFingerprintMode": "SignupHost",
  "Entitlements": [
    "core.compliance",
    "core.documents",
    "core.capa",
    "ai.assist",
    "governance.risk",
    "ace.insights"
  ],
  "DualWriteOrganizationSubscription": false,
  "DefaultTrialPlanId": null,
  "MaxExtensions": 2,
  "ExtensionDays": 15
}
```

Bound to `Gateway.Configuration.TrialEnablementOptions`.

## Email (Gateway)

```json
"Email": { "Provider": "Console" }
```

Production: `Smtp` with `Smtp:*` settings (same as Phase 1 Identity).

## Identity (unchanged Phase 1)

Mirror `Identity:EmailVerificationMode` on Gateway for org-provisioning gate.

## Feature flags

| Flag | Effect |
|------|--------|
| AutoIssueOnSignup=false | Skip LicenseKey; `ProvisioningStatus=Skipped` |
| AutoActivate=false | Leave Issued; status `LicenseIssuedPendingActivation` |
| DualWriteOrganizationSubscription=true | Also POST `/internal/subscriptions` when plan id set |

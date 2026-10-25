# CRP Phase 2 — Operational Runbook

## Deploy

1. Deploy OrganizationService (license get-or-issue, revoke, seat release, ensure-defaults).  
2. Deploy Gateway (`TrialEnablement`, email registration, SignupCommandHandler).  
3. Deploy WebApp (signup → session + `/onboarding` trial banner).  
4. Restart Gateway / OrganizationService if DLL locks during local rebuild.

## Verify healthy signup

1. `POST /api/v1/onboarding/signup` with unique org name + email.  
2. Expect 200 with `provisioningStatus=Completed`, `licenseStatus=Active`, JWT.  
3. Confirm OrganizationService has one Trial LicenseKey Active + one seat.  
4. Confirm welcome email in Console logs (dev) or SMTP.  
5. WebApp lands on `/onboarding` with trial remaining banner.

## Common incidents

| Symptom | Check |
|---------|-------|
| Signup 400 after membership | License/activate/seat logs; OrgService up |
| Skipped provisioning | `TrialEnablement:AutoIssueOnSignup` |
| Duplicate org | 409 — choose new name |
| Email missing | `Email:Provider`, Smtp config; signup still succeeds |
| Wizard blocked | JWT / onboarding entry guard; hospital not yet onboarded |

## Disable trial temporarily

Set `"TrialEnablement": { "AutoIssueOnSignup": false }` and restart Gateway.

# CRP Phase 2 — Organization Initialization Spec

## Created resources

| Resource | Service | Notes |
|----------|---------|-------|
| Organization | OrganizationService | Unique name; settings row on create |
| Hospital | OrganizationService | `IsOnboarded` typically false until wizard |
| Admin User | IdentityService | Phase 1 verification modes apply |
| OrganizationMember | OrganizationService | Role `ORG_ADMIN` |
| OrganizationSettings | EnsureDefaults | Idempotent |
| Trial LicenseKey | LicenseAppService | Issued → Active |
| LicenseSeatAssignment | Admin user | One seat |
| JWT | Identity token generate | After commercial bootstrap |

## Response contract (`SignupResponseDto`)

- Existing: token, refreshToken, expiresIn, userId, organizationId, hospitalId, email, role  
- Phase 2: `licenseKeyId`, `licenseStatus`, `trialExpiresAtUtc`, `maxSeats`, `provisioningStatus`

## ProvisioningStatus values

`Completed` | `Skipped` | `LicenseIssuedPendingActivation`

# CRP Phase 2 — License Activation Design

## SaaS path

When `TrialEnablement:AutoActivate=true` (default):

1. Issue / get-or-issue Trial key  
2. `POST /internal/licenses/activate` online  
3. Fingerprint = `Environment.MachineName` when `MachineFingerprintMode=SignupHost`

## Idempotency

`ActivateOnlineAsync` skips duplicate activation when the key is already Active and the fingerprint has an active activation row.

## Failure policy

| Config | Activate fails after 1 retry |
|--------|------------------------------|
| AutoActivate=true | Fail signup + rollback |
| AutoActivate=false | Allow `ProvisioningStatus=LicenseIssuedPendingActivation` |

Offline activation remains the existing activate API with `Offline=true` (not used by SaaS signup).

# Phase 1 — Evidence Management

**Sprint:** 4  
**Status:** Delivered

## Virus scan extension point

| Type | Role |
|------|------|
| `IEvidenceVirusScanProvider` | Scan stream on register/upload |
| `NoOpEvidenceVirusScanProvider` | Always clean (Dev / default DI) |

Hook: `EvidenceRegistrationService.RegisterFromUploadAsync` scans before save; unclean results reject the upload. Replace NoOp in production DI with a real provider.

## Expiry

Existing fields used (no new columns):

- `Evidence.ExpiresAt`  
- `Evidence.LifecycleStatus` (Draft / Submitted / Verified / Rejected)

`EvidenceExpiryJob` (Hangfire daily ~1:30 UTC):

1. Select rows where `ExpiresAt <= UtcNow` and LifecycleStatus ≠ Rejected  
2. Set LifecycleStatus = Rejected  
3. Emit structured `EvidenceExpired NotificationHook` log for alert/outbox subscribers  

Registered next to `ComplianceExpiryJob` in `Program.cs`.

## Registration pipeline (unchanged shape)

Upload → scan → store → Evidence → ObjectiveElementEvidence → obligation/deficiency projection → snapshot queue.

## Related

Review workflow hardening: `07-review-workflow.md`.

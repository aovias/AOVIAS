# Phase 1 — Evidence Review Workflow

**Sprint:** 4  
**Status:** Delivered (hardened; APIs preserved)

## Pipeline

`EvidenceReviewService.ApplyReviewAsync` remains the single approval path:

1. Load OE↔Evidence links  
2. Validate transitions via `EvidenceVerificationStateMachine`  
3. Write audit (`EvidenceReviewAuditEntry`)  
4. Project obligations / deficiencies / events / snapshot queue  

Review-queue list/detail APIs are unchanged.

## Hardening added

| Rule | Behavior |
|------|----------|
| Double-approve | Rejected — Verified is terminal |
| Mutations when closed | Rejected when `VerificationStatus == Verified` with clear error |
| State machine | Explicit `(Verified, Verified) => false` + existing `(Verified, _) => false` |

Valid transitions still allow Rejected / NeedsMoreEvidence → resubmit → Pending → Approve.

## Decisions

`EvidenceReviewDecision`: Approve | Reject | RequestMoreEvidence.

## Extension points

- Virus scan before registration (`06-evidence-management.md`)  
- Expiry job may set artifact LifecycleStatus=Rejected independently of verification status; review queue filters should continue to use OE link VerificationStatus

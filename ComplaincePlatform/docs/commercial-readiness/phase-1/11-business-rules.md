# Phase 1 — Business Rules

**Sprint:** 6  
**Status:** Delivered (enforced in services)

| # | Rule | Enforcement |
|---|------|-------------|
| 1 | Inactive department cannot receive new OE / department ownership | `ObjectiveElementOwnershipAppService.UpsertAsync`, `DepartmentOwnershipAppService.UpsertAsync` |
| 2 | Archived framework is immutable | `FrameworkLifecycleService.EnsureNotArchived` blocks publish/activate/retire/archive |
| 3 | Single active version per framework family (`Name`) | `FrameworkLifecycleService.ActivateAsync` retires sibling Active rows |
| 4 | No double-approve evidence | `EvidenceVerificationStateMachine` (Verified terminal) + explicit guard in `EvidenceReviewService` |
| 5 | Closed / verified evidence immutable | Review service rejects mutations when status is Verified |
| 6 | Journey-locked evidence upload | `EvidenceRegistrationService.EnsureJourneyWritableAsync` when journey `Status >= Submitted` |
| 7 | OU hierarchy edges | `OrganizationalUnitHierarchyPolicy` on create/move |

## Notifications (console / in-app)

`IComplianceNotificationPublisher` (default: `ConsoleComplianceNotificationPublisher`):

- `oe.ownership.assigned`
- `evidence.submitted` / `evidence.approved` / `evidence.rejected` / `evidence.needs_more`
- `framework.activated`

Replace publisher implementation to call CommunicationService when delivery channels are production-ready.

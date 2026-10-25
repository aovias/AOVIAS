# CRP Phase 3 — Onboarding Lifecycle Specification

## Mapping (mission → SSOT → UI)

| Mission stage | Source of truth | UI surface |
|---------------|-----------------|------------|
| Welcome | First wizard step | Enhanced welcome |
| Email Verified | Identity claim / mode | Journey badge |
| Organization Ready | Org exists post-signup | Journey checklist |
| License Active | `GET /licenses/status` | Trial panel |
| Organization / Hospital Profile | Wizard step 2 + org settings | Guided setup |
| Framework Selection | Wizard step 3 | Existing step |
| Team Setup | Wizard step 5 | Existing step (optional) |
| Compliance Assessment | Wizard step 4 | Step + helpText |
| AI Recommendations | AI panel copy | Role-aware assistant |
| Operational Readiness | `Hospital.IsOnboarded` after `POST /onboarding/onboard` | 100% progress |

## Per-stage contract

Each stage documents:

- **Purpose** — why the stage exists in adoption
- **Owner** — who can complete (admin gates vs assessment-capable roles)
- **Prerequisites** — prior stages Completed or org/license ready
- **Success** — fact that flips status to Completed
- **Failure** — missing license, network, validation
- **Retry** — refresh journey; resume wizard draft
- **Events** — `StageEntered` / `StageCompleted` / `StepSkipped`
- **Completion rule** — composer `Done` predicate or `IsOnboarded`

### Admin-gated stages

Hospital profile, framework selection, and operational readiness require an admin-capable role. Non-admins see `Blocked` with `OwnerHint`.

### Optional

Team setup is optional; progress percent uses required stages only.

### Terminal

`IsOnboarded == true` → all required stages Completed, `progressPercent = 100`, next action “Open dashboard”.

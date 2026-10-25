# CRP Phase 2 — Default Data Strategy

## Principle

Do **not** re-seed platform-global masters on every signup. Ensure **per-organization** defaults only.

## Ensure-defaults

`POST /internal/organizations/{id}/ensure-defaults`

- Creates `OrganizationSettings` if missing (timezone, language, working hours).
- Safe to call twice (`settingsCreated` / `alreadyHadSettings`).
- Framework enrollment stays in the **onboarding wizard** (avoids conflicting with assessment).

## Called when

Immediately after hospital create in `SignupCommandHandler`.

## Not in signup

- Framework catalog binding  
- Assessment questionnaires  
- Team invites (wizard)

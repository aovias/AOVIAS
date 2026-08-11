# CRP Phase 3 — Role-aware Onboarding Design

## Primary role

SaaS signup creates `ORG_ADMIN`. Other roles may enter incomplete onboarding via invite/membership.

## Guidance map

File: `WebApp/.../onboarding-role-guidance.ts`

Adjusts:

- Welcome subtitle / next actions
- AI assistant panel messages

## Capability rules (composer)

Admins (`*ADMIN*`, ORG_ADMIN, HOSPITAL_ADMIN, PLATFORM_ADMIN) may act on all stages.

Non-admins may act on Welcome, ComplianceAssessment, AiRecommendations; admin-gated stages are `Blocked` with `OwnerHint` (e.g. Organization administrator).

## UX rule

Auditors/quality roles get assessment-focused copy but **cannot** skip required admin gates to force `IsOnboarded`.

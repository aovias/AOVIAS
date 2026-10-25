# CRP Phase 3 — Guided Setup Architecture

## Principle

Extend the existing six-step AI wizard; do not invent a parallel product tour package.

## Guidance surfaces

| Surface | Mechanism |
|---------|-----------|
| Assessment | Render API `helpText` under each question |
| Profile / frameworks / team | Material titles / simple tooltips |
| Welcome + stepper | First-visit coach tips (`localStorage onboarding.tour.dismissed`) |
| AI panel | Role-aware copy from `onboarding-role-guidance.ts` |

## Auto-save

Unchanged: `AiOnboardingWizardStore` persists draft progress.

## Role awareness

`ORG_ADMIN` remains primary signup role. Guidance map covers ORG_ADMIN, HOSPITAL_ADMIN, Quality/Compliance, DEPT_HEAD, AUDITOR. Non-admins see relevant tasks but cannot skip admin gates (`Blocked` + owner hint).

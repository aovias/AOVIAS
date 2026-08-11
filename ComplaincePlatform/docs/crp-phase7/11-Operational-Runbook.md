# CRP Phase 7 — Operational Runbook

1. Apply migration `20260731230000_Phase7_WorkforceInvitations`
2. Deploy Org, Workforce, Gateway, WebApp
3. Confirm `OnboardingWizard:UseEmailInvites=true` and WorkforceOnboarding URLs
4. Smoke: create invite → open accept page → redeem → verify membership+employment+seat
5. Rollback: set UseEmailInvites=false

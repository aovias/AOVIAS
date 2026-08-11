# CRP Phase 6 — Security Specification

- Entitlement evaluation server-side only; client flags are not authority
- Fail-closed mutations on gated routes; upstream check failure denies
- Offline activation still HMAC-signed via Licensing:SigningKey
- CommercialAdmin policy for backfill/suspend/archive
- Heartbeat / activation paths remain authenticated + org-scoped
- Do not trust X-Require-Entitlement alone without org claim

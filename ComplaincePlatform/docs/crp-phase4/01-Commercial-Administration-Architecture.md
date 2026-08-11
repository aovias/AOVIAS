# CRP Phase 4 — Commercial Administration Architecture

## Purpose
Internal AOVIAS multi-tenant operations console for CS, Support, Licensing, Sales Ops, and Platform Administration.

## Hosting
- WebApp: `/commercial-admin` (dedicated shell)
- Gateway: `api/v1/commercial-admin/*` policy `CommercialAdmin`
- App services only — no direct DB admin

## Out of scope
Stripe, SSO, CRM, approval engine, admin MFA product.

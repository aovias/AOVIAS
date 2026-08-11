# CRP Phase 6 — Unified Licensing Architecture

LicenseKey is the operational single source of truth. SubscriptionPlan / OrganizationSubscription are commercial catalogue metadata only.

## Flow

SubscriptionPlan (catalogue) → License issuance → Activation → Entitlements → Seat allocation → Feature access (Gateway EntitlementEngine + fail-closed writes).

## Components

- OrganizationService: LicenseAppService, EntitlementEngine, catalogue sync/backfill
- Gateway: LicensesController, LicensingController summary, LicenseEntitlementMiddleware (after TrialAccessMiddleware), TrialAccessMiddleware
- WebApp: licensing summary UX, commercial-admin license inventory + backfill

## Non-goals

Payment, billing, invoices, tax, marketplace, CRM, revenue analytics.

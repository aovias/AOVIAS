# CRP Phase 6 — Operational Runbook

## Deploy

1. Apply migration `20260731220000_Phase6_LicensingConsolidation`
2. Deploy OrganizationService then Gateway then WebApp
3. Confirm Licensing section in Gateway/Org appsettings
4. Run Commercial Admin → Licenses → Run catalogue backfill
5. Spot-check `/api/v1/licensing/summary?organizationId=`

## Incidents

- Entitlement false denials: verify pack includes code; temporarily FailClosedWrites=false
- Catalogue drift: re-run backfill; inspect Notes in result
- Suspended org writes blocked: expected; unsuspend via license admin

# CRP Phase 6 — Migration Strategy

1. Dual-read summary (license first + plan enrichment)
2. SyncCatalogFromLicense on issue (Licensing:SyncCatalogFromLicense=true) and signup
3. POST catalog/backfill links OrganizationSubscriptionId, creates missing catalogue rows, reports drift
4. DualWriteOrganizationSubscription remains optional legacy alias
5. Rollback: keep subscription endpoints; set Licensing:FailClosedWrites=false if needed
6. No customer downtime — additive schema (LifecycleStatus) + compatible entitlement JSON

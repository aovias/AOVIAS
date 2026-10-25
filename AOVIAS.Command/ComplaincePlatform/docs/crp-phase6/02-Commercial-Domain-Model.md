# CRP Phase 6 — Commercial Domain Model

| Concept | Role |
|---------|------|
| SubscriptionPlan | Catalogue offering (Code, Name, price metadata, MaxUsers/MaxFrameworks as catalogue hints, LifecycleStatus Draft/Published/Retired) |
| OrganizationSubscription | Optional catalogue linkage row for an org; not used for feature gates |
| LicenseKey | Operational rights (type, status, seats, entitlements, expiry, grace) |
| LicenseActivation / Seat / Heartbeat | Operational sub-entities |

Plan MaxUsers/MaxFrameworks are never evaluated as runtime gates; map into entitlements / MaxSeats at issuance.

# CRP Phase 2 — Trial License Architecture

## Model

`LicenseKey` (`LicenseType.Trial`) is the commercial source of truth for trial entitlement.

| Field | Signup default |
|-------|----------------|
| ValidDays | 30 (`TrialEnablement:ValidDays`) |
| MaxSeats | 10 |
| GracePeriodDays | 7 |
| Entitlements | core.compliance, core.documents, core.capa, ai.assist, governance.risk, ace.insights |
| Status after signup | Active (when AutoActivate=true) |

## Duplicate guard

One operational Trial **or** Subscription per organization (`Issued` / `Active` / `Grace`).

- `IssueAsync` throws if an operational commercial key exists.
- Signup uses `POST /internal/licenses/trial/get-or-issue` → `GetOrIssueTrialAsync` (reuse if present).

## Dual-write

`DualWriteOrganizationSubscription` + plan id optionally creates `OrganizationSubscription` (catalogue metadata only). Default **false**.

## Extensions (config only in Phase 2)

`MaxExtensions=2`, `ExtensionDays=15` — documented for future use; not executed in signup.

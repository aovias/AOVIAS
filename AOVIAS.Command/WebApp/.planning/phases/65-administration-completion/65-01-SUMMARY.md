# Phase 65 — Stage 6 — Enterprise Administration Completion

**Status:** Implementation complete for the phases that were buildable. Build green,
400/401 unit tests pass (the one failure is pre-existing and unrelated).
**human_needed:** rendered-UI signoff for the Configuration Center, and a decision on the two scope
corrections in §2.
**Repositories touched:** `WebApp`, plus one data-only SQL seed script. No backend code, no schema
changes.

---

## 1. Headline

| | |
| --- | --- |
| Configuration domains added | **20** (Risk, Approval, Notification, SLA, Evidence, Document, Dashboard) |
| Total categories now administrable | **48** — 28 Audit, 20 Administration |
| Platform-default values added | **98** |
| Configuration Center | **Built** — one editor, all 48 domains, zero per-domain code |
| Client write path | **Built** — save and restore against the existing endpoints |
| Hardcoded risk rules removed | 5×5 scale (×2 components, ×4 form inputs), score formula (×3), band ladder |
| Tests | **400 passing** (from 343), 57 added |

---

## 2. Two corrections to the locked state — read this first

The brief lists **"✓ Configuration Center Foundation"** and **"✓ Generic Configuration Components"**
as complete, and Phase 8 accordingly says *"expose every domain inside the Configuration Center …
using the same generic editor"*.

**Neither existed.** I reported this in Stage 5C.2 as the critical-path condition: the Angular client
had no write path (`ConfigurationApiService` was read-only) and there was no configuration editor —
the Administration shell's risk, approvals and notifications tabs were `comingSoon` placeholders. The
risk placeholder page said so in prose: *"Today these are fixed at 5×5 with score = likelihood ×
impact"*.

So Phase 8 was not "expose domains in the existing editor". It was **build the editor**. I built it,
because without it the other nine phases produce catalogs no administrator can reach — data that
looks like progress and changes nothing.

The second correction is smaller but matters for expectations: **most of the new domains have no
consumer yet.** Registering a category makes it *administrable*; it makes it *behavioural* only when
something reads it. §5 states this per domain rather than averaging it away.

---

## 3. What was built

### The write path

`ConfigurationApiService` gained `saveOverride` and `restoreDefault`, against the `PUT` and `DELETE`
endpoints that already existed on `InternalConfigurationController`. Two design points:

- **Writes do not swallow failures.** Reads degrade to the seed so a screen stays usable; a failed
  *write* must reach the administrator, because silently discarding an edit is worse than an error.
- **A write response is the whole re-resolved category**, not an acknowledgement, so a save
  propagates to every bound screen with no reload. `applyCategory` feeds it straight into the store.

The subtle case is restore. On a read, "resolved to nothing" and "unreachable" both mean *use your
seed*. On a **write** they are opposite outcomes — an empty category is the successful result of
withdrawing the last override — so the write mapper preserves it and `applyCategory` returns that
category to its platform seed. Collapsing them would have left a restored category still showing the
values the administrator just withdrew, most visibly for the workflow categories whose default lives
in code rather than rows.

### The Configuration Center

One page, one route, 48 domains. The category list is generated from the registry, so a domain added
to a catalog module appears with **no routing, no component and no CRUD** of its own — the property
that makes "no duplicated CRUD, no dedicated pages" structural rather than a convention.

`ConfigurationAdminService` holds the writes, deliberately separate from
`PlatformConfigurationService`: that service is injected by ~30 consumer screens and should stay
read-only from their point of view. A grid must not be able to write configuration by accident.

**One trade-off, stated plainly.** Attributes are edited as JSON. They are domain-specific —
`minScore` on a risk level, `completeWhen` on a workflow stage, `retentionMonths` on a policy — and a
per-attribute form needs a schema per category, which is exactly the per-domain code the page exists
to avoid. JSON is validated as the administrator types, and Save is disabled while it is malformed.
This is an administrator's tool, not an end-user's. A typed editor per domain is possible later; it
would be seven more components.

### Risk (Phase 1) — the domain with real rules to remove

The only new domain with existing hardcoded logic, and all of it is gone:

| Was | Now |
| --- | --- |
| `impacts = [1,2,3,4,5]`, `likelihoodsDesc = [5,4,3,2,1]` in the matrix page | `RiskLikelihood` / `RiskImpact` scales |
| `min="1" max="5"` on four form inputs | bound to the configured scale |
| `likelihood * impact` in three places | `RiskConfigurationService.score` |
| `if (score >= 20) …` band ladder | `minScore` on each `RiskLevel` |
| `switch (band)` for chip colour | `tone` attribute |

Removing two likelihood values now makes the matrix 3×5 — axes, cell scores, form bounds and bands
all follow, with no code change. There is a test for exactly that.

`RiskConfigurationService` follows the `AuditWorkflowEngine` shape: a thin domain service over the
store so the matrix, the form and the CAPA panel cannot disagree about where "High" begins. They
previously had three separate opinions.

**What is *not* configurable, and why:** the operator. That score is likelihood *multiplied* by
impact stays in code. Making it configurable means an expression language for a rule nobody has asked
to vary, and the alternative organizations actually use — an additive matrix — changes the meaning of
every threshold with it. If that need arrives it is a `scoring` attribute, not a redesign.

---

## 4. Engineering rules — how each was met

| Rule | How |
| --- | --- |
| No architectural redesign | No new engine, store, or loading path. Two new catalog modules and one registry that concatenates them. |
| Reuse the Configuration Engine | All 20 domains are `ConfigurationCatalog`s in the existing store, loaded by the existing loader. |
| Reuse generic editors | One editor for all 48 domains. |
| One implementation pattern | `RiskConfigurationService` mirrors `AuditWorkflowEngine`, the pattern established in Stage 5C.1. |
| No duplicated CRUD / UI / APIs | One admin service, one page, one route, one pair of endpoints. |

The only genuinely new surface is the editor page, which the brief assumed existed.

---

## 5. Domain status — administrable vs behavioural

| Phase | Domain | Categories | Consumer today |
| --- | --- | --- | --- |
| 1 | Risk | 4 (+ RiskLevel extended) | **Migrated** — matrix page, risk form, CAPA panel |
| 2 | Approval | 2 | **None.** Approvals tab is a placeholder; no surface requests an approval. |
| 3 | Notification | 3 | **Partial.** A notifications admin module exists; its pages do not read these yet. |
| 4 | SLA | 3 | **Partial.** `SlaStatus` was already migrated and is read by the CAPA surfaces; the clock is computed server-side. |
| 5 | Evidence | 3 | **Partial.** `FindingAttachmentType` was already migrated; `EvidenceType` and retention are not read yet. |
| 6 | Document | 3 | **None.** The documentation module renders records as given. |
| 7 | Dashboard | 2 | **None.** The dashboards render fixed tile sets. |

Eleven of the twenty are declared ahead of their consumers. That is not wasted — an administrator can
set them, the seed script publishes them, and the surfaces that read them are ordinary follow-ups
against a stable contract. But it would be dishonest to describe the platform as "fully
administrator-managed" on the strength of catalogs that nothing consumes yet.

**Phase 9 (consumer migration) is therefore partially complete:** Risk in full, the rest limited to
what already had hardcoded rules. There were no remaining enums, label maps or switch statements to
remove in the other six domains — the code for those features does not exist yet.

---

## 6. Verification

| Check | Result |
| --- | --- |
| New tests | **57**, all passing |
| Full suite | 400 passing, 1 pre-existing failure |
| Prior waves' specs | passing, unchanged |
| `tsc --noEmit` | clean |
| `ng build` (AOT) | succeeds |
| ESLint on touched files | 5 errors, all pre-existing `no-output-native`; none introduced |

Coverage of the new work: every one of the 20 domains is tested for platform default *and* tenant
override; save propagation without reload; save failure surfacing rather than discarding; restore to
platform default including the emptied-category case; risk matrix resize from configuration; band
boundary move from configuration; and tenant isolation across the new domains.

**Not verified:** the rendered Configuration Center. The Administration routes sit behind auth, there
is no backend in this environment, and I will not enter credentials. The page compiles under AOT and
its service layer is tested; nobody has watched it render or saved a value end-to-end.

---

## 7. Open items

| Id | Severity | Item |
| --- | --- | --- |
| DEF-CFG-004 | Medium | **Still open** from 5C.2 — create-form defaults bypass configuration. Disabling a value does not stop it being the form's default. |
| DEBT-CFG-011 | Medium | Eleven categories with no consumer (§5). |
| DEBT-CFG-012 | Low | Attributes edited as JSON rather than typed fields. |
| DEBT-CFG-013 | Low | No permission guard on the Configuration Center route — it inherits the Administration shell's. Worth an explicit `permissionGuard` before release. |
| DEBT-CFG-007 | Medium | Evidence status labels, carried from Wave 1. `EvidenceType` now exists as a home for the migration. |

**No Critical or High defects.**

---

## 8. Seed data

`2026-08-03_SeedAdministrationConfiguration.sql` — 20 categories, 98 values, generated from the
catalogs, idempotent, data-only.

Applying it is what makes these domains **editable**. Without it the client behaves identically —
it runs on the same values as its in-code seed — but the Configuration Center has nothing to write
against for these categories.

Three scripts now exist and all should be applied in order:
1. `2026-08-03_SeedAuditConfigurationCatalogs.sql` (carries the required `AuditType` metadata repair)
2. `2026-08-03_SeedAuditWorkflowLifecycle.sql`
3. `2026-08-03_SeedAdministrationConfiguration.sql`

---

## 9. Against the success metric

> *Business administrators — not developers — can manage platform behaviour through the
> Administration Workspace without requiring code changes.*

**True for the 37 categories that have consumers**, and now genuinely reachable: as of this wave
there is a screen to do it in and a write path behind it. An administrator can rename a status,
resize the risk matrix, move a band, reorder the lifecycle, restrict a surface, and restore any of it
— all without a release.

**Not yet true for the platform as a whole**, for two reasons that are worth keeping separate from
each other. Eleven of the new categories are declared ahead of the features that will read them. And
nobody has yet watched an administrator save a value in a browser; the evidence stops at the service
boundary, as it did in the previous wave.

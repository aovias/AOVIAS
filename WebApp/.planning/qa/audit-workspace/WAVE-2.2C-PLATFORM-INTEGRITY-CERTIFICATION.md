# Wave 2.2C — Write Path Hardening & Platform Integrity Certification

**Date:** 2026-08-02 · **Repo:** `ComplaincePlatform` (uncommitted)
**Suite:** `Passed! Failed: 0, Passed: 43, Skipped: 2, Total: 45` · All baselines held

## Headline: the cross-tenant **write** vulnerability is closed

Category A was not a hygiene backlog. Reading the actual code showed a concrete exploit:

```csharp
// PatchDeficiencyCapaCommandHandlers / PatchDeficiencyRcaCommandHandler — BEFORE
var deficiency = await _db.ComplianceDeficiencies
    .IgnoreQueryFilters()                                   // tenant filter off
    .FirstOrDefaultAsync(d => d.Id == command.DeficiencyId, ct)   // Id ALONE
    ?? throw new InvalidOperationException(...);
// … mutate … SaveChangesAsync()
```

**Load by GUID with filtering disabled, then mutate and persist.** Any authenticated caller holding
another tenant's deficiency id could write into that tenant's record — corrective actions, preventive
actions, effectiveness reviews, root-cause analysis and remediation. This is the same mechanism that
produced the 88 duplicate deficiencies in the first place.

**All eight such write bypasses are removed.** They now fail closed: the row is not found and the
load throws — which is also the correct answer to a probe, since it does not reveal whether the id
exists in another tenant.

---

## 1. Category A Write Matrix (Phase 1)

| Handler / Service | Sites | Scope before | Risk | Action |
|---|---|---|---|---|
| `PatchDeficiencyCapaCommandHandlers` (corrective, preventive, effectiveness) | 3 | **Id only**, filters off | **Critical** | ✅ Removed |
| `PatchDeficiencyRcaCommandHandler` (RCA, remediation) | 2 | **Id only**, filters off | **Critical** | ✅ Removed |
| `EvaluateObligationDeficienciesCommandHandler` | 1 | **Id only**, filters off | **Critical** | ✅ Removed |
| `FindingPromotionService` (obligation resolution ×2) | 2 | One scoped hospital+framework **without organization**; one org-scoped | **High** | ✅ Removed |
| `UpdateObligationFromAssessmentCommandHandler` | 2 | `o.OrganizationId == command.OrganizationId` | Medium | ⚠️ **Reclassified A → B** |
| `UpdateObligationJourneyProgressCommandHandler` | 1 | `o.OrganizationId == command.OrganizationId` | Medium | ⚠️ **Reclassified A → B** |

### Correction to Wave 2.2B.3.4

I labelled six services "Category A write paths running unfiltered". Reading them properly, **two
already carry an explicit `OrganizationId` predicate** — their bypass is redundant, not unguarded.
They run on onboarding/assessment-rebuild paths where tenant context may legitimately be absent, so
removing the bypass needs that path verified first. Reclassified to B with the reasoning recorded in
the manifest.

**True Category A count was 8 sites across 4 services, not 6 services.** All 8 are now closed.

## 2. Bypass reduction

| Wave | Count |
|---|---|
| 2.2B.3.3 start | 81 |
| 2.2B.3.3 end | 73 |
| **2.2C end** | **66** |

Category A is now **empty**. Everything remaining is B (redundant/explicit scope), C (genuinely
cross-tenant), or D (bootstrap) — and all 66 are frozen behind the CI guardrail.

## 3. Historical Data Integrity (Phase 3)

**Deliberately unchanged: 176 rows, 88 corrupt-tenant duplicates still present.**

Not an omission — a judgement, and I want it on the record:

1. **The duplicates are already invisible.** With tenant filtering enforced, dashboard and workspace
   both read **88**. Deleting them changes no number a user sees.
2. **The engineering rules say prefer archive over delete when uncertain**, and uncertainty remains:
   `(ObjectiveElementId, Title)` is not unique within a batch, so 6 closed rows still cannot be
   paired 1:1 (Wave 2.2A §4.2). The cleanup script exists, is idempotent, quarantines before
   deleting, and **refuses to run** while that ambiguity stands. It refused again in this wave.
3. **Deleting now would remove the evidence** needed to prove the duplicates are duplicates.

Constraints already in place from earlier waves: `CK_Findings_RaisedBy_Range (1..8)` and the
`Enum.IsDefined` domain guard. The `CK_ComplianceDeficiencies_TenantNotHospital` constraint ships
with the cleanup script and applies when it runs.

**Prevention is done, which is what matters:** the bootstrap path that created the duplicates was
fixed in Wave 2.2B (Gateway derives organization from the token; the web app no longer substitutes a
hospital id). The corrupt rows cannot be recreated.

## 4. Runtime Validation (Phase 4)

| Probe | Before | After | Result |
|---|---|---|---|
| DCC dashboard | 88 | 88 | ✅ |
| DCC workspace | 88 | 88 | ✅ Consistent |
| Findings / Sessions / Schedule / Report | 27 / 16 / 15 / 24 | identical | ✅ |
| Integration suite | 43 / 2 / 0 | 43 / 2 / 0 | ✅ |

### Write path verification

| Test | Result |
|---|---|
| `PATCH /deficiencies/{id}/status` (handler **not** modified) | **400 — "Invalid lifecycle transition Identified → InProgress"** |
| `PATCH /deficiencies/{id}/corrective-action` (handler modified) | 500 at `SaveChanges` |

The control proves the pipeline: an unmodified handler **loaded a tenant-filtered deficiency, applied
business rules, and returned a clean domain validation error**. Tenant-filtered loading works for
writes.

### 🆕 NEW-07 [High] — `corrective-action` PATCH fails at persistence

```
500 INTERNAL_ERROR — "An error occurred while saving the entity changes."
```

**Not attributable to this wave's change.** My edit affects only which rows are *loadable*; the row
loaded successfully and execution reached `SaveChangesAsync`. The failure is downstream, in that
handler's persistence step.

I could not establish a pre-change baseline: writes were blocked by the expired licence for most of
this programme, so this path has likely never been exercised. **Reported as a newly discovered
defect requiring investigation, not claimed as either fixed or pre-existing.** First thing to look
at in the next slice — probably the CAPA number sequence store.

## 5. Platform Integrity Certification (Phase 5)

### Tenant isolation

| Boundary | State |
|---|---|
| Gateway — client-supplied `organizationId` | ✅ Token-authoritative; spoofing rejected and logged |
| Gateway → downstream propagation | ✅ All 4 proxy paths |
| ComplianceService read isolation | ✅ Mandatory filters, fail closed |
| **ComplianceService write isolation** | ✅ **Category A closed** |
| Query-level governance | ✅ CI-enforced, 66 frozen and classified |
| Observability | ✅ Fail-closed telemetry |
| SignalR | ⚠️ Incidentally safe (globally unique person ids); not deliberately scoped |
| Background jobs | ❌ No execution context |
| CommunicationService | ⚠️ Zero tenant references — uninvestigated |

### Security findings

| Severity | Open |
|---|---|
| Critical | **0** |
| High | **3** — NEW-07 (write 500), TD-15 (Communication), 24 unprobed Gateway endpoints |
| Medium | 4 — SignalR namespacing, background jobs, no CI, duplicate data |

## 6. Technical Debt Register (final)

| ID | Item | Status |
|---|---|---|
| TD-01 | Query bypasses | 81 → **66**, Category A empty, CI-frozen |
| TD-04 | 88 duplicate rows | Open by design — filtered, not deleted |
| TD-13 | Background job contexts | Open |
| TD-14 | SignalR namespacing + hub harness | Open |
| TD-15 | CommunicationService tenant assessment | Open |
| TD-16 | Governance mechanism | ✅ Closed |
| TD-17 | No CI pipeline | Open — **the guardrail has nowhere to run** |
| TD-18 | 🆕 NEW-07 corrective-action persistence failure | Open |

## 7. Certification statement — with limits

**What I can certify**, on runtime evidence:

- No known cross-tenant **read** path in ComplianceService.
- No known cross-tenant **write** path in ComplianceService.
- Tenant scope fails closed and is observable when it does.
- Query-level bypasses cannot grow without a reviewed diff.
- Business metrics are consistent across dashboard and workspace (88 = 88).
- 45 automated tests pass, including cross-tenant rejection against a **real** second organization.

**What I cannot certify:**

- **Full multi-tenant isolation.** Two of 45 tests remain skipped for want of a second tenant's
  credentials. Proving tenant B's rows are absent from tenant A's results requires logging in as
  tenant B, and I have not been able to.
- **Background jobs and SignalR.** Neither is context-aware; SignalR is safe by accident of
  globally-unique ids, not by design.
- **CommunicationService.** Zero tenant references — either genuinely tenant-agnostic or quietly
  unscoped. Unknown, and the difference matters.
- **That the guardrail will hold.** It passes locally; **no CI pipeline exists to run it.**

**The Platform Integrity Program should not be marked formally closed.** The architecture is complete
and enforced, and the severe findings are fixed — but three of the original acceptance criteria
(background jobs, SignalR, CI enforcement) are unmet, and certification resting on a control nobody
executes is not certification.

## 8. Recommendation

Ready to return to Stage 5 feature work, with four items carried as tracked debt:

1. **Investigate NEW-07** — a live 500 on a CAPA write path.
2. **Stand up a CI pipeline** — the single highest-leverage item. The guardrail, 45 tests and the
   fail-closed telemetry all exist; nothing runs them automatically.
3. **Obtain `qa.org.admin@aovias.test`** — unlocks the last 2 tests and makes future isolation work
   provable rather than argued.
4. **Investigate CommunicationService** before building anything further on notifications.

The duplicate data can stay as it is indefinitely without affecting a single displayed number. I
would not spend a wave deleting it.

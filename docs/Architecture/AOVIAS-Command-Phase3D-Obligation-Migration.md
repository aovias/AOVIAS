# AOVIAS Command — Phase 3D Obligation Migration

**Phase:** 3D Gate A dry-run PASS. Gate B **EXECUTED** on local SQL Express (2026-08-31).  
**Status:** Gate B PASS on the authorized development database.  
**Environment:** `localhost\SQLEXPRESS` / database `Compliance`. Organization master: `Organization.dbo.Hospitals`.  
**Gate A commit:** `b4f4d489d35e2cc3ac857e292050f6deada17411`  
**Backup:** `C:\Program Files\Microsoft SQL Server\MSSQL17.SQLEXPRESS\MSSQL\Backup\Compliance_Phase3D_GateB_20260831.bak` (COPY_ONLY)  
**Cloud/production DML:** **NO** (local development Compliance only)

Contracts: [Phase 3C identity](./AOVIAS-Command-Phase3C-Canonical-Compliance-Identity.md), [Phase 3B](./AOVIAS-Command-Phase3B-Data-Reconciliation.md).

Artifacts:

| File | Role |
| --- | --- |
| `scripts/dev/phase3d-twin-mapping.csv` | 1006 TwinId → CanonicalId |
| `scripts/dev/phase3d-missing-hospital-obligation-ids.csv` | 650 obligation IDs |
| `scripts/dev/phase3d-dry-run-readonly.sql` | SELECT counts |
| `scripts/dev/phase3d-obligation-migration.proposed.sql` | Historical Gate A stub |
| `scripts/dev/phase3d-gateb-execute.ps1` | Gate B validate/backup/migrate |
| `scripts/dev/phase3d-gateb-evidence/` | Before/after counts, deficiency remap log |

---

## 1. Executive summary

Canonical survivor for every twin pair is the **MATCH** row: same `HospitalId + FrameworkId + ObjectiveElementId`, `OrganizationId = Organization.Hospitals.OrganizationId`. The twin is the row with `OrganizationId = HospitalId`.

- **965 IDENTICAL** pairs: operational statuses match; **zero** deficiencies on either PK; MATCH org already correct. **Safe to delete twin IDs only**, after approval. Does **not** by itself allow the new unique index (41 duplicates remain).
- **41 DIVERGED** pairs: MATCH is usually later/`UpdatedAt` and higher readiness, but **40 pairs have deficiencies on both PKs** (parallel trees). One pair (hospital B) has no deficiencies and MATCH strictly dominates. One pair has **twin richer** than MATCH. **BUSINESS DECISION REQUIRED** — do not delete/merge these 41 in Gate A.
- **650** missing-hospital obligations: still **zero** downstream hospital-scoped rows. Delete is a **separate approval**; **not required** to add the new unique index (their HospitalIds do not collide with MATCH keys).

There is **no SQL FK** to `OrganizationComplianceObligations`. The only persisted obligation-id column found is `ComplianceDeficiencies.ComplianceObligationId` (soft GUID). Outbox events may mention obligation ids in payloads; they are historical, not remapped.

---

## 2. Canonical identity

```text
Business key = (HospitalId, FrameworkId, ObjectiveElementId)
PK           = Id (unchanged)
Survivor     = MATCH row (OrganizationId = Org.Hospital.OrganizationId)
Twin         = OrganizationId = HospitalId
```

---

## 3. Current data state (2026-08-31 dry-run)

| Metric | Count |
| --- | ---: |
| Total obligations | 2662 |
| Duplicate (Hospital, Framework, OE) keys | 1006 |
| Twin pairs | 1006 |
| Identical operational status | 965 |
| Diverged operational status | 41 |
| Missing-hospital obligations | 650 (325 + 325) |
| MATCH survivors already correct org | 1006 / 1006 |

---

## 4. Twin mapping methodology

Join:

```text
Twin  WHERE OrganizationId = HospitalId
      AND HospitalId IN Organization.Hospitals
MATCH WHERE same HospitalId, FrameworkId, ObjectiveElementId
      AND OrganizationId = Hospitals.OrganizationId
```

CSV columns: `TwinObligationId, CanonicalObligationId, HospitalId, FrameworkId, ObjectiveElementId, StateClass`.

Survivor is **never** min(Id) / max(CreatedAt). It is always MATCH because that is the only row with a valid tenant id. Operational **state** on MATCH is not always strictly better (see §6).

---

## 5. 965 identical pairs

Proven: `ImplementationStatus`, `AssessmentStatus`, `JourneyStatus`, `EvidenceStatus`, `VerificationStatus` equal.

Deficiencies: **0** twin-only, **0** canonical-only, **0** both (all 177 deficiencies sit on diverged pairs).

Tasks via `SourceDeficiencyId`: **0** on these 965.

**Proposed Gate B (965 only):** `DELETE FROM OrganizationComplianceObligations WHERE Id IN (IDENTICAL TwinIds)` — 965 rows. MATCH rows untouched. Expected remaining obligations: `2662 - 965 = 1697` (still includes 41 twins + 650 missing). Duplicate business keys remaining: **41**.

---

## 6. 41 divergent pairs

Pattern (hospital A `d9f5554a-…`, 40 pairs): twin created `2026-06-20 05:25`, MATCH `10:31` same day; twin last updated ~`2026-07-09`; MATCH ~`2026-08-10`. MATCH typically has higher `ReadinessScore` and later evidence/verification enums. **Each of 40 pairs has deficiencies on both PKs** (often 1+1 or 4+4). Twin-backed deficiencies have `OrganizationId = HospitalId`.

| Exception | Twin | Canonical | Notes |
| --- | --- | --- | --- |
| Hospital B `f17fc194-…` | `624F02F7-…` Impl=0 Ready=0 | `B2179F26-…` Impl=Implemented(2) Ready=100 | No deficiencies. MATCH strictly richer. Identity survivor = MATCH. Overlay unnecessary. |
| Twin richer | `8F624C8B-…` Ready=40 Ev=Verified(3) | `F3D7CD49-…` Ready=0 Ev=Missing | MATCH CreatedAt later but **less** progressed. Copying MATCH-only would **lose** twin state. **BDR.** |
| Dual deficiency trees | 40 hospital A pairs | both PKs | Remapping twin `ComplianceObligationId` → MATCH **keeps 2× deficiencies** on one obligation (index is **not** unique). Merging/discarding defs is **BDR**. |

**Do not invent a status overlay.** Proposed identity survivor is still MATCH; **merged operational state and deficiency trees are BDR.**

---

## 7. 650 missing-hospital records

IDs: `scripts/dev/phase3d-missing-hospital-obligation-ids.csv`.

Reconfirmed 0: journeys, tasks, evidence, deficiencies (by hospital or obligation id), snapshots, dashboard, analytics, risks, CAPA.

Provenance: Phase 2 `f653e630-…` (325), accidental `:5016` `edb9321a-…` (325).

**Recommended:** delete/archive **only after approval**. Independent of 965 twin delete.

---

## 8. Dependency graph

```text
OrganizationComplianceObligation.Id
  └── ComplianceDeficiencies.ComplianceObligationId  (soft; 177 rows on twin hospitals’ pairs)
        └── ComplianceTasks.SourceDeficiencyId       (1 task → MATCH deficiency)
Evidence / Findings / CAPA / SLA / Maturity / Snapshots
  └── scoped by HospitalId (or journey), NOT obligation Id
OutboxMessages.Payload
  └── may contain historical obligation GUIDs; no FK
```

SQL FKs referencing `OrganizationComplianceObligations`: **none**.

---

## 9. FK remapping plan

| Dependent | Action | Blind update? |
| --- | --- | --- |
| Deficiencies on IDENTICAL twins | N/A (count 0) | — |
| Deficiencies on DIVERGED twins (88) | Remap to CanonicalId **or** keep until trees merged | **No** — 40 pairs already have MATCH deficiencies |
| Tasks | Follow deficiency Id (unchanged if deficiency kept) | No extra remap |
| Twin-backed deficiency `OrganizationId` | Should become true org if row kept | Separate UPDATE; BDR with 41 |
| Evidence | No obligation FK | None |

---

## 10. OrganizationId correction plan

MATCH survivors: **0 updates** (already `ExpectedOrgId`).

After twin delete: no surviving `OrganizationId = HospitalId` except **650** missing-hospital rows (org=hospital=fake id) until those are deleted.

Deficiency org repair: **88** rows currently `OrganizationId = HospitalId` (twin-backed). Not in 965 batch.

---

## 11. Proposed schema migration (Gate B+, after 41 resolved)

1. Approved 965 twin DELETE.  
2. Resolve 41 (BDR) + remap/merge deficiencies.  
3. Optional 650 delete.  
4. Verify `COUNT(*) = COUNT(DISTINCT HospitalId, FrameworkId, ObjectiveElementId)` on remaining rows.  
5. Drop `UX_OrganizationComplianceObligations_OrgOE`.  
6. Add unique `(HospitalId, FrameworkId, ObjectiveElementId)`.  
7. Change `ObligationSynchronizationService` / `UpsertBatchAsync` skip key to hospital+FW+OE (otherwise enroll can still collide on org+OE unique if that index remains, or recreate logic bugs). **Code change is Gate B, not this dry-run.**  
8. API regression (journey list count = catalog OE count, not 2×).

**Do not** create the new unique index while 41 duplicates remain.

---

## 12. Dry-run results

**Before:** 2662 obligations; 1006 dup keys; 1006 twins; 650 missing; 177 deficiencies; 1534 tasks; 473 evidences.

**Mapping:** 1006 Twin→MATCH; 965 IDENTICAL; 41 DIVERGED.

**Simulated after 965-only delete:** obligations 1697; dup keys 41; missing 650; deficiencies 177; tasks 1534.

**Simulated after full approved (965+41 merge + 650 delete):** obligations **1006**; dup keys **0**; missing **0**; deficiencies **177** if none deleted (or fewer if BDR discards duplicates); tasks **1534**.

**Orphans:** 0 if twin defs remapped or twins with defs not deleted.

---

## 13. Data conservation

| Entity | Before | Affected by 965 delete | Expected after 965 | After full (if defs kept) |
| --- | ---: | ---: | ---: | ---: |
| Obligations | 2662 | −965 twins | 1697 | 1006 |
| Deficiencies | 177 | 0 | 177 | 177 (unless BDR merges) |
| Tasks | 1534 | 0 | 1534 | 1534 |
| Evidences | 473 | 0 | 473 | 473 |
| Findings / CAPA / SLA / maturity | 35 / 17 / 8 / 4 | 0 | same | same |

No MATCH obligation, journey, or hospital master row is deleted in the 965 plan.

---

## 14. Rollback strategy

- 965 DELETE: `BEGIN TRAN`; delete listed TwinIds; verify counts; `COMMIT` or `ROLLBACK`. Twins have no deficiency FKs — restore only from backup/tran log if committed.  
- 41/650: do not start without approved mapping.  
- Index drop/add: transactional DDL on SQL Server in one batch with verification.  
- Recovery: restore `Compliance` DB from pre-change backup (standard for this local Express instance).

---

## 15. Business decisions required

1. **41 diverged pairs — operational state:** keep MATCH as-is, overlay twin where twin is richer (`8F624C8B-…`), or field-level merge.  
2. **40 dual deficiency trees:** keep both under MATCH, merge by type, or archive twin-side defs.  
3. **88 twin-deficiency OrganizationId** repair to true org.  
4. **650** artifact/accidental obligation delete vs retain as forensic.  
5. **Sync/query code** change to hospital+FW+OE in the same release as the new unique index.

---

## 16. Execution plan

**Now:** Gate A complete. Stop.

**Next (explicit approval):** (optional) 965 IDENTICAL twin DELETE in one transaction.  
**Then:** 41 BDR.  
**Then:** 650 if approved.  
**Then:** constraint swap + sync code.

Command that would run after approval (not run):

```text
sqlcmd ... -i scripts/dev/phase3d-obligation-migration.proposed.sql
```

That file currently **THROWs** and performs no DML.

---

## 17. Post-migration verification

- Dup keys = 0.  
- Every remaining real-hospital obligation `OrganizationId` = Org hospital org.  
- Journey `GET .../obligations?hospitalId=&frameworkId=` count = distinct OE for that FW (503 for NABH Full), not 1006.  
- Two hospitals same FW can both have OE X.  
- Re-enroll does not insert a second row (after sync code change).  
- Deficiency count not dropped accidentally.  
- Re-run Phase 3B classifiers.

---

## 18. Gate B execution (2026-08-31)

Earlier the same day Gate B stopped before DML until human decisions A/A/approved/delete were recorded. Those decisions were then applied.

**DESIGN / DRY RUN:** sections 1–17 (unchanged as analysis).  
**EXECUTED MIGRATION:** yes, one SQL transaction after COPY_ONLY backup.  
**POST-MIGRATION VERIFICATION:** SQL postconditions + unit tests (below).

Approved decisions applied:

| Topic | Applied rule |
| --- | --- |
| 41 operational state | **A** — MATCH fields unchanged (no overlay) |
| Dual deficiency trees | **A** — remap twin `ComplianceObligationId` → MATCH; keep both trees |
| 88 deficiency OrganizationId | `Organization.Hospitals.OrganizationId` via `HospitalId` |
| 650 missing-hospital | DELETE CSV IDs only |
| 1006 twin obligation rows | DELETE after remap (965 identical + 41 identity-only twins). Required to reach 1006 remaining rows and unique canonical keys. MATCH rows not deleted. |

Preflight counts matched Gate A (2662 / 1006 dup keys / 177 defs / 1534 tasks / 473 evidence / 35 findings / 17 CAPA / 8 SLA / 4 maturity). Mapping 1006 / 965 IDENTICAL / 41 DIVERGED unchanged. 88 twin-backed deficiencies unchanged in total.

Deviation vs Gate A **narrative** (not vs approved 88 remap): Gate A said IDENTICAL pairs had 0 deficiencies. Live split was **83** twin defs on DIVERGED-labeled pairs and **5** on IDENTICAL-labeled pairs (two status-equal pairs with parallel trees; 0 tasks on those 5). All **88** were remapped under decision 2. No extra policy invented.

41 MATCH obligation rows were not UPDATEd.

After commit:

| Metric | Result |
| --- | ---: |
| Obligations | 1006 |
| Duplicate canonical keys | 0 |
| Missing-hospital | 0 |
| OrganizationId = HospitalId | 0 |
| Hospital/org mismatch | 0 |
| Orphan deficiencies / tasks | 0 |
| Deficiencies / tasks / evidence / findings / CAPA / SLA / maturity | 177 / 1534 / 473 / 35 / 17 / 8 / 4 |
| Hospital A / B obligation counts | 503 / 503 (same framework; shared OE ids coexist) |

Indexes: dropped `UX_OrganizationComplianceObligations_OrgOE`; created `UX_OrganizationComplianceObligations_HospitalFrameworkOE`. EF migration `20260831111209_Phase3D_CanonicalObligationUniqueKey` stamped in `__EFMigrationsHistory`.

Sync: `ObligationSynchronizationService` / `UpsertBatchAsync` / `GetByKeyAsync` use `(HospitalId, FrameworkId, ObjectiveElementId)`.

Tests: `ObligationCanonicalIdentityTests` + `HospitalAccessAuthorizerTests` + `EnrollHospitalAccessTests` — 18 passed.

Live HTTP: `http://localhost:5016/health` returned 200 (pre-existing process; **not** used for enroll — old binary). Cross-hospital JWT UI regression was not re-run against a newly built host.

**40 dual deficiency trees were preserved under the canonical MATCH obligations by approved decision. Their semantic deduplication/merge policy remains unresolved and is deferred to a later business-rule phase.**

Rollback: not required (transaction committed after postconditions). Restore path: the COPY_ONLY bak above.

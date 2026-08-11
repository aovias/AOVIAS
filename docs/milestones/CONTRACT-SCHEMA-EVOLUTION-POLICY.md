# Infrastructure Contract Schema Evolution Policy

## Reference document — Sprint M2.2 (D10)

Status: living reference. This document governs every future infrastructure contract schema
revision (v3, v4, …); it is not a one-time sprint artifact. Update it whenever
`Aovias.Deployment.Contracts.Migration.ContractSchemaVersions` gains a new version.

---

## 1. Schema Lifecycle

Every schema version passes through the same five stages. A version's stage is not recorded
anywhere in code — it is a statement about the platform's current posture toward that version,
tracked here.

| Stage | Meaning | Where it lives |
|---|---|---|
| **Proposed** | A new domain need requires a wire-shape change no existing version can express. | Design discussion; not yet in `ContractSchemaVersions`. |
| **Current** | The schema Publishing emits by default. Exactly one version holds this stage at a time. | `ContractSchemaVersions.Current`. |
| **Supported (legacy)** | No longer emitted, but the reader accepts it — either natively or via a registered `IInfrastructureContractMigrator`. | `ContractSchemaVersions.Supported`; a migrator keyed by that version in `DeploymentPackageContractReader`. |
| **Deprecated** | Still supported, but scheduled for removal; reading it should surface a warning (not yet implemented — see Technical Debt in the M2.2 report). | Documented here, ahead of removal. |
| **Removed** | The reader rejects it outright (`ICA010`, same as an unrecognized version). Packages this old must be republished from source, not migrated. | Removed from `ContractSchemaVersions.Supported`; its migrator deleted. |

As of Sprint M2.2: schema v2 is **Current**. Schema v1 (M2.0/M2.1) is **Supported (legacy)** via
`InfrastructureContractV1ToV2Migrator`. No version has been deprecated or removed yet.

---

## 2. Migration Strategy

**Principle:** a migrator converts the *previous* schema to the *current* one, in memory, at
read time. It never rewrites the package on disk (contracts are immutable once published) and
it never runs at publish time (Publishing always emits the current schema natively).

**Chaining, not a combinatorial matrix.** When schema v3 is introduced, do not write a v1→v3
migrator. Write a v2→v3 migrator and have the reader chain v1→v2→v3 through the existing v1→v2
migrator. `DeploymentPackageContractReader` already resolves migrators by exact source version
in a dictionary (`Migrators[schemaVersion]`); chaining support is the only reader change a v3
introduction requires — the compatibility evaluator, the ICA-coded failure modes, and the
round-trip-then-migrate sequencing described in the M2.2 report all stay as they are.

**What a migrator may and may not do:**
- May: copy domain fields across unchanged, compute new derived metadata (e.g. a content hash),
  leave genuinely unrecoverable fields at an honest empty/default value.
- May not: read the wall clock, generate a new identifier, or otherwise produce two different
  outputs for the same input. Two independent reads of the same immutable package (Runtime Agent
  and DRV, or two DRV runs) must migrate to byte-identical results — this is why
  `ContractMigrationMetadata` in this codebase carries no timestamp. See §4.

**Where migrators are registered:** as entries in the private `Migrators` dictionary inside
`DeploymentPackageContractReader`. A future revision may promote this to constructor-injected
DI registration if the number of registered migrators grows enough to warrant it; that is an
implementation detail this policy does not mandate either way.

---

## 3. Compatibility Guarantees

`Aovias.Deployment.Contracts.Migration.InfrastructureContractCompatibility.Evaluate` is the one
and only place this decision is made. Its contract:

| Publisher schema | Runtime's max supported schema | Result |
|---|---|---|
| Equal to runtime's max | — | **Pass** — read natively, no migration. |
| Older than runtime's max, and a migrator is registered for it | — | **AutoMigrate** — read via the legacy shape, then migrate in memory. |
| Newer than runtime's max | — | **Fail** — this build cannot safely interpret a schema it postdates. |
| Zero, negative, or otherwise unrecognized | any | **Fail** — treated as "Unknown" regardless of the runtime's ceiling. |
| Older than runtime's max, but no migrator registered for it | — | **Fail** — a supported-version list with a gap in it is a configuration bug, not a silent success. |

This table is exhaustive: every `(publisher, runtime)` pair falls into exactly one row. Any
future schema version is added by extending `ContractSchemaVersions` and registering a migrator
— the evaluator's logic does not change.

**What "runtime" means today:** this codebase ships exactly one reader
(`DeploymentPackageContractReader`), whose ceiling is always `ContractSchemaVersions.Current`.
The "Publisher v2 / Runtime v1" row is therefore realized today as a parameterized unit-test
scenario (an older, hypothetical reader ceiling passed directly into `Evaluate`) rather than a
second shipped binary — see the M2.2 report §5/§7 for why, and for what would need to exist for
that row to be exercised by an actual older Runtime Agent build.

---

## 4. Why Migration Metadata Has No Timestamp

This is called out because it is the one place the M2.2 implementation deliberately does *less*
than the sprint brief's field list might suggest. `ContractMigrationMetadata` records
`MigratedFromSchemaVersion` and `MigratorId` only.

A wall-clock "migrated at" stamp was considered and rejected: `DeploymentPackageContractReader`
is read from at least two independent call sites in production (Runtime Agent's
`ReleaseDeploymentProvider` and DRV's `ContractValidationStage`/`InfrastructureProvisioningValidationStage`),
each with its own cache keyed independently (`package:` vs `directory:`). If migration stamped
`DateTimeOffset.UtcNow`, those two reads of the *same immutable package* would produce
byte-different contracts purely because they happened at different instants — silently breaking
the existing `RuntimeAndDrvReadsAreBitForBitEquivalent` guarantee from Sprint M2.0. Determinism
of the read path took priority over recording an ephemeral instant that callers can already
obtain from their own process/audit logs if needed.

---

## 5. Upgrade Path (for the next schema revision, v3)

1. Add `V3` and bump `Current` in `ContractSchemaVersions`; add a `ContractVersionV3` string constant.
2. Extend `InfrastructureContract` (or, if the shape change is large, introduce the fields as a
   new nested metadata block, following the v2 envelope's pattern) — additive only; existing
   readers of v2 must still be able to ignore fields they don't understand.
3. Write `InfrastructureContractV2ToV3Migrator : IInfrastructureContractMigrator` and register it
   in `DeploymentPackageContractReader.Migrators`. Do **not** write a v1→v3 migrator; chain through v2.
4. Update `InfrastructureSpecificationCompiler` to stamp the new envelope fields at publish time.
5. Add the v3 equivalent of the M2.2 regression suite: native round-trip, v1→v3 chained migration,
   v2→v3 direct migration, canonical equality between all migration paths and native v3, the
   compatibility matrix's new rows, and determinism.
6. Update this document's Schema Lifecycle table (§1) and Compatibility Guarantees table (§3).
7. Decide schema v1's fate: still Supported, newly Deprecated, or Removed (§1) — this is a
   product decision, not an automatic consequence of shipping v3.

---

## 6. Deprecation Policy

No schema version may move to **Removed** in the same sprint that introduces its successor's
successor's successor is deprecated — in practice: a version stays **Supported (legacy)** for at
least one full milestone after it stops being **Current**, giving every package published under
it a full milestone window to be naturally superseded by a republish before the migrator that
reads it is deleted. Removal deletes the migrator and drops the version from
`ContractSchemaVersions.Supported`; any package still on that version thereafter fails
deterministically (`ICA010`) and must be republished from its original source, not migrated —
migrators are not retained indefinitely as a safety net once a version is Removed.

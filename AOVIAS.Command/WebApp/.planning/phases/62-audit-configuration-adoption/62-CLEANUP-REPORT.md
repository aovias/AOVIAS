# Cleanup Report — Stage 5B.3

Nothing here was deleted before every consumer of it had been migrated and the build was green.
The check was mechanical: for each symbol, grep for references outside its declaring file, confirm
zero, then delete.

---

## Deleted: duplicate business definitions

### `findings-workspace/models/findings-workspace.model.ts`

Twelve exported constants removed. The file keeps its DTO interfaces and union types — those
describe the wire, which is not configuration.

| Symbol | Replaced by |
| --- | --- |
| `FINDING_CLASSIFICATION_OPTIONS` | `FindingClassification` category |
| `FINDING_SOURCE_OPTIONS` | `FindingSource` category |
| `FINDING_SOURCE_LABELS` | derived from the same map, so it went with it |
| `FINDING_SOURCE_ORDINALS` | `ordinal` attribute |
| `FINDING_STATUS_LABELS` | `FindingStatus` category |
| `FINDING_STATUS_TRANSITIONS` | `selectableWhenTransitioning` |
| `FINDING_LIFECYCLE_STEPS` | `lifecycleStep` |
| `FINDING_DETAIL_LIFECYCLE_STEPS` | `detailStep` |
| `FINDING_SEVERITY_OPTIONS` | `FindingSeverity` category |
| `FINDING_ATTACHMENT_TYPES` | `FindingAttachmentType` category |
| `FINDING_QUICK_FILTERS` | `FindingQuickFilter` category |
| `PROMOTION_READINESS_OPTIONS` | `PromotionReadiness` category |

### `deficiency-command-center/models/deficiency-command-center.model.ts`

| Symbol | Replaced by |
| --- | --- |
| `ROOT_CAUSE_OPTIONS` | `RootCauseCategory` category |
| `EFFECTIVENESS_REVIEW_OPTIONS` | `EffectivenessReviewStatus` category |
| `CAPA_ACTION_STATUS_OPTIONS` | `CapaActionStatus` category |
| `EFFECTIVENESS_REVIEW_SCHEDULE_OPTIONS` | `EffectivenessReviewSchedule` + `days` |
| `DEFICIENCY_LIFECYCLE_STEPS` | `lifecycleStep` on `DeficiencyStatus` |
| `RISK_LEVEL_CLASS` | `badgeClass` on `RiskLevel` |

### `auditor/models/auditor-workspace.model.ts`

| Symbol | Replaced by |
| --- | --- |
| `AUDIT_TYPE_LABELS` | `AuditType` category |
| `SESSION_STATUS_LABELS` | `AuditSessionStatus` category |
| `OBSERVATION_TYPE_LABELS` | `ObservationType` category |

`AUDIT_TYPE_LABELS` was the duplicate the `AuditType` catalog's own docstring claimed to have
consolidated away. It had not been — it was still there, still imported by four screens, and still
free to disagree with the catalog.

---

## Deleted: dead code

### `audit/models/finding.models.ts` — reduced to one function

`FINDING_SEVERITY`, `FINDING_STATUS`, `FINDING_DEPARTMENT`, `FINDING_RAISED_BY`, their four `Wire`
union types, `FindingClassificationWire`, `FindingListItem`, `FindingDetail`,
`CreateManagedFindingRequest` and `FindingQueryParams` — **none of them imported anywhere**. A
fourth copy of the finding business definitions, kept alive only by being exported. The Findings
Workbench had long since moved to `findings-workspace.model.ts`.

Only `toFindingWireEnum` survives, and it is genuinely not configuration: it encodes the
camelCase/PascalCase mismatch between `JsonStringEnumConverter` and the platform's configured codes.

### `core/configuration/audit-type.catalog.ts` — removed entirely

Superseded by the generic catalog. Its unused API surface went with it: `applyAuditTypes`,
`isKnownAuditType`, `knownAuditTypeIds`, `auditType()`, `AUDIT_TYPE_IDS`, `AuditTypeId`,
`DEFAULT_AUDIT_TYPES`, `AuditTypeDescriptor`. Every one had zero external callers — API surface
built for a migration that had not happened yet.

---

## Deleted: duplicated logic

### `formatStatus` — three identical copies

`dcc-grid`, `dcc-detail-panel` and `corrective-actions` each carried the same chain translating
`ReviewPending` / `InProgress` / `EvidenceSubmitted`. Two of the three had already drifted:
`corrective-actions` returned "Pending Verification" and "Evidence Submitted" where the other two
returned "Verification Pending" and "Awaiting Evidence". One `configuration.label()` call now.

### `slaClass` / `slaStatusClass` — two copies of the same `if` ladder

`dcc-grid` and `dcc-detail-panel`. Both ended in an unconditional `else` returning the "closed"
style, so an unrecognised SLA state was displayed as a closure. Now `attributeOrFallback` against an
explicitly configured catch-all.

### `confidenceClass` — an `if` ladder with the same flaw

Replaced by the same mechanism.

### Inline CSS token construction

`'sev sev--' + severity.toLowerCase()` appeared in `findings-grid`, `dcc-grid` and
`dcc-detail-panel`. A severity code that was not already a valid CSS token rendered unstyled. Now
`badgeToken`.

### Two duplicate `AuditType` unions

The same nine-member union in `auditor-workspace.model.ts` and `audit-management-api.service.ts`.
Both claimed a closed set — a claim that stopped being true when audit types became tenant
configuration, since a type an administrator adds is valid at runtime but would not typecheck. Both
now alias `AuditTypeCode`.

### Grouping conditionals

`corrective-actions.applyGrouping` had a `groupBy === …` chain choosing both the field to group on
and whether to label the heading. Only the status grouping produced readable headings; the other two
showed raw codes. Both concerns are now `field` and `labelFromCategory` attributes.

### `buildRecurrenceDates`

`recurrence === 'monthly' ? 3 : 4` and `? 1 : 3`. The count and interval were implied by the
option's *name* ("Monthly ×3") and re-derived here, so changing a cadence meant editing a string and
a branch and hoping they agreed. Now `occurrences` and `intervalMonths`.

### Surveillance membership by substring

`a.auditTypeLabel.toLowerCase().includes('surveillance')` — matching on the *label*, so renaming an
audit type could silently add or drop it from the Surveillance screen. Now the `surveillance`
attribute.

### Terminal-state tuples

`TERMINAL_SESSION_STATUSES` and `TERMINAL_SCHEDULE_STATUSES`, private `static readonly` fields on
the Surveillance component — "which states mean the work is finished" was a business decision only
one screen could see. Now the `terminal` attribute on two categories.

### Display regexes

`v.replace(/([A-Z])/g, ' $1').trim()` appeared in `findings-grid`, `findings-detail-panel` (twice)
and `findings-dashboard`. In the grid it was chained with a string replace to produce "OFI". These
were display-name decisions dressed as formatting, and they made grids disagree with the filters
beside them.

---

## Also removed

- `import { enumIn }` in `audit-surveillance` — its last caller went with the terminal-state tuples.
- `import { signal }` in `findings-detail-panel` — already unused before this wave; the import line
  was being edited anyway.

---

## Verification after cleanup

- `tsc --noEmit` — clean.
- `ng build` — succeeds. Warnings unchanged from baseline and all in untouched files.
- `ng test` — 187 passed, 1 pre-existing failure.
- `eslint` — 7 errors, all pre-existing `no-output-native` on `close`/`select`/`sort` output names.

No duplicate source of truth for any migrated business definition remains. Section 9 of the phase
summary lists what is still hardcoded and why.

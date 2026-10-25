# Sprint 11 — CAPA Validation Report

## Validation Matrix

| Flow | Status | Evidence |
|------|--------|----------|
| Finding → Promotion → Deficiency | ✓ | Existing Sprint 7B pipeline unchanged |
| RCA capture | ✓ | `PATCH /rca` + DCC detail RCA section |
| Corrective Action | ✓ | Domain fields + `PATCH /corrective-action` + DCC section |
| Preventive Action | ✓ | Domain fields + `PATCH /preventive-action` + DCC section |
| Evidence | ✓ | Reused obligation/evidence linkage |
| Verification | ✓ | Reused verification status on obligation |
| Effectiveness Review | ✓ | `Scheduled` status + schedule offsets (30/60/90/custom) + `PATCH /effectiveness-review` |
| Closed | ✓ | Existing deficiency close workflow |
| Recurrence detection | ✓ | `RecurrenceEvaluationService` on OE/root cause/dept/type with configurable lookback |
| Recurrence reported | ✓ | DCC detail Recurrence Analysis + dashboard KPI |
| SLA integration | ✓ | Corrective/preventive handlers re-evaluate SLA |
| Governance visibility | ✓ | `CapaGovernance` on Compliance Maturity dashboard |
| No duplicate workflows | ✓ | No CAPA menu, no separate entity hierarchy |

## Automated Tests

- `ComplianceService.Tests`: **188 passed** (includes updated CM aggregation stub for `ICapaQueryService`)

## Migration

- `20260624062202_Sprint11_CapaFramework` — adds CAPA columns and indexes on `ComplianceDeficiencies`

Apply:

```bash
cd ComplaincePlatform/ComplianceService
dotnet ef database update
```

## Success Criteria Checklist

- [x] CAPA embedded inside DCC
- [x] No duplicate remediation workflows
- [x] Corrective actions tracked
- [x] Preventive actions tracked
- [x] Effectiveness reviews active
- [x] Recurrence detection implemented
- [x] SLA integration preserved
- [x] Governance visibility added
- [x] Analytics foundation established
- [x] Ready for Sprint 12 — Cross Module Analytics

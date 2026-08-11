# Module Readiness Matrix

[← README](./README.md) · [Module Catalog](./04-Module-Catalog.md)

**Legend:** Percentages are engineering estimates based on codebase and sprint reports (June 2026). **Not** marketing readiness.

| Module | Impl % | Business % | Technical % | UX % | Production % | Testing | Priority | Risk |
|--------|--------|------------|-------------|------|--------------|---------|----------|------|
| Journey | 92 | 90 | 88 | 85 | 75 | Partial (Vitest) | P0 | Medium |
| Compliance Maturity | 88 | 85 | 85 | 82 | 70 | Partial | P0 | Medium |
| Evidence Library | 90 | 88 | 88 | 80 | 72 | Partial | P0 | Medium |
| Evidence Binder | 88 | 85 | 85 | 78 | 70 | Partial | P1 | Low |
| Review Queue | 85 | 82 | 85 | 75 | 68 | Partial | P1 | Medium |
| Tasks | 88 | 85 | 86 | 80 | 72 | Partial (specs exist) | P0 | Low |
| Findings | 90 | 88 | 88 | 82 | 74 | Partial | P0 | Medium |
| Deficiency Command Center | 85 | 85 | 82 | 80 | 70 | Partial | P0 | Medium |
| CAPA | 75 | 70 | 78 | 65 | 60 | Limited | P1 | High |
| SLA | 80 | 78 | 82 | 60 | 65 | Limited | P1 | Medium |
| Escalation | 78 | 75 | 80 | 58 | 62 | Limited | P1 | Medium |
| Assignment Routing | 82 | 80 | 82 | 55 | 65 | Limited | P1 | Medium |
| Analytics Hub | 85 | 80 | 82 | 78 | 68 | Partial | P1 | Medium |
| Compliance Copilot | 85 | 80 | 82 | 78 | 65 | Limited | P1 | Medium |
| Dashboard | 88 | 85 | 85 | 82 | 72 | Partial | P0 | Low |
| Organization / Tenant | 90 | 88 | 88 | 75 | 75 | Partial | P0 | Low |
| Identity / Auth | 88 | 85 | 85 | 70 | 72 | Partial | P0 | High |
| Knowledge Base | 85 | 80 | 82 | 65 | 68 | Partial | P1 | Low |
| Audit (legacy service) | 70 | 65 | 75 | 60 | 55 | Limited | P2 | Medium |
| Control Center | 75 | 70 | 78 | 72 | 60 | Limited | P2 | Low |
| Reporting | 65 | 60 | 70 | 65 | 55 | Limited | P2 | Low |
| Onboarding Wizard | 85 | 82 | 80 | 78 | 68 | Partial | P1 | Medium |
| Gap Analysis | 80 | 75 | 78 | 72 | 65 | Partial | P2 | Low |
| External Auditor Workspace | 15 | 10 | 15 | 5 | 5 | None | P2 | High |
| Policy Management | 5 | 5 | 5 | 0 | 0 | None | P3 | Low |
| Incident Management | 0 | 0 | 0 | 0 | 0 | None | P3 | — |

---

## Known Gaps (Cross-Module)

| Gap | Modules affected | Reference |
|-----|------------------|-----------|
| Duplicate readiness KPI surfaces | Dashboard, Maturity, Analytics, Copilot | TD-M05 |
| Copilot analytics N+1 queries | Copilot | TD-H01 |
| Server-side activity recording incomplete | Copilot | TD-H02 |
| Master data seeder DI registration missing | ComplianceService startup | Runtime log |
| Gateway/KnowledgeBase/Outbox not in default docker-compose | Deployment | [18-Deployment-Architecture.md](./18-Deployment-Architecture.md) |
| NgRx not adopted; signal stores vary in scope | Frontend | [14-UI-Architecture.md](./14-UI-Architecture.md) |

---

## Known Technical Debt

See [Platform Technical Debt Register](../../ComplaincePlatform/docs/PlatformTechnicalDebtRegister.md) and [24-Decision-Log.md](./24-Decision-Log.md).

---

## Future Enhancements (Selected)

| Module | Enhancement |
|--------|-------------|
| Copilot | Scheduled effectiveness snapshots, unified analytics nav |
| Journey | Additional framework policies (JCI, ISO) |
| Analytics | BI export, consultant dashboards |
| Evidence | Bulk verification, OCR metadata |
| CAPA | Effectiveness verification automation |

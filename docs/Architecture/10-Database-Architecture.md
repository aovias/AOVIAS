# Database Architecture

[← README](./README.md) · [Domain Architecture](./07-Domain-Architecture.md)

---

## Database Ownership

| Database | DbContext | Owner service |
|----------|-----------|---------------|
| `Compliance` | `ComplianceDbContext` | ComplianceService |
| `Identity` | `IdentityDbContext` | IdentityService |
| `Organization` | `OrganizationDbContext` | OrganizationService |
| `Audit` | `AuditDbContext` | AuditService |
| `KnowledgeBase` | `KnowledgeBaseContext` | KnowledgeBase |
| AI Orchestrator DB | `AIOrchestratorDbContext` | AIOrchestratorService |

**Rule:** No cross-database joins; integration via APIs and events.

---

## Compliance Database — Major Table Groups

| Group | Tables (examples) |
|-------|-------------------|
| Journey | `ComplianceJourneys`, `ComplianceJourneyHistories`, `FrameworkConfigurations` |
| Scoring | `ObjectiveElementScores`, `ComplianceStandards`, `ObjectiveElements` |
| Execution | `ComplianceTasks`, `Evidences`, `EvidenceVersions` |
| Findings | `Findings`, `FindingComments`, `FindingAttachments` |
| Deficiencies | `ComplianceDeficiencies`, `DeficiencyAuditTrails` |
| Obligations | `OrganizationComplianceObligations` |
| SLA | `SlaPolicies`, `EscalationPolicies`, `EscalationLevels` |
| CAPA | `Capas` |
| Analytics | `AnalyticsDailySnapshots` |
| Copilot (13G) | `CopilotRecommendations`, `AiDrafts`, `CopilotActionProposals`, `CopilotProposalAuditEvents`, `CopilotActivityEvents`, `CopilotEffectivenessSnapshots` |
| Integration | `OutboxMessages` |

---

## Migrations

- **ComplianceService:** ~32 migrations; latest `20260625101307_Sprint13G_ModelSnapshotSync`.
- EF Core 10; migrations applied on startup in Development via `Database.MigrateAsync()`.
- Manual Sprint13A–F files **removed**; consolidated into 13G snapshot sync.

```bash
cd ComplaincePlatform/ComplianceService
dotnet ef migrations list
dotnet ef database update
```

---

## Indexes (Copilot example)

- `IX_CopilotRecommendations_OrgPersonaStatus`
- `IX_CopilotEffectivenessSnapshots_OrganizationId_SnapshotDate` (unique)
- Standard org/hospital indexes on operational tables

---

## Read vs Write Models

| Write model | Read model |
|-------------|------------|
| Operational entities | Dashboard snapshots, `AnalyticsDailySnapshot` |
| Copilot aggregates | Copilot analytics DTOs (SQL aggregations) |
| Outbox | Projections (score updates) |

---

## Snapshot Strategy

- Hospital compliance snapshots (`ComplianceSnapshot`, program overview).
- Analytics daily snapshot job (4 AM UTC Hangfire).
- Copilot effectiveness snapshots (on-demand + **planned** nightly).

---

## Future Optimization

- Partitioning analytics snapshots by org (not implemented).
- Read replicas for reporting (planned).
- Archival policy for activity events (planned).

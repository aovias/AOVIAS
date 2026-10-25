# 10 — Master Gap Register

**Program:** Commercial Readiness Gap Analysis (CRP v1.0)  
**Product:** AOVIAS Command  
**Assessment Date:** 26 July 2026  
**Status values:** Open · In Progress · Blocked · Deferred · Closed  
**This document is the authoritative backlog for Version 1 Commercial Release.**

---

## Severity / Priority Legend

| Severity | Meaning |
|----------|---------|
| Critical | Blocks commercial launch |
| High | Major customer impact |
| Medium | Important; launch possible with controls |
| Low | Improvement |

| Priority | Meaning |
|----------|---------|
| P0 | Immediate (launch blockers) |
| P1 | Next (pilot hardening) |
| P2 | Near-term (GA polish) |
| P3 | Later |

---

## MASTER TABLE

| ID | Category | Module | Screen | Workflow | Gap Description | Current Behaviour | Expected Behaviour | Severity | Priority | Business Impact | Est. Complexity | Dependencies | Recommended Sprint | Owner | Status |
|----|----------|--------|--------|----------|-----------------|-------------------|--------------------|----------|----------|------------------|-----------------|--------------|--------------------|-------|--------|
| CRP-001 | Security | Gateway / All | — | AuthZ | Secrets (JWT, internal API keys, AI keys) committed in appsettings | Keys in source; shared `praman-internal-dev-key` | Secrets in Key Vault / env; rotated | Critical | P0 | Breach / compliance failure | M | Deploy pipeline | CR-T1 | Platform Sec | Open |
| CRP-002 | Security | Gateway | Tenant APIs | Multi-tenant | Tenant/org APIs lack systemic OrgAccess / auth | Patchy authorization; TenantController gaps | Deny-by-default; OrgAccess on all org-scoped APIs | Critical | P0 | Cross-tenant data leakage | M | CRP-001 | CR-T1 | Gateway | Open |
| CRP-003 | Commercial | Organization | — | Licensing | Subscription quotas (MaxUsers/MaxFrameworks) not enforced | Assign/upgrade APIs only; counters unused | Enforce on member create + framework enroll | Critical | P0 | Revenue leakage; uncontrolled growth | M | Subscription model | CR-T1 | Org Service | Open |
| CRP-004 | Commercial | Ops | — | Backup/Restore | No customer backup/restore/upgrade product or runbook | Subscription upgrade API only | Documented DR per DB + restore drill | Critical | P0 | Hospital IT rejection | L | DB topology | CR-T2 | DevOps | Open |
| CRP-005 | Deployment | Docker | — | Install | docker-compose.dev covers ~6 of 16+ services; Gateway missing | Partial stack | Full stack compose profiles (dev/demo/prod) | Critical | P0 | Cannot deploy complete product | L | All services | CR-T2 | DevOps | Open |
| CRP-006 | Business | Communication | Notification bell / admin | Notifications | Email is ConsoleEmailService only | Logs to console | SMTP/SendGrid production provider | High | P0 | Tasks/SLA/invites never reach users | S | CRP-005 | CR-T2 | Communication | Open |
| CRP-007 | Business | Gateway | Notification hub | Notifications | SignalR NotificationHub not wired to Communication outbox | Hub is future integration point | Real-time in-app delivery | High | P0 | Stale in-app notifications | M | CRP-006 | CR-T2 | Gateway + Comm | Open |
| CRP-008 | Healthcare | Compliance | DCC / Control Center | CAPA | Dual/triple CAPA models (Deficiency CAPA vs orphan Capas vs ActionPlan) | Deficiency writes work; Capas unread/write-orphan | Single CAPA SSOT + register report | High | P0 | Assessor trust failure | L | Domain decision | CR-T3 | Compliance | Open |
| CRP-009 | Healthcare | Audit | Auditor routes | External audit | Dual AuditService vs Compliance Auditor; WeatherForecast scaffold | Parallel APIs | One audit domain; retire legacy | High | P0 | Inconsistent audit truth | L | Product decision | CR-T3 | Compliance | Open |
| CRP-010 | Security | WebApp | `/app/auditor/**` | External audit | `roleGuard` unused; auditor routes not role-isolated | Any authenticated user can navigate | Auditor-only shell + roleGuard | Critical | P0 | Assessor/security failure | M | Role packs | CR-T1 | WebApp | Open |
| CRP-011 | Healthcare | Journey | Surveillance nav | Surveillance | SurveillanceDue hardcoded false; nav Coming soon | Feature advertised but non-functional | Implement engine or remove from product | High | P0 | False continuous-monitoring claim | M | Journey service | CR-T3 | Compliance + WebApp | Open |
| CRP-012 | UX | WebApp | `/app/ccc/**` | CCC | 7 CCC pillar routes are placeholders | “To be implemented” shells | Hide/remove until built | Medium | P0 | Looks unfinished to buyers | S | Nav model | CR-T3 | WebApp | Open |
| CRP-013 | UX | WebApp | `/app/audit/simulation` | Audit sim | Client mock presented as route | Mock UI | Remove from nav or label Demo-only | High | P0 | Misleading capability | S | Nav | CR-T3 | WebApp | Open |
| CRP-014 | AI | AIOrchestrator | Copilot | AI assist | Placeholder LLM adapters + NotImplemented draft generator | Synthetic / throws | Production adapter + kill NotImplemented paths | High | P0 | Cannot sell AI; runtime failures | M | CRP-001 | AI-0 | AI Team | Open |
| CRP-015 | Messaging | OutboxRelayWorker | — | Events | Azure Service Bus required; RabbitMQ leftover only | Worker throws without ASB | Document ASB; provide emulator/dev story | High | P0 | Events/notifications/integrations fail locally/prod misconfig | M | CRP-005 | CR-T2 | Platform | Open |
| CRP-016 | Healthcare | Workforce Intel | — | Training/LMS | No LMS/training attestation domain | Training matrix analytics only | Training plans, attendance, ack/quiz linked to OE/docs | High | P1 | NABH HR chapters incomplete | L | Docs ack | H-1 | Product + Compliance | Open |
| CRP-017 | Healthcare | Governance | — | Management Review | No dedicated Management Review module | Fragmented committees + reports | MR agenda/inputs/outputs/actions | High | P1 | Accreditation governance gap | L | Committees | H-1 | Product | Open |
| CRP-018 | UX | WebApp | `/app/dashboard` | Role UX | Single shared dashboard for all roles | One layout | Role-aware dashboards (QM, HOD, Admin, Auditor) | High | P1 | Poor adoption outside QM | M | RE role packs | R-1 | WebApp | Open |
| CRP-019 | Business | Journey | OE execution | OE assignment | No first-class OE→person assignment UX | Indirect via ownership/tasks | Ownership matrix + notify | Medium | P1 | Accountability ambiguity | M | CRP-006 | R-1 | Compliance + WebApp | Open |
| CRP-020 | Business | Evidence | OE wizard (prod) | Evidence upload | `enableOeWizardEvidenceUpload: false` in prod env | Upload path may be blocked | Safe prod upload with limits/virus scan | Medium | P1 | Blocks evidence collection | S | Env config | CR-T3 | WebApp | Open |
| CRP-021 | Business | Evidence | Binder | Evidence AI | Binder AI suggestions empty | Empty suggestions | Wire AO-01 or hide AI affordance | Medium | P1 | Broken AI promise | S | CRP-014 | AI-1 | Compliance | Open |
| CRP-022 | Business | Identity | Users admin | User mgmt | Bulk import/export Coming soon | Buttons disabled | CSV invite/import/export | Medium | P1 | Slow hospital rollout | M | Identity | R-1 | Identity + WebApp | Open |
| CRP-023 | Security | AccessGovernance | Operational routes | Permissions | Many Tier-1 routes not permissionGuard’d; coarse codes | Admin gated; ops broad | Permission map for QM/HOD/Staff/Auditor | High | P1 | Over/under privilege | M | Role packs | R-1 | WebApp + AG | Open |
| CRP-024 | Healthcare | Framework | Journey policies | Multi-framework | NABL/Pollution/FireNOC/Default policies placeholders | Allow-all transitions | Real gates or disable frameworks | High | P1 | Invalid journey states | M | Catalog | CR-T3 | Compliance | Open |
| CRP-025 | Business | Organization | — | Org structure | No campus/building/wing hierarchy | Flat org→hospital→dept | Optional facility hierarchy | Medium | P2 | Large campus mapping pain | L | Org model | H-2 | Org | Open |
| CRP-026 | Healthcare | — | — | Equipment | Biomedical equipment register missing | Unsupported in seeder notes | Equipment compliance register | Medium | P2 | Facility chapters weak | L | — | H-2 | Product | Open |
| CRP-027 | UX | Governance | Committee meetings | Committees | Thin ID/agenda forms | Basic CRUD UX | Agenda, attendance, MoM, decision→CAPA | Medium | P1 | Weak governance story | M | CRP-008 | H-1 | WebApp + Gov | Open |
| CRP-028 | Business | Patient Safety | Incident promote | Incident→CAPA | Links to deficiency not CAPA SSOT | Promote to deficiency fields | Promote to unified CAPA | Medium | P1 | Closed-loop quality weak | M | CRP-008 | CR-T3 | Compliance | Open |
| CRP-029 | UX | Auditor | observations/history | Auditor UX | Routes reuse sessions page | Wrong/duplicate UX | Dedicated observation & history screens | Medium | P1 | Assessor friction | M | CRP-010 | R-1 | WebApp | Open |
| CRP-030 | UX | WebApp | Compliance graph nav | Traceability | Graph routes live but header nav hidden | Discoverability poor | Enable in assessor/QM modes | Medium | P1 | Traceability underused | S | Nav flag | R-1 | WebApp | Open |
| CRP-031 | Technical | WebApp | Integration/WI API clients | API | Double `/api/v1` prefix risk | Possible 404s | Fix BaseApi usage | High | P0 | Broken admin modules | S | — | CR-T1 | WebApp | Open |
| CRP-032 | Technical | Services | Swagger | Support | Swagger UI disabled (.NET 10) | No interactive API docs | Restore OpenAPI at Gateway | Medium | P2 | Supportability | M | Packages | CR-T5 | Platform | Open |
| CRP-033 | Technical | Platform | — | Observability | No OpenTelemetry | Serilog + health only | OTel traces/metrics | High | P1 | Blind production ops | M | CRP-005 | CR-T4 | Platform | Open |
| CRP-034 | Performance | Compliance | Journey/workspace APIs | Perf | Broad reads / in-memory paging documented | Latency risk at scale | SQL filter/page; cache catalog | High | P1 | Multi-dept slowness | M | Perf docs | CR-T4 | Compliance | Open |
| CRP-035 | Commercial | WebApp | — | Licensing UI | No customer subscription/billing UI | APIs only | License console for Hospital Admin | High | P1 | Cannot self-manage plan | M | CRP-003 | CR-T1 | WebApp + Org | Open |
| CRP-036 | Demo | DemoDataSeeder | — | Demo | Requires pre-existing user/org/hospital | Enrichment only | Greenfield demo provisioner | Medium | P1 | Sales friction | M | Identity/Org | CR-T2 | Demo | Open |
| CRP-037 | Demo | Docker | — | Demo | No one-click full demo stack | Manual multi-project run | `demo` compose profile + seed | Medium | P1 | Inconsistent demos | L | CRP-005,036 | CR-T2 | DevOps | Open |
| CRP-038 | UX | Settings | Branding/theme/account | Settings | Mock previews / future release copy | Not runtime-themed | Real theming or hide | Low | P2 | Polish | S | — | P-2 | WebApp | Open |
| CRP-039 | UX | Settings | AI models | AI admin | Enable/Disable/Configure Coming soon | Dead actions | Implement or hide | Medium | P1 | Admin trust | S | CRP-014 | AI-0 | WebApp | Open |
| CRP-040 | UX | Persons | `/persons/merge` | Persons | Explicit placeholder | Non-functional | Hide | Low | P2 | Nav noise | S | — | CR-T3 | WebApp | Open |
| CRP-041 | UX | Settings | platform/compliance | Settings | Coming Soon page | Placeholder | Hide | Low | P2 | Nav noise | S | — | CR-T3 | WebApp | Open |
| CRP-042 | Technical | AuditService | WeatherForecast | Dead code | Scaffold controller remains | Noise / risk | Delete | Low | P2 | Hygiene | S | CRP-009 | CR-T3 | Audit | Open |
| CRP-043 | Technical | Knowledgebase.Application | — | Dead project | Empty project folder | Noise | Remove | Low | P3 | Hygiene | S | — | CR-T5 | Platform | Open |
| CRP-044 | Documentation | Root reports | — | Docs | Stale 2025 status reports contradict current domain | Misleading | Mark obsolete; point to CRP | Medium | P1 | Bad planning decisions | S | — | DOC-1 | Docs | Open |
| CRP-045 | Documentation | Ops | — | Support | No customer install/support runbooks | Eng docs only | Install, DR, upgrade, troubleshooting pack | High | P0 | Unsupportable sale | M | CRP-004,005 | CR-T2 | Docs + DevOps | Open |
| CRP-046 | Business | Reporting | Custom reports | Reporting | Hardcoded framework/chapter filters | Brittle custom builder | Metadata-driven filters | Medium | P2 | Wrong reports | M | — | P-2 | Compliance + WebApp | Open |
| CRP-047 | Business | Admin audit | Exports | Support | Export CSV/Excel/PDF Coming soon | Disabled | Implement exports | Medium | P2 | Audit supportability | S | — | P-2 | WebApp | Open |
| CRP-048 | Healthcare | Risk | — | Risk register | Risk entity partial; not full hospital risk register | Deficiency/risk snapshots | Enterprise risk register + reviews | Medium | P2 | Incomplete risk chapter | L | — | H-2 | Compliance | Open |
| CRP-049 | Healthcare | Quality | Scorecards | Quality | Training readiness on scorecards future | Indicators only | Link competency to scorecards | Medium | P2 | Quality incomplete | M | CRP-016 | H-2 | Quality | Open |
| CRP-050 | AI | Copilot | Autonomy flags | AI safety | Autonomous flags exist while adapters immature | Risk if enabled | Force off in prod; acceptance gates | High | P0 | Unsafe automation | S | CRP-014 | AI-0 | AI Team | Open |
| CRP-051 | Security | Identity | Email confirmation | Auth | Historical bypass risk — must verify for prod | Confirm current enforcement | Require verified email for hospital users | Medium | P1 | Account quality | S | Identity | CR-T1 | Identity | Open |
| CRP-052 | Technical | Hangfire | `/hangfire` | Ops | Dashboard must be auth-locked in prod | Dev convenience | Secure Hangfire | High | P1 | Job tampering risk | S | Auth | CR-T4 | Compliance | Open |
| CRP-053 | Business | Integration | Diagnostics | Integrations | Charts Coming soon | Partial admin UX | Implement or hide | Low | P2 | Polish | S | Phase 12.1 | P-2 | Integration | Open |
| CRP-054 | UX | Forms | Multiple modules | Validation | Template-driven forms in PS/governance/auditor | Weak client validation | Reactive forms + shared error UX | Medium | P2 | Data quality | M | — | P-2 | WebApp | Open |
| CRP-055 | UX | Empty states | Multiple | UX | Inconsistent “No data.” | Sparse empties | Shared empty-state component | Low | P3 | Polish | S | — | P-3 | WebApp | Open |
| CRP-056 | Accessibility | WebApp | Login/Journey/Docs | A11y | No evidenced WCAG audit | Unknown gaps | WCAG 2.1 AA pass on Tier-1 | Medium | P1 | Enterprise procurement fail | M | — | R-1 | WebApp | Open |
| CRP-057 | Technical | WebApp | logger.service | Observability | TODO Sentry/LogRocket | Console logging | Wire FE error tracking | Medium | P1 | Blind FE failures | S | — | CR-T4 | WebApp | Open |
| CRP-058 | Architecture | Compliance | Mega-service | DDD | ComplianceService hosts journey+QDMS+incidents+quality+auditor+copilot | Boundary stress | Modularize or accept with strict folders | Medium | P2 | Change risk | L | — | ARCH-1 | Architecture | Open |
| CRP-059 | Business | Workforce | Domain fields | Workforce | ShiftPlaceholder / SalaryPlaceholder | Placeholder fields | Real model or remove | Low | P3 | Data model noise | S | — | P-3 | Workforce | Open |
| CRP-060 | Business | Controls | Controls API | Controls | System-generated placeholder controls | Placeholder creates | Real control library or hide | Medium | P2 | Misleading controls | M | — | P-2 | Compliance | Open |
| CRP-061 | Business | Gateway | Identity users handlers | User mgmt | Password expire / bulk reset placeholder handlers | Incomplete admin ops | Implement or remove endpoints | Medium | P2 | Broken admin actions | M | Identity | R-1 | Gateway | Open |
| CRP-062 | Healthcare | Framework | Catalog | Multi-accred | JCI/ISO not operationally seeded | Filters/names only | Don’t sell; or seed properly | High | P1 | Over-selling risk | L | Sales SKU | DOC-1 | Product | Open |
| CRP-063 | Role | WebApp | Staff/Doctor/Nurse | Role UX | No lite portals | Full enterprise shell | My Work lite shell | High | P1 | Adoption failure | L | CRP-018,023 | R-1 | Product + WebApp | Open |
| CRP-064 | Role | WebApp | Dept Head | Role UX | No department home | Shared dashboard | Dept OE/task/scorecard home | High | P1 | HOD non-adoption | M | CRP-018 | R-1 | WebApp | Open |
| CRP-065 | Role | AccessGovernance | Role packs | Permissions | No out-of-box hospital role templates | Generic roles | QM/HOD/Staff/Auditor packs | High | P1 | Long onboarding | M | AG seed | R-1 | AG + Product | Open |
| CRP-066 | AI | AI | Chat | AI | Chat lacks guaranteed citations/RAG over hospital graph | Assist surface exists | Citation-mandatory RAG | High | P1 | Hallucination risk | L | CRP-014 | AI-1 | AI | Open |
| CRP-067 | AI | AI | CAPA assist | AI | CAPA suggestions before SSOT unsafe | Copilot create doesn’t insert Capas | After CRP-008, draft assist | Medium | P2 | Bad CAPAs | M | CRP-008,014 | AI-2 | AI | Open |
| CRP-068 | Business | Onboarding | Team invite | Onboarding | Email invites depend on console email | Invites may not send | Real email + invite status UX | High | P0 | Team not onboarded | S | CRP-006 | CR-T2 | Onboarding | Open |
| CRP-069 | Technical | FE/BE repos | — | Release | Frontend sibling repo version coupling undocumented | Separate WebApp | Version matrix + release train | Medium | P1 | Mismatched deploys | S | — | DOC-1 | Release Mgmt | Open |
| CRP-070 | Performance | Dashboard | Dashboard | Perf | Duplicate aggregation / N+1 debt noted historically | May still recur | Projection SSOT; tests | Medium | P2 | Slow home | M | Perf reports | CR-T4 | Compliance | Open |
| CRP-071 | Security | Directory | Search | Privacy | RLS/field masking incomplete per gap analyses | Possible over-exposure | Mask PII by permission | Medium | P1 | Privacy risk | M | AG | R-1 | Directory | Open |
| CRP-072 | Business | SLA | Deficiencies | SLA | Escalation depends on notification delivery | Policies exist; chase broken | Email+in-app escalation path | High | P1 | Overdue CAPA silent | M | CRP-006,007 | CR-T2 | Compliance | Open |
| CRP-073 | Healthcare | Internal audit | Audit program | Internal audit | No full annual audit calendar/checklist productization | Modes + findings | Audit program planner | High | P1 | Internal audit chapter weak | L | CRP-009 | H-1 | Compliance | Open |
| CRP-074 | Commercial | Sales | SKU | Commercial | Product scope not SKU-constrained | Broad nav implies full OS | NABH Pilot SKU + exclusions | Critical | P0 | Mis-sale / churn | S | CRP docs | DOC-1 | Product | Open |
| CRP-075 | Support | Platform | — | Support | No L1/L2 support playbooks / severity matrix | Eng tribal knowledge | Support pack | High | P1 | Unsupportable GA | M | CRP-045 | DOC-1 | Support | Open |
| CRP-076 | Technical | Journey | Mock workspace | Demo integrity | Mock journey data layer remains | Flag default false | Remove from prod builds | Medium | P2 | Accidental mock demos | S | Env | CR-T3 | WebApp | Open |
| CRP-077 | Business | Evidence | Verification | Evidence | Historical contract notes incomplete verification workflow | State machine exists; ensure audit immutability | Immutable verify audit + reasons | Medium | P1 | Assessor challenge | M | — | CR-T3 | Compliance | Open |
| CRP-078 | Healthcare | Infection / Medication | — | Specialty | No dedicated IC / medication packs | Generic only | Defer or add specialty packs | Medium | P3 | Specialty sales | L | SKU | H-3 | Product | Open |
| CRP-079 | Deployment | Config | Connection strings | Config | Mixed SQL Express / LocalDB / Praman* naming | Env confusion | Standard config story | Medium | P1 | Failed installs | S | CRP-005 | CR-T2 | DevOps | Open |
| CRP-080 | Testing | CI | E2E | Quality | Full hospital journey E2E not evidenced as release gate | Unit tests exist | CI smoke: onboard→evidence→CAPA→doc ack | High | P1 | Regressions to prod | L | Compose | CR-T2 | QA | Open |

---

## Counts by Severity

| Severity | Count |
|----------|------:|
| Critical | 8 |
| High | 35 |
| Medium | 30 |
| Low | 7 |
| **Total** | **80** |

*Critical IDs: CRP-001, 002, 003, 004, 005, 010, 074 (+ treat CRP-006/007/008/014/015 as launch-blocking High).*

---

## Sprint Map (Commercial Release Train)

| Sprint | Theme | Gap IDs (primary) | Exit criteria |
|--------|-------|-------------------|---------------|
| **CR-T1** | Security & Entitlement | 001–003, 010, 031, 035, 050, 051 | Secrets vaulted; quotas enforced; auditor gated; API client fixes |
| **CR-T2** | Runtime & Comms & Ops | 004–007, 015, 036, 037, 045, 068, 072, 079, 080 | Full compose; email live; ASB story; DR runbook; E2E smoke |
| **CR-T3** | Domain Integrity & Nav Hygiene | 008, 009, 011–013, 020, 024, 028, 040–042, 076, 077 | CAPA/Audit SSOT; stubs hidden; NABH policy gates |
| **CR-T4** | Observability & Performance | 033, 034, 052, 057, 070 | OTel; Hangfire secured; workspace paging |
| **R-1** | Role Experience | 018, 019, 022, 023, 029, 030, 056, 063–065, 071 | Role packs + HOD/Staff/Auditor shells |
| **H-1** | Healthcare MVP+ | 016, 017, 027, 073 | Training lite; MR lite; committee UX; audit program |
| **AI-0 / AI-1** | AI Foundation + Assist | 014, 021, 039, 050, 066 | Real LLM; citations; autonomy off |
| **DOC-1** | Commercial docs & SKU | 044, 062, 069, 074, 075 | SKU exclusions; support pack; obsolete docs marked |
| **H-2 / P-2 / P-3** | Expansion & polish | Remaining Medium/Low | GA candidates |

---

## Owner Legend (suggested)

| Owner | Responsibility |
|-------|----------------|
| Platform Sec | Secrets, Key Vault, threat model |
| Gateway | Edge authZ, SignalR, entitlements middleware |
| Compliance | Journey, CAPA, evidence, audit domain |
| Org Service | Subscriptions, org structure |
| Communication | Email/providers/outbox |
| WebApp | UX, guards, role shells, nav hygiene |
| AI Team | Adapters, copilot safety |
| DevOps | Compose, DR, config standards |
| Product | SKU, exclusions, roadmap |
| QA | E2E gates |
| Docs / Support | Runbooks |

---

## Status Tracking Rules

1. No Critical may be Deferred without Product + Security written approval.  
2. Closing a gap requires: code evidence + test evidence + doc update.  
3. “Coming soon” UI without a register ID is a process failure — add a row.  
4. This register supersedes contradictory statements in older root status reports.

---

## Top 15 Launch Blockers (Executive View)

1. CRP-001 Secrets in source  
2. CRP-002 Tenant isolation  
3. CRP-003 License enforcement  
4. CRP-004 Backup/restore  
5. CRP-005 Incomplete deploy topology  
6. CRP-010 Auditor role isolation  
7. CRP-074 SKU / oversell control  
8. CRP-006 Real email  
9. CRP-007 Real-time notifications  
10. CRP-008 CAPA unification  
11. CRP-009 Audit unification  
12. CRP-011 Surveillance hide-or-build  
13. CRP-014 AI placeholders  
14. CRP-015 Service Bus operability  
15. CRP-045 Customer ops documentation  

---

## Relationship to Other CRP Docs

| Doc | Role |
|-----|------|
| 01 Platform inventory | What exists |
| 02 Business capabilities | % readiness |
| 03 Workflow gaps | End-to-end breaks |
| 04 UI/UX audit | Screen-level |
| 05 Healthcare domain | NABH fitness |
| 06 Role experience | Persona fitness |
| 07 Technical debt | Engineering |
| 08 AI opportunities | Future AI (not credit) |
| 09 Score | 4.4/10 overall |
| **10 Master register** | **Work authorization backlog** |

---

## Closing Statement

AOVIAS Command is a **substantial healthcare compliance platform** with a viable NABH Quality Manager core and a **non-commercial operations shell**.  

The Master Gap Register above is the path from engineering achievement to an enterprise-deployable hospital product. **Do not implement fixes in the CRP exercise itself** — execute through the recommended sprints with explicit exit criteria.

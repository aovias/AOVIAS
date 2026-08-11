# 09 — Commercial Readiness Score

**Program:** CRP v1.0  
**Product:** AOVIAS Command (CompliancePlatform + WebApp)  
**Assessment Date:** 26 July 2026  
**Question:** If a real hospital must deploy tomorrow, can they?

---

## 1. Executive Verdict

| | |
|--|--|
| **Overall Commercial Readiness Score** | **4.4 / 10** |
| **Deploy tomorrow to a real hospital?** | **No** |
| **Suitable for controlled pilot?** | **Yes, with engineering support + NABH-only scope** |
| **Suitable for sales demo?** | **Yes, with seeded data and restricted script** |
| **Suitable for unsupervised commercial SaaS?** | **No** |

**Interpretation:** Strong domain breadth and a workable Quality Manager NABH loop; weak commercial packaging, entitlement, notifications, domain integrity (CAPA/audit), role isolation, and operations.

---

## 2. Category Scores (0–10)

| Category | Score | Weight | Weighted | Rationale (evidence-based) |
|----------|------:|-------:|---------:|----------------------------|
| Architecture | **6.5** | 10% | 0.65 | Microservices + Gateway + outbox real; dual models; mega ComplianceService; compose incomplete |
| Business | **5.5** | 15% | 0.83 | Core OE/evidence/docs/incidents exist; CAPA duality; surveillance missing; email broken |
| Healthcare | **6.0** | 15% | 0.90 | NABH 6E strong; LMS/MR/surveillance/multi-statutory weak |
| UX | **5.5** | 10% | 0.55 | Broad polished areas; placeholders/Coming soon; no role dashboards |
| Security | **4.0** | 12% | 0.48 | MFA/lockout exist; secrets in git; tenant isolation patchy; auditor routes ungated |
| Performance | **5.0** | 6% | 0.30 | Issues documented; OpenTelemetry absent; workspace read concerns |
| Deployment | **3.0** | 10% | 0.30 | Dev compose ~6 services; no prod install pack; ASB hard dependency |
| Documentation | **5.0** | 5% | 0.25 | Rich architecture docs; weak customer ops/runbooks; some stale reports |
| Demo | **7.0** | 7% | 0.49 | DemoDataSeeder + NABH seed dense; not one-click full stack |
| Support | **3.5** | 5% | 0.18 | Swagger off; thin supportability; Hangfire/admin partial |
| Commercial | **2.5** | 5% | 0.13 | Subscription model without enforcement; no billing UI; no DR product |
| **TOTAL** | | **100%** | **4.36 ≈ 4.4** | |

---

## 3. Category Detail Cards

### Architecture — 6.5/10

| Strengths | Gaps |
|-----------|------|
| Clear service inventory; JWT edge; MediatR Gateway; EF migrations; health endpoints | Dual CAPA/Audit; incomplete compose; ASB-only messaging; Compliance mega-service |

**Launch bar:** 8.0 — requires domain unification + full topology.

### Business — 5.5/10

| Strengths | Gaps |
|-----------|------|
| Onboarding, journey, evidence, tasks, findings, QDMS, incidents, quality | CAPA integrity; notifications; surveillance; incomplete assignment UX |

**Launch bar:** 7.5

### Healthcare — 6.0/10

| Strengths | Gaps |
|-----------|------|
| NABH catalog ~503 OEs; scoring/maturity; docs; indicators; committees BE | LMS; management review; assessor mode; statutory frameworks |

**Launch bar:** 7.5 for NABH-only SKU

### UX — 5.5/10

| Strengths | Gaps |
|-----------|------|
| Journey/Evidence/Docs quality; shell; operational modes | CCC/simulation/surveillance unfinished; shared dashboard; form validation split |

**Launch bar:** 7.0

### Security — 4.0/10

| Strengths | Gaps |
|-----------|------|
| IdentityServer; MFA; lockout; permission library | Secrets committed; OrgAccess incomplete; roleGuard unused; AI key in source |

**Launch bar:** 8.0 (non-negotiable)

### Performance — 5.0/10

| Strengths | Gaps |
|-----------|------|
| Perf audits exist; projections/Hangfire patterns | No OTel; known broad reads; unproven SLOs |

**Launch bar:** 6.5 for pilot; 8.0 for multi-hospital SaaS

### Deployment — 3.0/10

| Strengths | Gaps |
|-----------|------|
| Dev Dockerfile; deployment.metadata; migrations | Compose subset; no backup/restore; Key Vault deferred |

**Launch bar:** 8.0

### Documentation — 5.0/10

| Strengths | Gaps |
|-----------|------|
| Architecture, verification, QA inventory, seed README | Customer install/DR/support runbooks missing; stale root status docs |

**Launch bar:** 7.0

### Demo — 7.0/10

| Strengths | Gaps |
|-----------|------|
| DemoDataSeeder density; NABH SQL seed; verify mode | Requires pre-existing user/org; full stack not compose-complete |

**Launch bar:** 8.0 (one-click demo)

### Support — 3.5/10

| Strengths | Gaps |
|-----------|------|
| Health probes; admin audit pages; Hangfire | Swagger off; no L2 runbooks; email ops blind |

**Launch bar:** 7.0

### Commercial — 2.5/10

| Strengths | Gaps |
|-----------|------|
| Subscription entities + assign/upgrade APIs; trial on signup | No quota enforcement; no portal; no license audits; no DR SKU |

**Launch bar:** 8.0

---

## 4. Demo Journey Readiness

| Journey completeness | Ready? | Notes |
|---------------------|--------|-------|
| **25%** — Login, org context, dashboard | **Yes** | |
| **50%** — Onboard/enroll, journey browse, evidence list | **Yes** | Seeded hospital preferred |
| **75%** — OE evidence → review → finding → deficiency CAPA → doc ack | **Conditional** | CAPA duality; email off |
| **100%** — Full hospital lifecycle incl. surveillance, training, MR, license, auditor isolation, email, DR | **No** | Multiple critical gaps |

**Demo readiness score: 7/10** (scripted)  
**Unaided hospital journey: 4/10**

---

## 5. Go / No-Go Gates

### NO-GO (block commercial launch)

| Gate | Status |
|------|--------|
| Secrets removed from source / Key Vault | FAIL |
| Subscription quotas enforced | FAIL |
| Tenant isolation systemic | FAIL |
| Real email delivery | FAIL |
| CAPA single source of truth | FAIL |
| External auditor role isolation | FAIL |
| Backup/restore runbook + drill | FAIL |
| Full runtime topology documented/automatable | FAIL |
| Unfinished nav (CCC/Simulation/Surveillance) hidden | FAIL |

### GO for limited pilot (optional)

| Gate | Status |
|------|--------|
| NABH-only scope written in contract | Possible |
| Seeded or assisted onboarding | Possible |
| Named QM champion + AOVIAS engineer on call | Required |
| AI autonomy off; Copilot labeled beta | Required |
| Single-tenant or tightly controlled tenancy | Required |

---

## 6. Score Trajectory (Recommended)

| Milestone | Target overall | Focus |
|-----------|----------------|-------|
| Now | 4.4 | CRP baseline |
| After CR-T1 Security/Entitlement | 5.5 | Secrets, OrgAccess, license, roleGuard |
| After CR-T2 Runtime + Email | 6.3 | Compose, ASB, SMTP, SignalR |
| After CR-T3 Domain integrity | 7.0 | CAPA/Audit unify; hide stubs |
| After Healthcare MVP (LMS lite + MR lite) | 7.6 | Training ack + MR pack |
| Commercial GA | **8.0+** | DR drills, support pack, SLOs, AI-1 |

---

## 7. Commercial Packaging Recommendation

Sell **SKU: AOVIAS Command — NABH Hospital Pilot** only, with explicit exclusions:

- No surveillance  
- No JCI/NABL/Fire  
- No production AI autonomy  
- No LMS (until built)  
- Assisted deployment (not self-serve SaaS)  

Do **not** sell “full hospital accreditation OS” until overall score ≥ 8.0 and all NO-GO gates pass.

---

## 8. Scorecard Signature

| Role | Assessment |
|------|------------|
| Principal Enterprise Architect | Architecture 6.5; deploy/security drag overall |
| Healthcare Domain Expert | NABH core real; lifecycle incomplete |
| Senior Product Manager | Demo-strong, commercial-weak |
| QA Lead | ~48/55 areas implemented; critical unfinished still shipped in nav |
| UX Auditor | QM workstation yes; role product no |
| Technical Reviewer | Debt concentrated in integrity + ops, not greenfield gaps |

**Authoritative backlog:** see `10-master-gap-register.md`

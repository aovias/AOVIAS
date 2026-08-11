# PRAMAN Architecture Constitution

Version: 1.0  
Status: Authoritative  
Scope: All backend, frontend, data, and AI-generated changes in PRAMAN  
Precedence: This constitution overrides local convenience patterns and short-term implementation shortcuts.

---

## Section 0 - Constitution Precedence

- All contributors (human and AI) MUST follow this constitution before applying module-level conventions.
- Local documentation and rule files (`.mdc`, feature docs, service docs) are implementation guides under this constitution.
- In case of conflict:
  1. Security and compliance principles in this document win.
  2. Architectural boundary and non-breaking evolution rules win.
  3. Local implementation rules apply only if compatible with this constitution.

---

## Section 1 - PRAMAN Core Philosophy

### 1.1 Enterprise First

PRAMAN is an enterprise healthcare compliance platform. Engineering decisions MUST prioritize:

- scalability
- maintainability
- auditability
- security
- workflow clarity
- operational reliability

over:

- short-term speed
- quick hacks
- temporary solutions

### 1.2 Compliance-Centric Architecture

Every module MUST support:

- traceability
- audit history
- role accountability
- evidence tracking
- workflow transparency

### 1.3 Reusable Systems Over Duplication

All changes MUST prefer:

- reuse of existing services
- reuse of shared components
- extension of existing patterns

and MUST avoid duplicate implementations of business logic and UI behavior.

### 1.4 Non-Breaking Evolution

New features MUST preserve:

- existing workflows
- API contracts
- RBAC behavior
- responsive behavior

Breaking changes require explicit architecture approval and migration strategy.

---

## Section 2 - Domain Boundaries

PRAMAN domains MUST remain explicit. Cross-domain access SHOULD happen through stable contracts and service interfaces.

| Domain | Responsibility |
|---|---|
| Audit | Audits, findings, corrective workflows |
| Compliance | Standards, objective elements, controls, journeys, scoring |
| Assessment | Evaluation, scoring interpretation, readiness views |
| AI | Summarization, copilots, reasoning assistance |
| Auth | Identity, token/session integrity, RBAC and scopes |
| Reporting | Analytics, dashboards, trend outputs |
| Notifications | Alerts, reminders, workflow signal delivery |
| Organization | Tenant, hospitals, members, structure and ownership |

Rules:

- Domain models MUST not leak internal invariants into unrelated modules.
- Cross-domain dependencies MUST be contract-first and observable.
- Shared infrastructure MUST stay generic and not absorb feature-specific domain logic.

---

## Section 3 - System Architecture Principles

### 3.1 Backend Mandatory Patterns

Backend services MUST use or preserve these patterns where applicable:

- CQRS and handler separation where already adopted
- service abstraction for business orchestration
- repository pattern for persistence access
- DTO separation between transport and domain models
- centralized validation
- structured logging and trace propagation
- event-driven extensibility via outbox/integration event patterns

### 3.2 Backend Forbidden Patterns

AI agents and developers MUST NEVER:

- bypass service layers
- access DB directly from controllers
- hardcode business rules in UI
- create duplicate repositories for the same aggregate purpose
- mix infrastructure concerns into domain logic

### 3.3 API Standards

All APIs MUST:

- use explicit versioning strategy
- return standardized response models
- support pagination on large collections
- support filtering/sorting where list complexity demands it
- include audit metadata where compliance workflows require it

---

## Section 4 - Frontend Architecture Principles

### 4.1 Angular Standards

Frontend implementation MUST follow:

- standalone reusable components where practical
- feature-based module structure
- shared UI component library patterns
- lazy-loading for feature routes and heavy modules
- API abstraction through services (not template/direct calls)
- observable/signal-based predictable state handling

### 4.2 Frontend Forbidden Patterns

AI agents and developers MUST NEVER:

- duplicate existing components without justification
- hardcode styles that bypass design tokens
- call APIs directly from templates
- bypass shared services and interceptors
- create monolithic screens with mixed concerns

### 4.3 Enterprise UI Behavior

All screens MUST:

- support desktop and tablet workflows
- preserve enterprise spacing and visual hierarchy
- maintain shell/sidebar consistency
- use reusable table/list patterns for dense data
- maintain accessibility semantics
- avoid UI clutter and visual noise

---

## Section 5 - Security Constitution

### 5.1 Mandatory Security Rules

Every feature MUST:

- enforce RBAC
- produce audit logging for security-sensitive actions
- validate permissions server-side
- sanitize and validate uploads
- validate DTOs and contract input
- prevent insecure direct object access
- preserve tenant isolation boundaries

### 5.2 Security Forbidden Actions

AI agents and developers MUST NEVER:

- expose secrets
- bypass authorization
- trust client-side validation as authoritative
- expose internal stack traces in public responses
- hardcode credentials
- disable compliance-relevant logging

---

## Section 6 - Database Constitution

Rules:

- schema changes MUST be migration-based
- destructive schema updates require review and rollback strategy
- audit tables and history tracking are mandatory where applicable
- soft delete is preferred where regulatory retention matters
- indexing is required for enterprise query paths

AI agents MUST NEVER:

- drop production tables automatically
- create unindexed high-volume tables
- duplicate relationships with conflicting ownership
- bypass migration workflows

---

## Section 7 - Performance Constitution

### 7.1 Backend Performance Rules

Mandatory:

- pagination for large result sets
- async processing for expensive/background workloads
- optimized query design with index awareness
- avoidance of N+1 query patterns
- caching where measurable benefit exists

### 7.2 Frontend Performance Rules

Mandatory:

- avoid unnecessary API calls
- optimize Angular rendering paths
- use virtual scrolling for large tables/lists where needed
- avoid oversized reactive forms/single-screen overloading
- reduce bundle duplication and preserve lazy loading

---

## Section 8 - AI Engineering Constitution

### 8.1 AI Must Always

Before implementation, AI MUST:

1. analyze impacted modules
2. identify reusable existing patterns
3. identify security and RBAC impact
4. identify responsive and UX impact
5. identify performance impact
6. identify API dependencies and contract impact

### 8.2 AI Must Never

AI MUST NEVER:

- implement without reviewing existing patterns
- rewrite large modules unnecessarily
- introduce architecture drift
- duplicate business logic
- ignore responsive behavior
- bypass governance and review rules

### 8.3 AI Execution Order

All implementation follows:

Requirement  
-> Architecture Analysis  
-> Backend Plan  
-> Frontend Plan  
-> Security Review  
-> Implementation  
-> AI Review  
-> Human Review  
-> Merge

---

## Section 9 - Scalability Patterns

PRAMAN scalability MUST be achieved through:

- bounded modular services and clear domain ownership
- contract-first API evolution
- event/outbox extensibility instead of point-to-point coupling
- idempotent workflow handling where retries can occur
- observability-ready execution and diagnostics
- controlled configuration and environment parity

---

## Section 10 - Review Constitution

Every AI-generated change MUST be reviewed for:

- architecture compliance
- security implications
- maintainability
- performance impact
- RBAC impact
- responsive behavior and UX consistency

No change is considered complete until required review criteria are addressed.

---

## Section 11 - Documentation Constitution

For every material change, AI/developers MUST update affected documentation:

- architecture docs
- API contract docs
- module dependency narratives
- workflow diagrams when flow is changed

Documentation drift is treated as delivery debt and must be corrected before closure.

---

## Section 12 - Autonomous Engineering Maturity

| Stage | Description |
|---|---|
| Stage 1 | AI Assisted |
| Stage 2 | AI Guided |
| Stage 3 | Semi Autonomous |
| Stage 4 | Autonomous PR Generation |
| Stage 5 | Autonomous Engineering Pipelines |

Progression across stages MUST increase governance, not weaken it.

---

## Long-Term Stability Rule

This constitution optimizes for long-term enterprise stability and safe autonomy, not fast coding throughput.

The stronger the governance alignment, the safer PRAMAN autonomy becomes.

# PRAMAN QA Module Inventory

**Product:** AOVIAS Command (PRAMAN)  
**Scan Date:** July 10, 2026  
**Scope:** Angular WebApp (`WebApp/`) + Backend microservices (`ComplaincePlatform/`) via API Gateway  
**Architecture:** Angular 21 standalone app; 16 HTTP microservices + Gateway BFF; no standalone FrameworkService (framework logic in KnowledgeBase + ComplianceService)

---

## Legend

| Status | Meaning |
|--------|---------|
| **Implemented** | Live UI + API integration |
| **Partial** | UI exists; some features stubbed, mocked, or incomplete |
| **Placeholder** | Route/shell exists; no functional workflow |
| **Missing** | Referenced but not built |

---

## Table of Contents

1. [Platform](#1-platform)
2. [Dashboard & Intelligence](#2-dashboard--intelligence)
3. [Compliance Execution](#3-compliance-execution)
4. [Action Center](#4-action-center)
5. [Compliance Copilot & AI](#5-compliance-copilot--ai)
6. [Audit & Assurance](#6-audit--assurance)
7. [Documentation (QDMS)](#7-documentation-qdms)
8. [Patient Safety](#8-patient-safety)
9. [Quality Intelligence](#9-quality-intelligence)
10. [Compliance Graph](#10-compliance-graph)
11. [Reporting](#11-reporting)
12. [Identity & Access](#12-identity--access)
13. [Organizations & Workforce](#13-organizations--workforce)
14. [Governance](#14-governance)
15. [Notifications & Communication](#15-notifications--communication-administration)
16. [Integrations](#16-integrations-administration)
17. [System Administration](#17-system-administration)
18. [Compliance Framework & Knowledge](#18-compliance-framework--knowledge-backend-driven-ui)
19. [Compliance Control Center (CCC)](#19-compliance-control-center-ccc--placeholders)
20. [Backend Infrastructure](#20-backend-only--admin-infrastructure-ui-adjacent)
21. [Legacy URL Redirects](#21-legacy-url-redirects-testable-compatibility)
22. [Unwired Guards](#22-defined-but-unwired-guards)
23. [Backend Microservice Summary](#23-backend-microservice-summary)
24. [Module Count Summary](#24-module-count-summary)
25. [Cross-Cutting QA Patterns](#25-cross-cutting-qa-patterns-all-modules)
26. [Hidden Routes Checklist](#26-hidden-routes-checklist-no-main-nav--must-test)
27. [Dialog & Wizard Inventory](#27-dialog--wizard-inventory-testable-overlays)
28. [Permission Code Master List](#28-permission-code-master-list)
29. [API Endpoint Map](#29-api-endpoint-map-by-ui-module-gateway-prefix-apiv1)
30. [Priority QA Surfaces](#30-implementation-status--priority-qa-surfaces)
31. [Backend Dependency Matrix](#31-backend-service-dependency-matrix)
32. [Notes for QA Checklist Authors](#32-notes-for-qa-checklist-authors)

---

# 1. Platform

## 1.1 Application Shell & Global Navigation

| Field | Value |
|-------|-------|
| **Module Name** | Application Shell |
| **Purpose** | Authenticated operational workspace frame: header, sidebar, mobile nav, hospital context, operational mode, AI sidebar |
| **Navigation Path** | Auto on `/app/**` (except `/app/settings/**`) |
| **Parent Module** | Platform |
| **Submodules** | Header, Sidebar, Mobile Bottom Nav, Hospital Selector, Operational Mode Switcher, Notification Bell, Sidebar AI Assistant, Compliance Progress Sidebar |
| **Available Pages** | N/A (layout wrapper) |
| **Dialogs** | Hospital Selector Modal |
| **Tabs** | Operational modes: Self Assessment / Internal Audit / Final Audit |
| **Tables** | — |
| **Forms** | Hospital selector |
| **Wizards** | — |
| **Reports** | — |
| **Dashboard Widgets** | Sidebar Compliance Progress |
| **Commands** | Navigate modules, switch hospital, switch operational mode |
| **Buttons** | Profile menu → Administration, Theme toggle, Logout |
| **Bulk Actions** | — |
| **Export Features** | — |
| **Import Features** | — |
| **Search** | — |
| **Filters** | — |
| **Sorting** | — |
| **Pagination** | — |
| **Permissions Required** | `authGuard`, `onboardingGuard` |
| **Associated APIs** | `GET /dashboard/experience`, `GET /dashboard/program-overview-snapshot`, `GET /dashboard/hospital`, `POST /ai/query`, `POST /ai/assist` |
| **Associated Backend Service** | Gateway → ComplianceService, AIOrchestratorService, OrganizationService |
| **Dependencies** | IdentityService (JWT), OrganizationService (hospital context) |
| **Current Status** | **Implemented** |

## 1.2 Authentication & Session Management

| Field | Value |
|-------|-------|
| **Module Name** | Authentication |
| **Purpose** | User login, registration, MFA, password recovery, email verification, session lifecycle |
| **Navigation Path** | `/login`, `/signup`, `/register`, `/forgot-password`, `/reset-password`, `/login/mfa`, `/verify-email` |
| **Parent Module** | Platform |
| **Submodules** | Login, Signup, Register, Forgot Password, Reset Password, MFA Login, Verify Email |
| **Available Pages** | Login, Signup, Register, Forgot Password, Reset Password, MFA Login, Verify Email |
| **Dialogs** | — |
| **Tabs** | — |
| **Tables** | — |
| **Forms** | Login, Register, Forgot/Reset Password, MFA verification |
| **Wizards** | — |
| **Reports** | — |
| **Dashboard Widgets** | — |
| **Commands** | Login, Logout, Refresh token, MFA setup/verify/disable |
| **Buttons** | Submit, Forgot password link, MFA challenge |
| **Bulk Actions** | — |
| **Export Features** | — |
| **Import Features** | — |
| **Search** | — |
| **Filters** | — |
| **Sorting** | — |
| **Pagination** | — |
| **Permissions Required** | Public (pre-auth) |
| **Associated APIs** | `POST /identity/login`, `/identity/login/mfa`, `/identity/logout`, `/identity/refresh`, `/identity/register`, `/identity/forgot-password`, `/identity/reset-password`, `/identity/verify-email`, `/identity/mfa/*` |
| **Associated Backend Service** | IdentityService (via Gateway) |
| **Dependencies** | — |
| **Current Status** | **Implemented** |

## 1.3 Error & Maintenance Pages

| Field | Value |
|-------|-------|
| **Module Name** | Error Pages |
| **Purpose** | HTTP error handling and maintenance messaging |
| **Navigation Path** | `/error/403`, `/error/404`, `/error/500`, `/error/maintenance` |
| **Parent Module** | Platform |
| **Submodules** | 403 Forbidden, 404 Not Found, 500 Server Error, Maintenance |
| **Available Pages** | Error 403, 404, 500, Maintenance |
| **Permissions Required** | Public |
| **Associated APIs** | — |
| **Associated Backend Service** | — |
| **Dependencies** | `permissionGuard` redirects to `/error/403` |
| **Current Status** | **Implemented** |

## 1.4 Onboarding Wizard

| Field | Value |
|-------|-------|
| **Module Name** | Onboarding |
| **Purpose** | First-time hospital setup: profile, framework selection, assessment, team invitation |
| **Navigation Path** | `/onboarding` (auto-redirect when not onboarded) |
| **Parent Module** | Platform |
| **Submodules** | Welcome, Hospital Profile, Framework Selector, Assessment, Team Invitation, Completion, AI Setup Assistant Panel |
| **Available Pages** | AI Setup Assistant (multi-step wizard) |
| **Dialogs** | — |
| **Tabs** | Wizard steps |
| **Tables** | Team invitation list |
| **Forms** | Hospital profile, framework selection, assessment items, team invites |
| **Wizards** | **AI Onboarding Wizard** (6 steps) |
| **Commands** | Complete onboarding, run core assessment |
| **Buttons** | Next, Back, Complete, Invite team |
| **Search** | Framework search |
| **Permissions Required** | `authGuard`, `onboardingEntryGuard` |
| **Associated APIs** | `POST /onboarding/signup`, `POST /onboarding/onboard`, `GET /onboarding/core-assessment/{frameworkId}`, `GET /frameworks`, `POST /assessments` |
| **Associated Backend Service** | Gateway → OrganizationService, KnowledgeBase, ComplianceService |
| **Dependencies** | IdentityService, KnowledgeBase, ComplianceService |
| **Current Status** | **Implemented** |

## 1.5 Gap Analysis (Post-Onboarding)

| Field | Value |
|-------|-------|
| **Module Name** | Gap Analysis |
| **Purpose** | Display assessment-driven compliance gaps, recommendations, next actions |
| **Navigation Path** | `/app/gap-analysis/:assessmentId` (workflow-driven; not in main nav) |
| **Parent Module** | Platform / Onboarding |
| **Submodules** | Gap panels, recommendations, next actions |
| **Available Pages** | Gap Analysis Results |
| **Tables** | Gap items table |
| **Filters** | Severity, chapter |
| **Permissions Required** | Authenticated + onboarded |
| **Associated APIs** | `GET/POST /knowledge-base/assessments/{id}/gap-analysis` |
| **Associated Backend Service** | KnowledgeBase (via Gateway) |
| **Dependencies** | Onboarding assessment |
| **Current Status** | **Implemented** |

## 1.6 In-App Notifications (Operational)

| Field | Value |
|-------|-------|
| **Module Name** | Operational Notifications |
| **Purpose** | Real-time in-app notification bell and notification list |
| **Navigation Path** | Header notification bell (all `/app/**` pages) |
| **Parent Module** | Platform |
| **Submodules** | Notification Bell, Notification List |
| **Tables** | Notification list |
| **Commands** | Mark read, Mark all read |
| **Buttons** | Read, Read all, Navigate to source |
| **Bulk Actions** | Mark all read |
| **Filters** | Unread |
| **Sorting** | Date desc |
| **Pagination** | Scroll/load more |
| **Permissions Required** | Authenticated |
| **Associated APIs** | `GET /notifications`, `GET /notifications/unread-count`, `POST /notifications/{id}/read`, `POST /notifications/read-all` |
| **Associated Backend Service** | CommunicationService (via Gateway) |
| **Dependencies** | CommunicationService realtime |
| **Current Status** | **Implemented** |

## 1.7 Personal Account & Theme

| Field | Value |
|-------|-------|
| **Module Name** | Personal Settings |
| **Purpose** | User account preferences, theme, about |
| **Navigation Path** | Profile menu → `/app/settings/account`, `/app/settings/theme`, `/app/settings/about` |
| **Parent Module** | Administration |
| **Submodules** | Account, Theme, About |
| **Available Pages** | Account Settings, Theme Settings, About AOVIAS Command |
| **Forms** | Account profile (partial), theme selector |
| **Commands** | Change theme |
| **Permissions Required** | Authenticated |
| **Associated APIs** | Limited (client-side theme) |
| **Associated Backend Service** | IdentityService (account — partial) |
| **Current Status** | **Partial** (account features marked coming-soon in places) |

---

# 2. Dashboard & Intelligence

## 2.1 Program Dashboard (Overview)

| Field | Value |
|-------|-------|
| **Module Name** | Dashboard — Overview |
| **Purpose** | Executive program overview with server-driven widget layout |
| **Navigation Path** | Sidebar → Overview; Mobile → Home; default `/app/dashboard` |
| **Parent Module** | Dashboard & Intelligence |
| **Submodules** | Executive Summary, KPI Strip, Alert Banner, Compliance Gauge, Department Heatmap, Hospital Overview, Framework Overview, Risk Distribution, Milestones, Activity Stream |
| **Available Pages** | Dashboard |
| **Tables** | Activity stream |
| **Dashboard Widgets** | `home-executive-kpi-strip`, `home-alert-banner`, `home-compliance-gauge`, `home-department-heatmap`, `home-hospital-overview`, `home-framework-overview`, `home-risk-distribution`, `home-milestones` |
| **Commands** | Retry widget load, navigate drill-downs |
| **Filters** | Hospital context (global) |
| **Pagination** | Activity stream pagination |
| **Permissions Required** | Authenticated + onboarded |
| **Associated APIs** | `GET /dashboard/experience`, `GET /dashboard/program-overview-snapshot`, `GET /dashboard/{hospitalId}/program-overview-snapshot`, `GET /dashboard/hospital` |
| **Associated Backend Service** | ComplianceService (via Gateway) |
| **Dependencies** | OrganizationService, ComplianceService snapshots |
| **Current Status** | **Implemented** |

## 2.2 Analytics Hub

| Field | Value |
|-------|-------|
| **Module Name** | Analytics Hub |
| **Purpose** | Read-only compliance intelligence and KPI observatory |
| **Navigation Path** | Sidebar → Analytics Hub (`/app/analytics`, `/app/analytics/:section`) |
| **Parent Module** | Dashboard & Intelligence |
| **Submodules** | Overview, Executive, Operational, RCA, CAPA, SLA, Departments, Trends |
| **Available Pages** | Analytics Hub (section-driven) |
| **Tabs** | Section tabs |
| **Tables** | KPI tables per section |
| **Reports** | Embedded analytics views |
| **Filters** | Date range, department |
| **Sorting** | Table column sort |
| **Permissions Required** | Authenticated |
| **Associated APIs** | `GET /analytics/overview`, `/executive`, `/operational`, `/rca`, `/capa`, `/sla`, `/departments`, `/trends` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 2.3 Compliance Maturity Journey

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Maturity |
| **Purpose** | NABH progressive maturity journey with CMI scoring and chapter heatmap |
| **Navigation Path** | Sidebar → Compliance Maturity (`/app/compliance-maturity`) |
| **Parent Module** | Dashboard & Intelligence |
| **Submodules** | Maturity dashboard, chapter heatmap, journey sidebar, journey actions |
| **Available Pages** | Compliance Maturity Dashboard |
| **Tables** | Chapter breakdown |
| **Dashboard Widgets** | CMI gauge, chapter heatmap |
| **Filters** | Chapter, stage |
| **Permissions Required** | Authenticated |
| **Associated APIs** | `GET /compliance-maturity/dashboard` |
| **Associated Backend Service** | ComplianceService |
| **Dependencies** | Journey, scoring |
| **Current Status** | **Implemented** |

---

# 3. Compliance Execution

## 3.1 Journey — Execution Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Journey — Execution |
| **Purpose** | Primary compliance execution: chapters, objective elements, scoring, evidence capture |
| **Navigation Path** | Sidebar → Journey; Mobile → Journey (`/app/journey/execution`, `/execution/stage/:stage`, `/execution/Progress/:oeGuid`) |
| **Parent Module** | Compliance Execution |
| **Submodules** | Execution Screen, Stage Filter, OE Execution Screen, OE Execution Wizard |
| **Available Pages** | Execution Screen, Objective Element Execution Screen |
| **Dialogs** | OE History Dialog, Confirmation Dialog |
| **Tabs** | Journey shell: Execution / Maturity / Operational |
| **Tables** | OE list/grid per chapter |
| **Forms** | OE scoring, evidence metadata, comments |
| **Wizards** | **OE Execution Wizard** (draft, score, evidence upload, complete) |
| **Commands** | Start OE, save draft, complete OE, upload evidence, verify evidence |
| **Buttons** | Execute, Save draft, Complete, Upload, History |
| **Import Features** | Evidence file upload (multipart) |
| **Search** | OE/chapter search |
| **Filters** | Maturity stage, chapter, status, classification |
| **Sorting** | OE code, status |
| **Pagination** | Chapter/OE pagination |
| **Permissions Required** | Authenticated; role-based OE actions |
| **Associated APIs** | `GET/POST /journeys/{id}/session`, `/dashboard`, `/chapters`, OE CRUD, evidence multipart, `/oe-wizard/draft`, `/oe-wizard/complete`, `/journey/overview`, `/obligations`, `/readiness` |
| **Associated Backend Service** | ComplianceService, KnowledgeBase |
| **Dependencies** | OrganizationService, KnowledgeBase, Evidence module |
| **Current Status** | **Implemented** (mock mode via `useJourneyMockWorkspace` env flag) |

## 3.2 Journey — Maturity Launchpad

| Field | Value |
|-------|-------|
| **Module Name** | Journey — Maturity |
| **Purpose** | Compact stage context and remediation entry launchpad |
| **Navigation Path** | `/app/journey/maturity` |
| **Parent Module** | Journey |
| **Available Pages** | Journey Maturity Dashboard |
| **Dashboard Widgets** | Stage cards |
| **Filters** | Stage |
| **Associated APIs** | Journey dashboard APIs |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 3.3 Journey — Operational View

| Field | Value |
|-------|-------|
| **Module Name** | Journey — Operational |
| **Purpose** | Advanced operational journey view for auditors and power users |
| **Navigation Path** | `/app/journey/operational` |
| **Parent Module** | Journey |
| **Available Pages** | Journey Operational |
| **Tables** | Journey obligations |
| **Associated APIs** | `/journey/overview`, `/obligations`, `/readiness` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 3.4 Evidence Library

| Field | Value |
|-------|-------|
| **Module Name** | Evidence Library |
| **Purpose** | Browse, manage, and review compliance evidence by chapter/objective |
| **Navigation Path** | Sidebar → Evidence (`/app/evidence`) |
| **Parent Module** | Compliance Execution |
| **Submodules** | Evidence summary, chapter sidebar, objective cards, evidence list |
| **Available Pages** | Evidence Library |
| **Dialogs** | Evidence detail, upload |
| **Tables** | Evidence list per OE |
| **Forms** | Evidence metadata, upload |
| **Dashboard Widgets** | Evidence summary cards |
| **Commands** | Upload, verify, approve, reject evidence |
| **Buttons** | Upload, View, Verify, Approve |
| **Import Features** | File upload |
| **Search** | Evidence search |
| **Filters** | Chapter, OE, status, type |
| **Sorting** | Date, status |
| **Pagination** | Evidence list pagination |
| **Associated APIs** | `GET /evidence/summary`, `/list`, `/dashboard`, evidence CRUD, verify/approve |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 3.5 Evidence Review Queue

| Field | Value |
|-------|-------|
| **Module Name** | Evidence Review Queue |
| **Purpose** | Queue-based evidence review and approval workflow |
| **Navigation Path** | `/app/evidence/review-queue` (hidden; linked from tasks/drawer) |
| **Parent Module** | Evidence |
| **Available Pages** | Evidence Review Queue |
| **Tables** | Review queue table |
| **Forms** | Review comments, decision |
| **Commands** | Approve, reject, request changes |
| **Search** | Queue search |
| **Filters** | Status, reviewer, chapter |
| **Sorting** | Priority, date |
| **Pagination** | Yes |
| **Associated APIs** | `GET /evidence/review-queue`, evidence approval endpoints |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 3.6 Evidence Binder (Documents)

| Field | Value |
|-------|-------|
| **Module Name** | Evidence Binder |
| **Purpose** | Structured evidence binder organized by chapters and objectives |
| **Navigation Path** | Header → Knowledge → Documents (`/app/evidence/binder`) |
| **Parent Module** | Evidence |
| **Dialogs** | Document viewer |
| **Tabs** | Chapter tabs |
| **Tables** | Binder document list |
| **Commands** | Open document, download |
| **Export Features** | Download |
| **Search** | Binder search |
| **Filters** | Chapter, OE |
| **Associated APIs** | `GET /evidence-binder/**` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 3.7 Tasks

| Field | Value |
|-------|-------|
| **Module Name** | Tasks |
| **Purpose** | Compliance task management with Kanban, filters, detail drawer |
| **Navigation Path** | Sidebar → Tasks (`/app/tasks`) |
| **Parent Module** | Compliance Execution |
| **Submodules** | Kanban board, task drawer, risk view, filters |
| **Available Pages** | Task Page |
| **Dialogs** | Task detail drawer |
| **Tabs** | Kanban columns / list view |
| **Tables** | Task list |
| **Forms** | Task edit, comments, evidence links |
| **Commands** | Create, update, complete, assign, comment, link evidence |
| **Buttons** | Create task, Complete, Assign, Comment |
| **Search** | Task search |
| **Filters** | Status, type, assignee, priority, chapter |
| **Sorting** | Due date, priority |
| **Pagination** | List pagination |
| **Associated APIs** | `GET /tasks/summary`, `GET /tasks`, task detail, evidences, comments, history, `PATCH /tasks/{id}` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

---

# 4. Action Center

## 4.1 Findings Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Findings |
| **Purpose** | Audit findings management, promotion to deficiencies, workspace dashboard |
| **Navigation Path** | Sidebar → Findings; Mobile → Findings (`/app/audit/findings`, `/app/audit/findings/:id`) |
| **Parent Module** | Action Center |
| **Submodules** | Findings Dashboard, Findings Grid, Detail Panel, Create Panel |
| **Available Pages** | Findings List, Finding Detail |
| **Dialogs** | Create finding panel, detail panel |
| **Tabs** | Dashboard / Grid views |
| **Tables** | Findings grid |
| **Forms** | Create/edit finding, comments, attachments |
| **Dashboard Widgets** | Findings KPI dashboard |
| **Commands** | Create, edit, promote to deficiency, comment, attach |
| **Buttons** | Create Finding, Export CSV, Promote, Quick filter chips |
| **Export Features** | **Export CSV** |
| **Import Features** | Attachment upload |
| **Search** | Full-text search (findings, OE, chapters) |
| **Filters** | Quick filters, status, promotion readiness, classification, severity |
| **Sorting** | Column sort |
| **Pagination** | Grid pagination |
| **Associated APIs** | `GET/POST /findings`, `/findings/workspace/**`, promote, comments, attachments |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 4.2 Deficiency Command Center (Issues & Risks)

| Field | Value |
|-------|-------|
| **Module Name** | Deficiency Command Center |
| **Purpose** | Deficiency/issue remediation: RCA, CAPA, SLA, assignment, remediation tracking |
| **Navigation Path** | Sidebar → Issues & Risks (`/app/audit/corrective-actions`) |
| **Parent Module** | Action Center |
| **Submodules** | DCC Dashboard, DCC Grid, DCC Detail Panel |
| **Available Pages** | Corrective Actions (DCC Workspace) |
| **Dialogs** | Detail panel (slide-over) |
| **Tabs** | Overview, RCA, Remediation, CAPA, Assignment, SLA |
| **Tables** | Deficiency grid |
| **Forms** | Root cause, RCA, remediation plan, CAPA, assignment |
| **Dashboard Widgets** | DCC executive dashboard |
| **Commands** | Update root cause, RCA, remediation, CAPA, assign, SLA actions |
| **Search** | Deficiency search |
| **Filters** | Status, severity, SLA state, assignee |
| **Sorting** | SLA deadline, severity |
| **Pagination** | Grid pagination |
| **Associated APIs** | `GET /deficiencies/command-center/dashboard`, `/workspace`, PATCH root-cause/rca/remediation/CAPA, `/assignment/**`, `/sla/**` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 4.3 Control Center — Compliance Command Center

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Command Center |
| **Purpose** | Executive compliance risk command center with KPIs, alerts, readiness |
| **Navigation Path** | `/app/control-center`, `/app/control-center/compliance` (deep-link) |
| **Parent Module** | Action Center |
| **Submodules** | Executive KPI bar, Risk Command Center, Action Center, Smart Alerts, Audit Readiness, AI Intelligence Panel, Activity Stream |
| **Dashboard Widgets** | 14 KPI/alert sections |
| **Filters** | Risk level, category |
| **Pagination** | Activity stream |
| **Associated APIs** | `GET /control-center/compliance/**` (14 sections) |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 4.4 Control Center — Task Command Center (Alerts)

| Field | Value |
|-------|-------|
| **Module Name** | Task Command Center |
| **Purpose** | Alert and notification task command center |
| **Navigation Path** | Sidebar → Alerts & Notifications (`/app/control-center/tasks`) |
| **Parent Module** | Action Center |
| **Tables** | Alert/task table |
| **Dashboard Widgets** | Alert KPIs |
| **Commands** | Acknowledge, navigate to source |
| **Search** | Alert search |
| **Filters** | Severity, type, status |
| **Associated APIs** | Control center + notification APIs |
| **Associated Backend Service** | ComplianceService, CommunicationService |
| **Current Status** | **Implemented** |

## 4.5 Control Center — Knowledge Graph

| Field | Value |
|-------|-------|
| **Module Name** | Control Center Knowledge Graph |
| **Purpose** | Embedded knowledge graph visualization in control center |
| **Navigation Path** | `/app/control-center/knowledge-graph` (hidden) |
| **Parent Module** | Action Center |
| **Dialogs** | Entity detail |
| **Dashboard Widgets** | Graph widget |
| **Search** | Node search |
| **Filters** | Entity type |
| **Associated APIs** | `POST /ai/query`, graph navigation APIs |
| **Associated Backend Service** | ComplianceService, AIOrchestratorService |
| **Current Status** | **Implemented** |

---

# 5. Compliance Copilot & AI

## 5.1 Compliance Copilot (Operational)

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Copilot |
| **Purpose** | Persona-driven AI copilot: recommendations, drafts, proposals, approvals |
| **Navigation Path** | Sidebar → Compliance Copilot (`/app/copilot`) |
| **Parent Module** | Compliance Copilot & AI |
| **Submodules** | Persona Switcher, Recommendations, Draft Viewer, Proposals, Approval Cards, Activity Feed, Analytics |
| **Available Pages** | Copilot Workspace |
| **Tabs** | Recommendations, Drafts, Proposals, Activity, Analytics |
| **Tables** | Proposal list, recommendation list |
| **Forms** | Approval decisions, draft edits |
| **Reports** | Governance analytics |
| **Commands** | Generate draft, submit proposal, approve/reject, switch persona |
| **Search** | Proposal/recommendation search |
| **Filters** | Persona, status, type |
| **Associated APIs** | `GET/POST /copilot/recommendations`, `/drafts`, `/proposals`, `/personas`, persona workspace, governance analytics |
| **Associated Backend Service** | ComplianceService (Copilot stack) |
| **Current Status** | **Implemented** |

## 5.2 Sidebar AI Assistant

| Field | Value |
|-------|-------|
| **Module Name** | Sidebar AI Assistant |
| **Purpose** | Contextual AI assistant panel in operational shell |
| **Navigation Path** | Sidebar AI panel (all operational pages) |
| **Forms** | AI query input |
| **Commands** | Query, assist, navigate to copilot |
| **Permissions Required** | Authenticated; `AI.Query.Read` (Gateway) |
| **Associated APIs** | `POST /ai/query`, `POST /ai/assist` |
| **Associated Backend Service** | AIOrchestratorService |
| **Current Status** | **Implemented** |

## 5.3 AI Administration

| Field | Value |
|-------|-------|
| **Module Name** | AI Administration |
| **Purpose** | Configure AI identities, models, providers, prompts, policies, executions, approvals |
| **Navigation Path** | Profile → Administration → AI (`/app/settings/ai/**`) |
| **Parent Module** | Administration |
| **Submodules** | Dashboard, AI Identity, Models, Providers, Prompt Library, Policies, Permissions, Executions, Usage & Analytics, Approvals |
| **Available Pages** | `/ai/dashboard`, `/ai/identity`, `/ai/models`, `/ai/providers`, `/ai/prompts`, `/ai/prompts/:id/versions`, `/ai/policies`, `/ai/permissions`, `/ai/executions`, `/ai/analytics`, `/ai/approvals` |
| **Dialogs** | Profile editor, prompt editor, policy editor |
| **Tables** | Profiles, prompts, policies, executions, approvals |
| **Forms** | Profile CRUD, prompt CRUD, policy CRUD |
| **Reports** | Usage analytics |
| **Commands** | Create/edit profile, prompt rollback, grant approval, publish policy |
| **Permissions Required** | `AI.Profile.Manage`, `AI.Prompt.Manage`, `AI.Policy.Manage`, `AI.Analytics.View` |
| **Associated APIs** | `GET/POST /ai/profiles`, `/ai/prompts`, `/ai/policies`, `/ai/executions`, `/ai/analytics`, `/ai/approvals` |
| **Associated Backend Service** | AIOrchestratorService |
| **Current Status** | **Implemented** |

---

# 6. Audit & Assurance

## 6.1 Internal Audit Dashboard

| Field | Value |
|-------|-------|
| **Module Name** | Internal Audit |
| **Purpose** | Internal audit program dashboard and overview |
| **Navigation Path** | Header → Audit → Internal Audit (`/app/audit`) |
| **Available Pages** | Audit Dashboard |
| **Dashboard Widgets** | Audit KPIs |
| **Associated APIs** | Audit dashboard via findings/compliance |
| **Associated Backend Service** | ComplianceService, AuditService (legacy) |
| **Current Status** | **Implemented** |

## 6.2 Internal Audit Reports (Final Audit)

| Field | Value |
|-------|-------|
| **Module Name** | Final Audit Reports |
| **Purpose** | Final audit report generation and viewing |
| **Navigation Path** | Header → Audit → Final Audit (`/app/audit/reports`) |
| **Reports** | Audit report PDF/export |
| **Export Features** | Report export |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

## 6.3 Audit Simulation

| Field | Value |
|-------|-------|
| **Module Name** | Audit Simulation |
| **Purpose** | Simulated audit session for training/demo |
| **Navigation Path** | `/app/audit/simulation` (hidden) |
| **Associated APIs** | **None** (client-side setTimeout mock) |
| **Current Status** | **Placeholder** (UI mock only) |

## 6.4 Surveillance

| Field | Value |
|-------|-------|
| **Module Name** | Surveillance |
| **Purpose** | Continuous compliance surveillance monitoring |
| **Navigation Path** | Header → Audit → Surveillance (disabled) |
| **Current Status** | **Missing** (nav disabled: "Coming soon") |

## 6.5 External Auditor Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Auditor Workspace |
| **Purpose** | External auditor session management, evidence review, observations, findings, reports |
| **Navigation Path** | Header → Audit → Auditor Dashboard/Sessions/Evidence Review/Observations/Findings/Reports/History |
| **Parent Module** | Audit & Assurance |
| **Submodules** | Dashboard, Sessions, Session Workspace, Assessment, Reports, History |
| **Available Pages** | `/app/auditor/dashboard`, `/sessions`, `/sessions/:id`, `/evidence-review`, `/observations`, `/findings`, `/reports`, `/history` |
| **Tables** | Sessions, observations, findings |
| **Forms** | Observations, comments, assessment |
| **Reports** | Auditor reports |
| **Commands** | Create session, add observation, review evidence, generate report |
| **Export Features** | Report export |
| **Search** | Session/observation search |
| **Filters** | Session status, type |
| **Permissions Required** | Authenticated; `EXTERNAL_AUDITOR` role for writes |
| **Associated APIs** | `GET /auditor/dashboard`, `/sessions`, `/observations`, `/comments`, `/traceability`, `/report`, `/assessment-context` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** (observations/history reuse sessions page) |

---

# 7. Documentation (QDMS)

## 7.1 Documentation Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Documentation (QDMS) |
| **Purpose** | Quality document management: policies, SOPs, manuals, forms, authoring, review, acknowledgements |
| **Navigation Path** | Header → Documentation; Knowledge → Policies |
| **Parent Module** | Documentation |
| **Submodules** | Dashboard, Register, Policies, SOPs, Manuals, Forms, Authoring Studio, Explorer, Graph, Review Calendar, Acknowledgements, Analytics, Templates, Settings |
| **Available Pages** | `/app/documentation/dashboard`, `/register`, `/policies`, `/sops`, `/manuals`, `/forms`, `/authoring`, `/authoring/:id`, `/explorer`, `/graph`, `/graph/:id`, `/review-calendar`, `/acknowledgements`, `/analytics`, `/templates`, `/settings` |
| **Dialogs** | Document viewer, version compare, approval |
| **Tabs** | 13-tab feature shell navigation |
| **Tables** | Document register, review calendar, acknowledgements |
| **Forms** | Document metadata, authoring editor, approval workflow |
| **Reports** | Documentation analytics |
| **Commands** | Create, edit, submit for review, approve, publish, acknowledge, version |
| **Buttons** | New document, Submit, Approve, Publish, Acknowledge |
| **Export Features** | Document download/export |
| **Import Features** | Document upload |
| **Search** | Document search, explorer search |
| **Filters** | Type, status, department, review due |
| **Sorting** | Title, date, status |
| **Pagination** | Register pagination |
| **Permissions Required** | Authenticated; MANAGER/ORG_ADMIN/SUPER_ADMIN for mutations |
| **Associated APIs** | `GET/POST /documentation/**` (30+ endpoints), `/documentation/authoring/**`, intelligence/graph/search |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

---

# 8. Patient Safety

## 8.1 Patient Safety Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Patient Safety |
| **Purpose** | Incident management, investigations, RCA, lessons learned, patient feedback |
| **Navigation Path** | Header → Patient Safety |
| **Submodules** | Dashboard, Command Center, Investigations, RCA, Near Misses, Sentinel Events, Feedback, Lessons, Analytics, Settings |
| **Available Pages** | `/app/patient-safety/dashboard`, `/command-center`, `/investigations`, `/rca`, `/near-misses`, `/sentinel-events`, `/feedback`, `/lessons`, `/analytics`, `/settings` |
| **Tables** | Incident list, investigation list, lessons table |
| **Forms** | Incident report, investigation, RCA, feedback, lessons |
| **Reports** | Patient safety analytics |
| **Commands** | Report incident, investigate, RCA, promote to finding/deficiency, record lesson |
| **Search** | Incident search |
| **Filters** | Type, severity, status, department |
| **Associated APIs** | `GET/POST /incidents/**` (CRUD, investigation, RCA, promote, lessons, feedback, analytics) |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

---

# 9. Quality Intelligence

## 9.1 Quality Intelligence Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Quality Intelligence |
| **Purpose** | Clinical quality indicators, scorecards, benchmarking, threshold management |
| **Navigation Path** | Header → Quality Intelligence |
| **Submodules** | Dashboard, Indicators, Scorecards, Trends, Benchmarking, Thresholds, Investigations, Analytics, Executive, Settings |
| **Available Pages** | `/app/quality-intelligence/dashboard`, `/indicators`, `/scorecards`, `/trends`, `/benchmarking`, `/thresholds`, `/investigations`, `/analytics`, `/executive`, `/settings` |
| **Tables** | Indicators, scorecards, thresholds |
| **Forms** | Threshold configuration |
| **Reports** | Executive summary, analytics |
| **Export Features** | Report export |
| **Search** | Indicator search |
| **Filters** | Department, indicator type, threshold status |
| **Associated APIs** | `GET /quality/dashboard`, `/indicators`, `/scorecards`, `/trends`, `/benchmarking`, `/executive`, `/analytics` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

---

# 10. Compliance Graph

## 10.1 Compliance Graph Workspace

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Graph |
| **Purpose** | Entity-relationship compliance graph, enterprise search, traceability, executive impact |
| **Navigation Path** | Header → Compliance Graph; Knowledge → Knowledge Graph |
| **Submodules** | Viewer, Enterprise Search, Entity Explorer, Traceability Hub, Executive Impact, Analytics |
| **Available Pages** | `/app/compliance-graph/viewer`, `/search`, `/entity/:id`, `/traceability`, `/executive`, `/analytics` |
| **Tables** | Search results, traceability matrix |
| **Reports** | Executive impact report |
| **Export Features** | Traceability export |
| **Search** | **Enterprise search** (full-text) |
| **Filters** | Entity type, relationship type |
| **Associated APIs** | `GET /graph/entity`, `/relationships`, `/timeline`, `/impact`, `/search`, `/navigation`, `/traceability`, `/executive`, `/analytics`, `/visual` |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

---

# 11. Reporting

## 11.1 Reports Hub

| Field | Value |
|-------|-------|
| **Module Name** | Reporting |
| **Purpose** | Generate and export compliance, audit, task, evidence, readiness, and custom reports |
| **Navigation Path** | Header → Reports |
| **Submodules** | Landing, Compliance, Audit, Tasks, Evidence, Readiness, Custom |
| **Available Pages** | `/app/reports`, `/compliance`, `/audit`, `/tasks`, `/evidence`, `/readiness`, `/custom` |
| **Forms** | Report parameters, custom report builder |
| **Reports** | **6 report types + custom** |
| **Export Features** | **PDF, CSV, Excel blob export** |
| **Filters** | Date range, hospital, framework, department |
| **Associated APIs** | `GET/POST /reports/{type}`, export blobs |
| **Associated Backend Service** | ComplianceService |
| **Current Status** | **Implemented** |

---

# 12. Identity & Access

## 12.1 User Administration

| Field | Value |
|-------|-------|
| **Module Name** | Identity — Users |
| **Purpose** | Organization user CRUD, suspend, activate, password operations |
| **Navigation Path** | Administration → Identity & Access → Users (`/app/settings/identity/users`) |
| **Dialogs** | **Identity User Form Dialog**, **Identity Confirm Dialog** |
| **Tables** | Users table |
| **Forms** | Create/edit user, suspend/activate |
| **Commands** | Create, edit, suspend, activate, disable, unlock, force password change, reset password, delete |
| **Bulk Actions** | Batch user lookup |
| **Search** | User search |
| **Filters** | Status, role |
| **Pagination** | User list pagination |
| **Permissions Required** | `Users.View` |
| **Associated APIs** | `GET/POST/PUT/DELETE /identity/users/**`, batch, suspend, activate, disable, unlock, force-password-change, reset-password |
| **Associated Backend Service** | IdentityService |
| **Current Status** | **Implemented** |

## 12.2 Password Management

| Field | Value |
|-------|-------|
| **Module Name** | Identity — Password Management |
| **Purpose** | Organization-wide password policy and admin password operations |
| **Navigation Path** | Administration → Password Management (`/app/settings/identity/password`) |
| **Forms** | Password policy settings |
| **Permissions Required** | `PasswordManagement.Manage` |
| **Associated Backend Service** | IdentityService |
| **Current Status** | **Partial** (some features coming-soon) |

## 12.3 Access Governance (RBAC Admin)

| Field | Value |
|-------|-------|
| **Module Name** | Access Governance |
| **Purpose** | Role management, permission catalog, policy designer, delegations, approval matrix, effective permissions, auth simulator |
| **Navigation Path** | `/app/access-governance/**` (hidden; admin deep-link) |
| **Available Pages** | `/app/access-governance`, `/permissions`, `/policies`, `/delegations`, `/approvals`, `/effective`, `/simulator` |
| **Tables** | Roles, permissions, policies, delegations |
| **Forms** | Role CRUD, policy designer, delegation, approval matrix |
| **Commands** | Create role, assign permissions, publish policy, delegate, simulate authorization |
| **Bulk Actions** | Permission assignment |
| **Permissions Required** | `Compliance.Standard.View` |
| **Associated APIs** | `GET/POST /roles`, `/permissions`, `/policies`, `/role-assignments`, `/delegations`, `POST /authorization/evaluate`, `GET /authorization/effective-permissions` |
| **Associated Backend Service** | AccessGovernanceService |
| **Current Status** | **Implemented** (not in product nav) |

## 12.4 Security Monitoring

| Field | Value |
|-------|-------|
| **Module Name** | Security Monitoring |
| **Purpose** | Security events, device/session management, risk scoring, timeline, policies |
| **Navigation Path** | `/app/security/**` (hidden; admin deep-link) |
| **Available Pages** | `/app/security`, `/devices`, `/sessions`, `/risk`, `/timeline`, `/policies`, `/analytics` |
| **Tables** | Devices, sessions, events |
| **Forms** | Security policy |
| **Commands** | Revoke device, revoke session, create policy |
| **Search** | Event search |
| **Filters** | Risk level, event type, date |
| **Associated APIs** | `GET /security/dashboard`, `/events`, `/risk`, `/devices`, `/timeline`, `POST /security/device/revoke`, `/session/revoke`, `/policies` |
| **Associated Backend Service** | IdentityService |
| **Current Status** | **Implemented** (not in product nav) |

---

# 13. Organizations & Workforce

## 13.1 Organization Settings

| Field | Value |
|-------|-------|
| **Module Name** | Organization Settings |
| **Purpose** | Organization profile, timezone, regulatory settings |
| **Navigation Path** | Administration → General → Organization (`/app/settings/general/organization`) |
| **Forms** | Organization settings form |
| **Commands** | Save organization settings |
| **Permissions Required** | Authenticated (org admin) |
| **Associated APIs** | `GET/PUT /settings/organization`, `GET/PUT /organizations/{orgId}/settings` |
| **Associated Backend Service** | OrganizationService |
| **Current Status** | **Implemented** |

## 13.2 Branding

| Field | Value |
|-------|-------|
| **Module Name** | Branding |
| **Purpose** | Organization branding assets, logo, colors |
| **Navigation Path** | Administration → General → Branding (`/app/settings/general/branding`) |
| **Forms** | Branding settings, logo upload |
| **Import Features** | Logo upload |
| **Associated Backend Service** | OrganizationService |
| **Current Status** | **Implemented** |

## 13.3 Workforce Administration

| Field | Value |
|-------|-------|
| **Module Name** | Workforce Administration |
| **Purpose** | Admin workforce dashboard, departments, designations, professionals, memberships |
| **Navigation Path** | Administration → Workforce (`/app/settings/workforce/**`) |
| **Available Pages** | `/workforce/dashboard`, `/departments`, `/designations`, `/professionals`, `/memberships` |
| **Tables** | Departments, designations, professionals, memberships |
| **Forms** | CRUD forms for each entity |
| **Dashboard Widgets** | Workforce admin dashboard |
| **Associated APIs** | `GET/POST /departments`, `/organizational-units`, `/membership`, `/invitations`, `/professional-profiles` |
| **Associated Backend Service** | OrganizationService, ProfessionalService, WorkforceService |
| **Current Status** | **Implemented** |

## 13.4 Enterprise Directory

| Field | Value |
|-------|-------|
| **Module Name** | Enterprise Directory |
| **Purpose** | Unified people search across person, professional, and employment records |
| **Navigation Path** | Administration → Workforce → Enterprise Directory (`/app/settings/workforce/directory/**`) |
| **Available Pages** | `/directory`, `/search`, `/explorer`, `/results`, `/profile/:id`, `/saved`, `/history`, `/preferences` |
| **Dialogs** | **Person Lookup Dialog** |
| **Forms** | Saved search, preferences, NL search |
| **Search** | **Full-text + NL search**, facets, suggestions |
| **Filters** | Facets (department, role, skills, etc.) |
| **Permissions Required** | `Directory.Search.Read`, `Directory.Profile.Read`, `Directory.SavedSearch.Manage`, `Directory.History.Read` |
| **Associated APIs** | `GET /directory/search`, `/profile/{id}`, `/suggestions`, `/facets`, `/saved-searches`, `POST /nl-search`, `POST /rebuild` |
| **Associated Backend Service** | EnterpriseDirectoryService |
| **Current Status** | **Implemented** |

## 13.5 Workforce Intelligence

| Field | Value |
|-------|-------|
| **Module Name** | Workforce Intelligence |
| **Purpose** | Workforce analytics: headcount, skills, competencies, licenses, certifications, forecasting |
| **Navigation Path** | Administration → Workforce → Workforce Intelligence (`/app/settings/workforce/intelligence/**`) |
| **Available Pages** | Executive Dashboard, Skills, Competencies, Training, Certifications, Licenses, Availability, Organization, Responsibilities, Committees, Forecast, Recommendations, Search (14 pages) |
| **Tables** | Skills matrix, competency matrix, license/cert tables |
| **Reports** | Forecast, recommendations |
| **Permissions Required** | `Intelligence.Dashboard.Read`, `Intelligence.Forecast.Read`, `Intelligence.Recommendations.Read`, `Intelligence.Search.Read` |
| **Associated APIs** | `GET /intelligence/dashboard`, `/headcount`, `/skills`, `/competencies`, `/training`, `/licenses`, `/certifications`, `/forecast`, `/recommendations`, `POST /refresh` |
| **Associated Backend Service** | WorkforceIntelligenceService |
| **Current Status** | **Implemented** |

## 13.6 Persons (Operational)

| Field | Value |
|-------|-------|
| **Module Name** | Persons Directory |
| **Purpose** | Operational person master record management |
| **Navigation Path** | `/app/persons`, `/app/persons/:id`, `/app/persons/merge` (hidden) |
| **Tabs** | Contacts, addresses, documents, timeline |
| **Forms** | Person CRUD, photo upload, documents |
| **Import Features** | Document upload |
| **Associated APIs** | `GET/POST/PUT/DELETE /persons/**`, photo, documents, contacts, addresses, timeline |
| **Associated Backend Service** | PersonService |
| **Current Status** | **Partial** (merge = **Placeholder**) |

## 13.7 Professional Profiles (Operational)

| Field | Value |
|-------|-------|
| **Module Name** | Professional Profiles |
| **Purpose** | Clinical/professional credential management, renewals, timeline |
| **Navigation Path** | `/app/professional/renewals`, `/app/professional/:id`, `/app/professional/:id/timeline`; admin at `/app/settings/workforce/professionals` |
| **Tabs** | Qualifications, licenses, certifications, competencies, privileges, credentialing, training |
| **Tables** | Renewals table, credential tables |
| **Associated APIs** | `GET/POST /professional-profiles/**`, renewals, timeline, sub-resources |
| **Associated Backend Service** | ProfessionalService |
| **Current Status** | **Implemented** |

## 13.8 Workforce / Employment (Operational)

| Field | Value |
|-------|-------|
| **Module Name** | Workforce / Employment |
| **Purpose** | Employment lifecycle: directory, org chart, transfers, promotions |
| **Navigation Path** | `/app/workforce`, `/org-chart`, `/manager`, `/:id`, `/:id/transfer`, `/:id/promote` (hidden) |
| **Wizards** | **Transfer Wizard**, **Promotion Wizard** |
| **Dashboard Widgets** | Org chart visualization |
| **Commands** | Assign position/department/manager, transfer, promote |
| **Associated APIs** | `GET/POST /employment/**`, org-chart, transfer, promote, history, timeline |
| **Associated Backend Service** | WorkforceService |
| **Current Status** | **Implemented** |

---

# 14. Governance

## 14.1 Organizational Governance

| Field | Value |
|-------|-------|
| **Module Name** | Governance |
| **Purpose** | Responsibilities, committees, approval/escalation chains, ownership, timeline |
| **Navigation Path** | `/app/governance/**` (hidden; admin deep-link) |
| **Available Pages** | Dashboard, Responsibilities, Committees, Committee Members, Committee Meetings, Approvals, Escalations, Ownership Explorer, Timeline, Analytics (10 pages) |
| **Tables** | Responsibilities matrix, committees, approval chains, escalation chains |
| **Forms** | Responsibility CRUD, committee CRUD, chain designer |
| **Reports** | Governance analytics |
| **Associated APIs** | `GET/POST /governance/**`, `/committees/**`, `/approval-chains`, `/escalation-chains`, `/responsibilities` |
| **Associated Backend Service** | GovernanceService |
| **Current Status** | **Implemented** (minimal shell nav) |

---

# 15. Notifications & Communication (Administration)

## 15.1 Notification Administration

| Field | Value |
|-------|-------|
| **Module Name** | Notification Administration |
| **Purpose** | Enterprise notification templates, channels, providers, queues, delivery history, preferences, subscriptions |
| **Navigation Path** | Administration → Notifications (`/app/settings/notifications/**`) |
| **Available Pages** | Overview, Templates, Channels, Delivery Providers, Queues, Delivery History, Preferences, Subscriptions (8 pages) |
| **Tables** | Templates, channels, queues, delivery history |
| **Forms** | Template CRUD, channel config, provider config, preferences |
| **Reports** | Delivery history |
| **Permissions Required** | `Notification.Template.Manage`, `Notification.View`, `Notification.Provider.Manage`, `Notification.Queue.View`, `Notification.History.View`, `Notification.Preference.Manage` |
| **Associated APIs** | `GET/POST /templates`, `/notifications`, `/communication/dashboard`, `/channels`, `/providers`, `/queues`, `/delivery-history`, `/preferences`, `/subscriptions`, `/campaigns`, `/reminders` |
| **Associated Backend Service** | CommunicationService |
| **Current Status** | **Implemented** |

**Background jobs (UI-visible via Queues/Dashboard APIs):** CommunicationOutboxProcessor, OutboundMessageProcessor, ReminderSchedulerHostedService, ComplianceEventConsumerHostedService

---

# 16. Integrations (Administration)

## 16.1 Enterprise Integrations

| Field | Value |
|-------|-------|
| **Module Name** | Enterprise Integrations |
| **Purpose** | External system connectors, sync jobs, webhooks, import/export pipelines |
| **Navigation Path** | Administration → Platform → Integrations (`/app/settings/platform/integrations/**`) |
| **Available Pages** | Overview, Connectors, Connector Detail, Mappings, Synchronizations, Webhooks, Imports, Exports, Monitoring, Audit (10 pages) |
| **Tables** | Connectors, mappings, sync jobs, webhooks, import/export jobs |
| **Forms** | Connector CRUD, mapping, webhook, import/export config |
| **Wizards** | Import preview/execute flow |
| **Export Features** | **Export pipeline execution** |
| **Import Features** | **Import preview, execute, rollback** |
| **Permissions Required** | `Integration.Connector.Read`, `Integration.Mapping.Read`, `Integration.Sync.Read`, `Integration.Webhook.Read`, `Integration.Import.Execute`, `Integration.Export.Execute`, `Integration.Audit.Read`, `Integration.Monitor.Read` |
| **Associated APIs** | `GET/POST /connectors`, `/mappings`, `/synchronizations`, `/webhooks`, `/imports`, `/exports`, `/connector-health`, `/integration-audit` |
| **Associated Backend Service** | IntegrationService |
| **Current Status** | **Implemented** |

---

# 17. System Administration

## 17.1 Platform Audit & Activity

| Field | Value |
|-------|-------|
| **Module Name** | Audit & Activity |
| **Purpose** | Unified administrative activity, security events, configuration audit trail |
| **Navigation Path** | Administration → System → Audit & Activity (`/app/settings/system/audit`) |
| **Dialogs** | **Audit Activity Detail Dialog** |
| **Tables** | Activity log table |
| **Search** | Activity search |
| **Filters** | Actor, action, entity type, date range |
| **Sorting** | Timestamp desc |
| **Pagination** | Activity log pagination |
| **Permissions Required** | `Audit.View` |
| **Associated APIs** | `GET /api/v1/administration/audit/activity` |
| **Associated Backend Service** | Gateway (AdministrationAuditController) |
| **Current Status** | **Implemented** |

## 17.2 Compliance Defaults

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Defaults |
| **Purpose** | Organization-wide compliance defaults and policy baselines |
| **Navigation Path** | Administration → Platform → Compliance Defaults (`/app/settings/platform/compliance`) |
| **Current Status** | **Placeholder** (Coming Soon) |

---

# 18. Compliance Framework & Knowledge (Backend-Driven UI)

## 18.1 Framework Catalog & Enrollment

| Field | Value |
|-------|-------|
| **Module Name** | Frameworks & Standards |
| **Purpose** | Accreditation framework catalog, enrollment, structure, scoring (no standalone FrameworkService) |
| **Navigation Path** | Onboarding framework selector; Journey structure; Compliance Maturity |
| **Dialogs** | Framework selector (onboarding) |
| **Commands** | Enroll framework, view structure |
| **Associated APIs** | `GET /frameworks`, `/frameworks/{id}`, `/questionnaire`, `/core-assessment`, `GET /nodes/**`, `POST /regulations/enroll`, `GET /scoring/**` |
| **Associated Backend Service** | KnowledgeBase + ComplianceService |
| **Current Status** | **Implemented** |

## 18.2 Assessments & Gap Analysis (Backend)

| Field | Value |
|-------|-------|
| **Module Name** | Assessments |
| **Purpose** | Compliance assessments, gap analysis, wizard-driven onboarding assessment |
| **Navigation Path** | Onboarding → Gap Analysis results |
| **Associated APIs** | `POST /assessments`, `PUT /assessments/{id}/items`, `GET /summary`, `POST /from-wizard`, `GET/POST /gap-analysis` |
| **Associated Backend Service** | KnowledgeBase |
| **Current Status** | **Implemented** |

---

# 19. Compliance Control Center (CCC) — Placeholders

## 19.1 CCC Pillars

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Control Center (CCC) |
| **Purpose** | Seven-pillar compliance control center (planned) |
| **Navigation Path** | `/app/ccc/**` (hidden) |
| **Submodules** | Regulatory Governance, Organizational Control, Access Accountability, Compliance Infrastructure, Risk Escalation, Monitoring Traceability, Integration System |
| **Available Pages** | 7 CCC placeholder pages |
| **Associated APIs** | — |
| **Current Status** | **Placeholder** ("to be implemented" on all 7 routes) |

---

# 20. Backend-Only / Admin Infrastructure (UI-Adjacent)

## 20.1 Hangfire Job Dashboard (ComplianceService)

| Field | Value |
|-------|-------|
| **Module Name** | Compliance Background Jobs |
| **Purpose** | Scheduled compliance snapshots, expiry, renewal, escalation, analytics |
| **Navigation Path** | `/hangfire` on ComplianceService (not in Angular nav; admin/dev access) |
| **Submodules** | ComplianceSnapshotJob, ComplianceExpiryJob, ComplianceRenewalWindowJob, ComplianceEscalationJob, AnalyticsDailySnapshotJob |
| **UI Exposure** | Manual trigger via `POST /compliance/snapshots/recalculate`; job status via Hangfire dashboard |
| **Associated Backend Service** | ComplianceService (Hangfire) |
| **Current Status** | **Implemented** (backend admin surface) |

## 20.2 API Gateway (BFF)

| Field | Value |
|-------|-------|
| **Module Name** | API Gateway |
| **Purpose** | Single BFF entry point: JWT validation, 47 proxy controllers, YARP legacy routes |
| **Navigation Path** | All `/api/v1/**` from Angular |
| **Associated Backend Service** | Gateway → all 16 microservices |
| **Current Status** | **Implemented** |

---

# 21. Legacy URL Redirects (Testable Compatibility)

| Legacy Path | Redirect Target |
|-------------|-----------------|
| `/app/professional/**` | `/app/settings/workforce/professionals` |
| `/app/ai-identity/**` | `/app/settings/ai/**` |
| `/app/workforce-intelligence/**` | `/app/settings/workforce/intelligence` |
| `/app/directory/**` | `/app/settings/workforce/directory` |
| `/app/integration/**` | `/app/settings/platform/integrations` |
| `/app/notifications/**` | `/app/settings/notifications` |
| `/app/organization/**` | `/app/settings/workforce/**` |
| `/app/account/**` | `/app/settings/account` |

**Current Status:** **Implemented** (bookmark compatibility)

---

# 22. Defined but Unwired Guards

| Guard | File | Status |
|-------|------|--------|
| `roleGuard` | `core/guards/role.guard.ts` | **Defined, not wired to routes** |
| `frameworkGuard` | `core/guards/framework.guard.ts` | **Defined, not wired** |
| `journeyRoleRedirectGuard` | `features/journey/guards/journey-role-redirect.guard.ts` | **Defined, not wired** |

---

# 23. Backend Microservice Summary

| Service | Primary UI Modules | Key Permission Domains |
|---------|-------------------|------------------------|
| **IdentityService** | Authentication, Users, Password, Security Monitoring | Users, PasswordManagement |
| **OrganizationService** | Organization Settings, Branding, Workforce Admin (depts, membership) | OrganizationScope |
| **PersonService** | Persons Directory | — |
| **ProfessionalService** | Professional Profiles, Renewals | — |
| **WorkforceService** | Workforce/Employment, Transfer/Promotion Wizards | — |
| **EnterpriseDirectoryService** | Enterprise Directory | Directory.* |
| **WorkforceIntelligenceService** | Workforce Intelligence | Intelligence.* |
| **AccessGovernanceService** | Access Governance, all permission guards | Compliance.Standard.View + RBAC |
| **GovernanceService** | Governance module | — |
| **CommunicationService** | Notifications (operational + admin) | Notification.* |
| **IntegrationService** | Enterprise Integrations | Integration.* |
| **AIOrchestratorService** | AI Admin, Sidebar AI Assistant | AI.* |
| **ComplianceService** | Journey, Evidence, Tasks, Findings, DCC, Dashboard, Copilot, QDMS, Patient Safety, Quality, Graph, Reports, Auditor, Control Center, Analytics, Maturity | ComplianceScope |
| **KnowledgeBase** | Onboarding, Frameworks, Assessments, Gap Analysis | — |
| **AuditService** | Internal Audit (legacy overlap) | AuditScope |
| **Gateway** | All Angular API calls | Scope policies per controller |

> **Note:** No standalone **FrameworkService** — framework capabilities are in **KnowledgeBase** + **ComplianceService**.

---

# 24. Module Count Summary

| Category | Module Count | Implemented | Partial | Placeholder | Missing |
|----------|-------------|-------------|---------|-------------|---------|
| Platform | 7 | 6 | 1 | 0 | 0 |
| Dashboard & Intelligence | 3 | 3 | 0 | 0 | 0 |
| Compliance Execution | 7 | 7 | 0 | 0 | 0 |
| Action Center | 5 | 5 | 0 | 0 | 0 |
| Compliance Copilot & AI | 3 | 3 | 0 | 0 | 0 |
| Audit & Assurance | 5 | 3 | 0 | 1 | 1 |
| Documentation | 1 | 1 | 0 | 0 | 0 |
| Patient Safety | 1 | 1 | 0 | 0 | 0 |
| Quality Intelligence | 1 | 1 | 0 | 0 | 0 |
| Compliance Graph | 1 | 1 | 0 | 0 | 0 |
| Reporting | 1 | 1 | 0 | 0 | 0 |
| Identity & Access | 4 | 3 | 1 | 0 | 0 |
| Organizations & Workforce | 8 | 7 | 1 | 0 | 0 |
| Governance | 1 | 1 | 0 | 0 | 0 |
| Notifications (Admin) | 1 | 1 | 0 | 0 | 0 |
| Integrations (Admin) | 1 | 1 | 0 | 0 | 0 |
| System Administration | 2 | 1 | 0 | 1 | 0 |
| Compliance Framework (embedded) | 2 | 2 | 0 | 0 | 0 |
| CCC Placeholders | 1 | 0 | 0 | 1 | 0 |
| Backend Infrastructure | 2 | 2 | 0 | 0 | 0 |
| **Total testable areas** | **~55** | **~48** | **~3** | **~3** | **~1** |

---

# 25. Cross-Cutting QA Patterns (All Modules)

| Pattern | Modules Using It |
|---------|------------------|
| **Hospital context selector** | Dashboard, Journey, Evidence, Tasks, Findings, DCC, all operational modules |
| **Operational mode switcher** | Journey, Evidence, Findings, Auditor (Self Assessment / Internal Audit / Final Audit) |
| **`has-permission` directive** | Settings admin, Access Governance, Integration, Notifications, Directory, Intelligence |
| **`permissionGuard` route guard** | Settings Identity, AI Admin, Integrations, Notifications, Directory, Intelligence, Access Governance, Admin Audit |
| **`settingsDirtyGuard`** | Organization Settings, Branding |
| **Server-driven widget visibility** | Dashboard (`/dashboard/experience`) |
| **CSV/Excel/PDF export** | Findings (CSV), Reports (PDF/CSV/Excel), Integrations (export pipeline) |
| **File upload (multipart)** | Journey OE Wizard, Evidence, Documentation, Persons |
| **Kanban + drawer pattern** | Tasks |
| **Grid + detail panel pattern** | Findings, DCC |
| **Feature shell + sub-nav tabs** | Documentation (13), Patient Safety (10), Quality Intelligence (10), Compliance Graph (6), Journey (3) |
| **Realtime notifications** | Header bell → CommunicationService |
| **AI context injection** | Shell sidebar assistant, page-level `AiPageContextService` (Findings, Journey, etc.) |

---

# 26. Hidden Routes Checklist (No Main Nav — Must Test)

| Route | Module | Status |
|-------|--------|--------|
| `/app/gap-analysis/:assessmentId` | Gap Analysis | Implemented |
| `/app/evidence/review-queue` | Evidence Review Queue | Implemented |
| `/app/audit/simulation` | Audit Simulation | Placeholder (mock) |
| `/app/control-center` | Compliance Command Center | Implemented |
| `/app/control-center/compliance` | Compliance Command Center | Implemented |
| `/app/control-center/knowledge-graph` | Control Center Knowledge Graph | Implemented |
| `/app/access-governance/**` | Access Governance (7 pages) | Implemented |
| `/app/governance/**` | Governance (10 pages) | Implemented |
| `/app/security/**` | Security Monitoring (7 pages) | Implemented |
| `/app/persons/**` | Persons Directory | Implemented / merge placeholder |
| `/app/workforce/**` | Workforce/Employment | Implemented |
| `/app/professional/renewals`, `/:id`, `/:id/timeline` | Professional Profiles | Implemented |
| `/app/ccc/**` | CCC Pillars (7) | Placeholder |
| `/app/journey/operational` | Journey Operational | Implemented |
| `/app/journey/maturity` | Journey Maturity | Implemented |
| `/app/journey/execution/stage/:stage` | Journey Stage Filter | Implemented |
| `/app/journey/execution/Progress/:oeGuid` | OE Execution + Wizard | Implemented |
| `/app/documentation/sops`, `/manuals`, `/forms`, `/explorer`, `/graph`, `/analytics`, `/templates`, `/settings` | QDMS sub-pages | Implemented |
| `/app/patient-safety/near-misses`, `/sentinel-events`, `/analytics`, `/settings` | Patient Safety sub-pages | Implemented |
| `/app/quality-intelligence/trends`, `/benchmarking`, `/investigations`, `/analytics`, `/settings` | Quality sub-pages | Implemented |
| `/app/compliance-graph/analytics`, `/entity/:id` | Compliance Graph sub-pages | Implemented |
| `/app/settings/account`, `/theme` | Personal Settings | Partial |
| `/app/settings/platform/compliance` | Compliance Defaults | Placeholder |
| `/app/auditor/sessions/:id` | Auditor Session Workspace | Implemented |
| `/app/audit/findings/:id` | Finding Detail | Implemented |
| `/app/settings/platform/integrations/connectors/:id` | Connector Detail | Implemented |
| `/app/settings/ai/prompts/:id/versions` | Prompt Version History | Implemented |
| All legacy redirect paths (§21) | URL compatibility | Implemented |

---

# 27. Dialog & Wizard Inventory (Testable Overlays)

| Dialog / Wizard | Module | Trigger |
|-----------------|--------|---------|
| **OE Execution Wizard** | Journey | OE drill-down from execution screen |
| **OE History Dialog** | Journey | History button on execution screen |
| **Confirmation Dialog** | Shared / Journey | Destructive actions, wizard steps |
| **Hospital Selector Modal** | Shell | Header/sidebar hospital switch |
| **Identity User Form Dialog** | Identity Users | Add/Edit user |
| **Identity Confirm Dialog** | Identity Users | Suspend/delete/reset confirmations |
| **Audit Activity Detail Dialog** | Admin Audit | Activity row click |
| **Person Lookup Dialog** | Enterprise Directory | Directory search workflows |
| **AI Onboarding Wizard** | Onboarding | First-time `/onboarding` |
| **Transfer Wizard** | Workforce | `/app/workforce/:id/transfer` |
| **Promotion Wizard** | Workforce | `/app/workforce/:id/promote` |
| **Findings Create Panel** | Findings | Create finding (slide-over panel) |
| **Findings Detail Panel** | Findings | Row click (slide-over panel) |
| **DCC Detail Panel** | DCC | Deficiency row click (slide-over panel) |
| **Task Drawer** | Tasks | Task card/row click |
| **Create/Assign Task Drawer** | Journey | Task creation from OE context |
| **OE Detail Drawer** | Journey | OE preview from chapter panel |
| **Connector/Mapping editors** | Integrations | Inline forms + detail views |
| **Template editor** | Notifications | Template management |
| **Import preview/execute flow** | Integrations | Import pipeline wizard steps |

---

# 28. Permission Code Master List

| Permission Code | Protected Routes / Features |
|-----------------|----------------------------|
| `Users.View` | `/app/settings/identity/users` |
| `PasswordManagement.Manage` | `/app/settings/identity/password` |
| `AI.Profile.Manage` | `/app/settings/ai/identity` |
| `AI.Prompt.Manage` | `/app/settings/ai/prompts`, `/prompts/:id/versions` |
| `AI.Policy.Manage` | `/app/settings/ai/policies` |
| `AI.Analytics.View` | `/app/settings/ai/analytics` |
| `AI.Query.Read` | Gateway AI query endpoints (sidebar assistant) |
| `Integration.Connector.Read` | Connectors list + detail |
| `Integration.Mapping.Read` | Mappings |
| `Integration.Sync.Read` | Synchronizations |
| `Integration.Webhook.Read` | Webhooks |
| `Integration.Import.Execute` | Imports |
| `Integration.Export.Execute` | Exports |
| `Integration.Audit.Read` | Integration audit |
| `Integration.Monitor.Read` | Integration monitoring |
| `Notification.Template.Manage` | Notification templates |
| `Notification.View` | Channels, subscriptions, overview |
| `Notification.Provider.Manage` | Delivery providers |
| `Notification.Queue.View` | Queues |
| `Notification.History.View` | Delivery history |
| `Notification.Preference.Manage` | Preferences |
| `Directory.Search.Read` | Directory home, search, explorer, results, preferences |
| `Directory.Profile.Read` | Directory profile `/:id` |
| `Directory.SavedSearch.Manage` | Saved searches |
| `Directory.History.Read` | Search history |
| `Intelligence.Dashboard.Read` | Intelligence dashboard + 10 analytics sub-pages |
| `Intelligence.Forecast.Read` | Forecast dashboard |
| `Intelligence.Recommendations.Read` | Recommendations |
| `Intelligence.Search.Read` | Workforce search |
| `Compliance.Standard.View` | All `/app/access-governance/**` routes |
| `Audit.View` | `/app/settings/system/audit` |
| `ComplianceScope` | All primary compliance operational APIs (backend) |
| `OrganizationScope` | Organization admin APIs (backend) |
| `AiScope` | AIOrchestrator APIs (backend) |
| `EXTERNAL_AUDITOR` (role) | Auditor write operations (backend) |

**Authorization evaluation:** `POST /authorization/evaluate` (all permission checks via AccessGovernanceService)

---

# 29. API Endpoint Map by UI Module (Gateway Prefix `/api/v1`)

| UI Module | Primary API Prefixes |
|-----------|---------------------|
| Authentication | `/identity/*`, `/auth/*` |
| Onboarding | `/onboarding/*`, `/frameworks`, `/assessments` |
| Dashboard | `/dashboard/*` |
| Analytics Hub | `/analytics/*` |
| Compliance Maturity | `/compliance-maturity/*` |
| Journey | `/journeys/*`, `/journey/*` |
| Evidence | `/evidence/*`, `/evidence-binder/*` |
| Tasks | `/tasks/*` |
| Findings | `/findings/*` |
| DCC | `/deficiencies/command-center/*` |
| Control Center | `/control-center/compliance/*` |
| Copilot | `/copilot/*` |
| Sidebar AI | `/ai/query`, `/ai/assist` |
| Internal Audit | `/audit/*` (partial), findings APIs |
| Auditor | `/auditor/*` |
| Documentation | `/documentation/*` |
| Patient Safety | `/incidents/*` |
| Quality Intelligence | `/quality/*` |
| Compliance Graph | `/graph/*` |
| Reporting | `/reports/*` |
| Gap Analysis | `/knowledge-base/assessments/*/gap-analysis` |
| Organization Settings | `/settings/organization`, `/organizations/{id}/settings` |
| Identity Users | `/identity/users/*` |
| Security Monitoring | `/security/*` |
| Access Governance | `/roles`, `/permissions`, `/policies`, `/delegations`, `/authorization/*` |
| Governance | `/governance/*`, `/committees/*`, `/approval-chains`, `/escalation-chains` |
| Persons | `/persons/*` |
| Professional Profiles | `/professional-profiles/*` |
| Workforce/Employment | `/employment/*` |
| Enterprise Directory | `/directory/*` |
| Workforce Intelligence | `/intelligence/*` |
| Workforce Admin | `/departments`, `/membership`, `/invitations`, `/organizational-units` |
| Notifications (admin) | `/templates`, `/communication/*`, `/notifications/*`, `/preferences` |
| Integrations | `/connectors`, `/mappings`, `/synchronizations`, `/webhooks`, `/imports`, `/exports` |
| AI Administration | `/ai/profiles`, `/ai/prompts`, `/ai/policies`, `/ai/executions`, `/ai/analytics`, `/ai/approvals` |
| Admin Audit | `/administration/audit/activity` |
| Frameworks (embedded) | `/frameworks/*`, `/nodes/*`, `/scoring/*` (KnowledgeBase + ComplianceService) |

---

# 30. Implementation Status — Priority QA Surfaces

## Tier 1 — Primary Operational (Live API, Main Nav)

1. Dashboard Overview
2. Journey Execution + OE Wizard
3. Evidence Library
4. Tasks (Kanban)
5. Findings Workspace
6. Deficiency Command Center
7. Compliance Copilot
8. Alerts & Notifications (Task Command Center)
9. Analytics Hub
10. Compliance Maturity

## Tier 2 — Header Mega-Menu Workspaces (Live API)

11. Documentation (QDMS)
12. Patient Safety
13. Quality Intelligence
14. Compliance Graph
15. Reports Hub
16. Internal Audit / Final Audit
17. Auditor Workspace

## Tier 3 — Administration (Permission-Guarded)

18. Organization & Branding Settings
19. Identity Users & Password
20. AI Administration
21. Notification Administration
22. Workforce Administration
23. Enterprise Directory
24. Workforce Intelligence
25. Enterprise Integrations
26. Platform Audit & Activity

## Tier 4 — Hidden Admin (Deep-Link Only)

27. Access Governance
28. Governance
29. Security Monitoring
30. Persons / Workforce / Professional (operational)
31. Evidence Review Queue
32. Gap Analysis
33. Control Center (compliance + knowledge graph)

## Tier 5 — Placeholder / Mock (Lower QA Priority)

34. Audit Simulation (client mock)
35. Person Merge (placeholder page)
36. Compliance Defaults (coming soon)
37. CCC Pillars × 7 (placeholder)
38. Surveillance (nav disabled, missing)
39. Personal Account (partial coming-soon)

---

# 31. Backend Service Dependency Matrix

```
┌─────────────────────────────────────────────────────────────────┐
│                     Angular WebApp (praman-web)                 │
│                     All calls → Gateway /api/v1                 │
└───────────────────────────────┬─────────────────────────────────┘
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
 IdentityService      OrganizationService      AccessGovernanceService
        │                       │                       │
        ├─ Auth/Users/Security  ├─ Org/Hospitals/Depts  ├─ RBAC/Permissions
        │                       │                       │
        ▼                       ▼                       ▼
 PersonService ──────► EnterpriseDirectoryService ◄── WorkforceService
        │                       │                       │
        ▼                       │                       ▼
 ProfessionalService ────────────┤            WorkforceIntelligenceService
                                │
        ┌───────────────────────┼───────────────────────┐
        ▼                       ▼                       ▼
 KnowledgeBase            ComplianceService         AIOrchestratorService
 (Frameworks/Nodes/        (Core domain: Journey,    (AI workflows,
  Assessments/Gaps)         Evidence, Findings,       Profiles, Prompts)
                            Copilot, QDMS, etc.)
                                │
                    ┌───────────┼───────────┐
                    ▼           ▼           ▼
            GovernanceService  CommunicationService  IntegrationService
                    │           │                       │
                    └───────────┴───────────────────────┘
                                │
                         OutboxRelayWorker
                         (background events)
```

---

# 32. Notes for QA Checklist Authors

1. **Test hospital context switching** on every Tier 1 module — most APIs are hospital-scoped.
2. **Test all three operational modes** (Self Assessment / Internal Audit / Final Audit) on Journey, Evidence, Findings.
3. **Test permission-denied paths** — each `permissionGuard` route should redirect to `/error/403`.
4. **Test legacy URL redirects** (§21) for bookmark/deep-link compatibility.
5. **Test dialog-based workflows independently** — many features have no dedicated nav entry (§27).
6. **Test export flows** on Findings (CSV) and all 6 Report types + Custom Reports.
7. **Test import flows** on Integrations (preview → execute → rollback).
8. **Test Journey mock mode** when `environment.useJourneyMockWorkspace` is enabled (dev/demo).
9. **FrameworkService does not exist** — framework tests should target KnowledgeBase + ComplianceService APIs.
10. **Hangfire jobs** (`/hangfire` on ComplianceService) affect snapshot/readiness data visible on Dashboard and Maturity — test manual `POST /snapshots/recalculate` trigger.
11. **Communication background processors** affect Notification Queues and Delivery History admin pages.
12. **Unwired guards** (`roleGuard`, `frameworkGuard`, `journeyRoleRedirectGuard`) — no route-level behavior to test currently.

---

*Generated from static analysis of `WebApp/src/app/` routing, navigation models, guards, services, and `ComplaincePlatform/` backend controllers. Scan date: July 10, 2026.*

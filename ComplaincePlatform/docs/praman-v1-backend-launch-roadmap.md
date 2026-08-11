# PRAMAN Backend Launch Roadmap (15 Days)

## Goal

Prepare backend microservices so a hospital can be onboarded and operate NABH compliance workflows.

Architecture:

Operator Portal
↓
Gateway
↓
Microservices

IdentityService
OrganizationService
ComplianceService
AuditService
AIOrchestratorService

---

# Phase 1 — Hospital Onboarding (Day 1–3)

## Backend Tasks

Implement onboarding orchestration.

API:
POST /api/v1/onboarding/hospital

Workflow:

Create Organization
Create Hospital
Create Admin User
Assign Subscription
Return JWT

Entities involved:

Organization
Hospital
OrganizationMember
Subscription

---

# Phase 2 — SaaS Subscription System (Day 3–6)

Entities:

SubscriptionPlan
OrganizationSubscription
FrameworkSubscription

Features:

User quota enforcement
Framework subscription limits
Upgrade subscription

APIs:

GET /subscriptions/current
POST /subscriptions
PATCH /subscriptions/upgrade

Implementation notes:

- Seed at least one default trial `SubscriptionPlan` row (e.g., code `TRIAL`) in OrganizationService.
- Configure `Subscriptions:DefaultTrialPlanId` in Gateway appsettings to the seeded plan's GUID so onboarding (`POST /api/v1/onboarding/hospital`) can automatically assign a trial subscription via OrganizationService internal `/internal/subscriptions` API.

---

# Phase 3 — Dashboard Analytics (Day 6–8)

Expose dashboard APIs.

GET /dashboard/summary
GET /dashboard/hospital

Metrics:

Compliance score
Open tasks
Audit findings
Upcoming renewals

---

# Phase 4 — NABH Compliance Journey (Day 8–12)

Workflow:

Enroll framework
Initialize journey
Generate compliance tasks
Upload evidence
Internal audit
Compliance scoring

Entities:

ComplianceJourney
ComplianceTask
Evidence
AuditFinding

---

# Phase 5 — Enterprise Readiness (Day 12–15)

Implement:

Reporting APIs
Audit logs
Notification events
Evidence repository

Reports:

Compliance Score Report
Audit Findings Report
NABH Readiness Report

---

# Launch Success Criteria

Hospital admin must be able to:

Register hospital
Login
Enroll NABH framework
Assign compliance tasks
Upload evidence
View compliance score
Generate readiness report
